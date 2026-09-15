using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Exceptions;
using MotorPortal.Application.Interfaces;
using MotorPortal.Application.Options;
using MotorPortal.Domain.Constants;
using MotorPortal.Domain.Entities;
using MotorPortal.Infrastructure.Data;
using Npgsql;

namespace MotorPortal.Infrastructure.Services;

public class BatchProcessingService : IBatchProcessingService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BatchProcessingService> _logger;

    public BatchProcessingService(AppDbContext context, IConfiguration configuration, ILogger<BatchProcessingService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BatchProcessSummaryDto> ProcessBatchAsync(long batchId, long userId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.BatchMasters.FirstOrDefaultAsync(b => b.BatchId == batchId, cancellationToken)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        // Step a: validation (also advances status UPLOADED -> VALIDATED internally).
        await RunOrTranslateAsync(() => PgFunctions.ProcessBatchValidationAsync(_context, batchId, cancellationToken));

        var product = await _context.ProductMasters.AsNoTracking().FirstAsync(p => p.ProductId == batch.ProductId, cancellationToken);

        var premiumRules = _configuration.GetSection("PremiumRules").Get<Dictionary<string, PremiumRuleItem>>()
            ?? new Dictionary<string, PremiumRuleItem>();
        if (!premiumRules.TryGetValue(product.ProductCode, out var rule))
        {
            throw new BusinessRuleException($"No premium rule configured for product '{product.ProductCode}'.");
        }

        var gstRate = _configuration.GetValue<decimal>("GstRate");

        var validDetails = await _context.BatchDetails
            .Where(d => d.BatchId == batchId && d.RecordStatus == RecordStatus.Valid)
            .ToListAsync(cancellationToken);

        var premiumCalculated = 0;
        foreach (var detail in validDetails)
        {
            var netPremium = await PgFunctions.CalculateNetPremiumAsync(_context, rule.BasePremium, rule.AddonPremium, rule.Discount, cancellationToken);

            var premium = new PremiumDetails
            {
                DetailId = detail.DetailId,
                BasePremium = rule.BasePremium,
                AddonPremium = rule.AddonPremium,
                Discount = rule.Discount,
                NetPremium = netPremium
            };
            _context.PremiumDetails.Add(premium);
            await _context.SaveChangesAsync(cancellationToken);

            (decimal gstAmount, decimal finalPremium) = await PgFunctions.CalculateGstAsync(_context, netPremium, gstRate, cancellationToken);

            _context.GstDetails.Add(new GstDetails
            {
                PremiumId = premium.PremiumId,
                GstRate = gstRate,
                GstAmount = gstAmount,
                FinalPremium = finalPremium
            });
            await _context.SaveChangesAsync(cancellationToken);

            premiumCalculated++;
        }

        if (premiumCalculated > 0)
        {
            await RunOrTranslateAsync(() => PgFunctions.AdvanceBatchStatusAsync(_context, batchId, BatchStatus.PremiumCalculated, cancellationToken));
            await RunOrTranslateAsync(() => PgFunctions.AdvanceBatchStatusAsync(_context, batchId, BatchStatus.GstCalculated, cancellationToken));
        }
        else
        {
            _logger.LogWarning("Batch {BatchId} has no VALID records; skipping premium/gst/proposal stages.", batchId);
        }

        var proposalsCreated = 0;
        if (premiumCalculated > 0)
        {
            var pricedDetailIds = validDetails.Select(d => d.DetailId).ToList();
            var priced = await _context.GstDetails
                .Where(g => pricedDetailIds.Contains(g.Premium!.DetailId))
                .Select(g => new { g.Premium!.DetailId, g.FinalPremium })
                .ToListAsync(cancellationToken);

            foreach (var item in priced)
            {
                var proposalNo = await PgFunctions.GenerateProposalNoAsync(_context, cancellationToken);
                _context.ProposalMasters.Add(new ProposalMaster
                {
                    DetailId = item.DetailId,
                    ProposalNo = proposalNo,
                    PremiumAmount = item.FinalPremium,
                    Status = ProposalStatus.ProposalCreated,
                    Remarks = "Successful Proposal Created"
                });
                await _context.SaveChangesAsync(cancellationToken);
                proposalsCreated++;
            }

            if (proposalsCreated > 0)
            {
                await RunOrTranslateAsync(() => PgFunctions.AdvanceBatchStatusAsync(_context, batchId, BatchStatus.ProposalCreated, cancellationToken));
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            EntityName = "BATCH_MASTER",
            Action = "PROCESS",
            RefId = batchId.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        var refreshed = await _context.BatchMasters.AsNoTracking().FirstAsync(b => b.BatchId == batchId, cancellationToken);

        return new BatchProcessSummaryDto
        {
            BatchId = batchId,
            ValidCount = refreshed.ValidRecords,
            InvalidCount = refreshed.InvalidRecords,
            PremiumCalculated = premiumCalculated,
            ProposalsCreated = proposalsCreated,
            Status = refreshed.Status
        };
    }

    public async Task<BatchStatusDto> GetStatusAsync(long batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.BatchMasters.AsNoTracking().FirstOrDefaultAsync(b => b.BatchId == batchId, cancellationToken)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        var detailIds = await _context.BatchDetails.AsNoTracking()
            .Where(d => d.BatchId == batchId)
            .Select(d => d.DetailId)
            .ToListAsync(cancellationToken);

        var premiumCalculatedCount = await _context.PremiumDetails.AsNoTracking()
            .CountAsync(p => detailIds.Contains(p.DetailId), cancellationToken);

        var gstCalculatedCount = await _context.GstDetails.AsNoTracking()
            .CountAsync(g => detailIds.Contains(g.Premium!.DetailId), cancellationToken);

        var proposalIds = await _context.ProposalMasters.AsNoTracking()
            .Where(p => detailIds.Contains(p.DetailId))
            .Select(p => p.ProposalId)
            .ToListAsync(cancellationToken);

        var paymentsProcessedCount = await _context.PaymentDetails.AsNoTracking()
            .CountAsync(pd => proposalIds.Contains(pd.ProposalId) && pd.PaymentStatus == PaymentStatus.Processed, cancellationToken);

        var policiesCreatedCount = await _context.PolicyMasters.AsNoTracking()
            .CountAsync(pm => proposalIds.Contains(pm.ProposalId), cancellationToken);

        return new BatchStatusDto
        {
            BatchId = batch.BatchId,
            FileName = batch.FileName,
            Status = batch.Status,
            TotalRecords = batch.TotalRecords,
            ValidRecords = batch.ValidRecords,
            InvalidRecords = batch.InvalidRecords,
            CreatedOn = batch.CreatedOn,
            PremiumCalculatedCount = premiumCalculatedCount,
            GstCalculatedCount = gstCalculatedCount,
            ProposalsCreatedCount = proposalIds.Count,
            PaymentsProcessedCount = paymentsProcessedCount,
            PoliciesCreatedCount = policiesCreatedCount
        };
    }

    public async Task<List<InvalidRecordDto>> GetInvalidRecordsAsync(long batchId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.BatchMasters.AnyAsync(b => b.BatchId == batchId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Batch {batchId} not found.");
        }

        return await _context.InvalidRecords.AsNoTracking()
            .Where(r => r.BatchId == batchId)
            .OrderBy(r => r.InvalidId)
            .Select(r => new InvalidRecordDto
            {
                TransitDate = r.TransitDate,
                InvoiceNo = r.InvoiceNo,
                EngineNo = r.EngineNo,
                ChassisNo = r.ChassisNo,
                ErrorRemarks = r.ErrorRemarks
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ClearInvalidRecordsAsync(long batchId, long userId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.BatchMasters.FirstOrDefaultAsync(b => b.BatchId == batchId, cancellationToken)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        var invalidRecords = await _context.InvalidRecords.Where(r => r.BatchId == batchId).ToListAsync(cancellationToken);
        _context.InvalidRecords.RemoveRange(invalidRecords);

        batch.InvalidRecords = 0;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            EntityName = "BATCH_MASTER",
            Action = "CLEAR_INVALID",
            RefId = batchId.ToString(),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    internal static async Task RunOrTranslateAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (PostgresException ex)
        {
            throw new BusinessRuleException(ex.MessageText, ex);
        }
    }
}

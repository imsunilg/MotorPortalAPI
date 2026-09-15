using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Exceptions;
using MotorPortal.Application.Interfaces;
using MotorPortal.Domain.Constants;
using MotorPortal.Domain.Entities;
using MotorPortal.Infrastructure.Data;
using Npgsql;

namespace MotorPortal.Infrastructure.Services;

public class PaymentTaggingService : IPaymentTaggingService
{
    private readonly AppDbContext _context;
    private readonly IPfGatewayService _pfGateway;
    private readonly ILogger<PaymentTaggingService> _logger;

    public PaymentTaggingService(AppDbContext context, IPfGatewayService pfGateway, ILogger<PaymentTaggingService> logger)
    {
        _context = context;
        _pfGateway = pfGateway;
        _logger = logger;
    }

    public async Task<PaymentBatchResultDto> TagPaymentsAsync(long batchId, long userId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.BatchMasters.FirstOrDefaultAsync(b => b.BatchId == batchId, cancellationToken)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        if (batch.Status == BatchStatus.ProposalCreated)
        {
            await BatchProcessingService.RunOrTranslateAsync(
                () => PgFunctions.AdvanceBatchStatusAsync(_context, batchId, BatchStatus.PaymentPending, cancellationToken));
            batch.Status = BatchStatus.PaymentPending;
        }

        var eligibleProposals = await (
            from p in _context.ProposalMasters
            join d in _context.BatchDetails on p.DetailId equals d.DetailId
            where d.BatchId == batchId
            where !_context.PaymentDetails.Any(pd => pd.ProposalId == p.ProposalId && pd.PaymentStatus == PaymentStatus.Processed)
            select p
        ).ToListAsync(cancellationToken);

        var results = new List<PaymentCaseResultDto>();
        var succeeded = 0;
        var policiesCreated = 0;

        foreach (var proposal in eligibleProposals)
        {
            try
            {
                var confirmation = await _pfGateway.ConfirmPaymentIntentAsync(proposal.ProposalId, proposal.PremiumAmount, cancellationToken);

                await PgFunctions.TagPaymentAsync(_context, proposal.ProposalId, cancellationToken);

                results.Add(new PaymentCaseResultDto
                {
                    ProposalId = proposal.ProposalId,
                    Success = true,
                    Message = $"Payment tagged successfully. {confirmation.Message}"
                });
                succeeded++;

                var policyCreated = await CreatePolicyForProposalAsync(proposal, cancellationToken);
                if (policyCreated)
                {
                    policiesCreated++;
                }
            }
            catch (PostgresException ex)
            {
                _logger.LogWarning("Payment tagging failed for proposal {ProposalId}: {Message}", proposal.ProposalId, ex.MessageText);
                results.Add(new PaymentCaseResultDto
                {
                    ProposalId = proposal.ProposalId,
                    Success = false,
                    Message = ex.MessageText
                });
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            EntityName = "PROPOSAL_MASTER",
            Action = "PAYMENT",
            RefId = batchId.ToString(),
            Timestamp = DateTime.UtcNow
        });

        if (policiesCreated > 0)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                EntityName = "POLICY_MASTER",
                Action = "POLICY_ISSUE",
                RefId = batchId.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync(cancellationToken);

        var refreshed = await _context.BatchMasters.FirstAsync(b => b.BatchId == batchId, cancellationToken);

        var anyProcessedPaymentExists = await (
            from p in _context.ProposalMasters
            join d in _context.BatchDetails on p.DetailId equals d.DetailId
            join pd in _context.PaymentDetails on p.ProposalId equals pd.ProposalId
            where d.BatchId == batchId && pd.PaymentStatus == PaymentStatus.Processed
            select pd.PaymentId
        ).AnyAsync(cancellationToken);

        if (anyProcessedPaymentExists && refreshed.Status == BatchStatus.PaymentPending)
        {
            await BatchProcessingService.RunOrTranslateAsync(
                () => PgFunctions.AdvanceBatchStatusAsync(_context, batchId, BatchStatus.PaymentProcessed, cancellationToken));
            refreshed.Status = BatchStatus.PaymentProcessed;
        }

        if (refreshed.Status == BatchStatus.PaymentProcessed)
        {
            var allProposalIds = await _context.ProposalMasters
                .Where(p => _context.BatchDetails.Any(d => d.BatchId == batchId && d.DetailId == p.DetailId))
                .Select(p => p.ProposalId)
                .ToListAsync(cancellationToken);

            var processedPaymentCount = await _context.PaymentDetails
                .CountAsync(pd => allProposalIds.Contains(pd.ProposalId) && pd.PaymentStatus == PaymentStatus.Processed, cancellationToken);
            var policyCount = await _context.PolicyMasters
                .CountAsync(pm => allProposalIds.Contains(pm.ProposalId), cancellationToken);

            if (processedPaymentCount > 0 && processedPaymentCount == policyCount)
            {
                await BatchProcessingService.RunOrTranslateAsync(
                    () => PgFunctions.AdvanceBatchStatusAsync(_context, batchId, BatchStatus.PolicyCreated, cancellationToken));
                refreshed.Status = BatchStatus.PolicyCreated;
            }
        }

        return new PaymentBatchResultDto
        {
            BatchId = batchId,
            Results = results,
            SucceededCount = succeeded,
            FailedCount = results.Count - succeeded,
            PoliciesCreated = policiesCreated,
            BatchStatus = refreshed.Status
        };
    }

    private async Task<bool> CreatePolicyForProposalAsync(ProposalMaster proposal, CancellationToken cancellationToken)
    {
        var alreadyHasPolicy = await _context.PolicyMasters.AnyAsync(pm => pm.ProposalId == proposal.ProposalId, cancellationToken);
        if (alreadyHasPolicy)
        {
            return false;
        }

        var payment = await _context.PaymentDetails
            .Where(pd => pd.ProposalId == proposal.ProposalId && pd.PaymentStatus == PaymentStatus.Processed)
            .OrderByDescending(pd => pd.PaymentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null)
        {
            return false;
        }

        var detail = await _context.BatchDetails.FirstAsync(d => d.DetailId == proposal.DetailId, cancellationToken);

        var policyNo = await PgFunctions.GeneratePolicyNoAsync(_context, cancellationToken);

        _context.PolicyMasters.Add(new PolicyMaster
        {
            ProposalId = proposal.ProposalId,
            PaymentId = payment.PaymentId,
            PolicyNo = policyNo,
            Make = detail.Make,
            Model = detail.Model,
            EngineNo = detail.EngineNo,
            ChassisNo = detail.ChassisNo,
            Premium = payment.Amount,
            Status = PolicyStatus.Active,
            IssuedOn = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

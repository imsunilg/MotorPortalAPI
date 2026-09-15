using Microsoft.EntityFrameworkCore;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Interfaces;
using MotorPortal.Domain.Constants;
using MotorPortal.Infrastructure.Data;

namespace MotorPortal.Infrastructure.Services;

public class BatchSummaryService : IBatchSummaryService
{
    private readonly AppDbContext _context;

    public BatchSummaryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BatchSummaryDto>> GetBatchSummaryAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var batches = await FilteredBatchesQuery(fromDate, toDate).OrderBy(b => b.BatchId).ToListAsync(cancellationToken);
        if (batches.Count == 0)
        {
            return new List<BatchSummaryDto>();
        }

        var batchIds = batches.Select(b => b.BatchId).ToList();

        // detailId -> (batchId, recordStatus)
        var details = await _context.BatchDetails.AsNoTracking()
            .Where(d => batchIds.Contains(d.BatchId))
            .Select(d => new { d.DetailId, d.BatchId, d.RecordStatus })
            .ToListAsync(cancellationToken);
        var detailIds = details.Select(d => d.DetailId).ToList();

        // detailId -> proposalId
        var proposals = await _context.ProposalMasters.AsNoTracking()
            .Where(p => detailIds.Contains(p.DetailId))
            .Select(p => new { p.ProposalId, p.DetailId })
            .ToListAsync(cancellationToken);
        var proposalIds = proposals.Select(p => p.ProposalId).ToList();

        var processedProposalIds = await _context.PaymentDetails.AsNoTracking()
            .Where(pd => proposalIds.Contains(pd.ProposalId) && pd.PaymentStatus == PaymentStatus.Processed)
            .Select(pd => pd.ProposalId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var processedProposalIdSet = processedProposalIds.ToHashSet();

        var detailIdsWithProposal = proposals.Select(p => p.DetailId).ToHashSet();

        var result = new List<BatchSummaryDto>();
        foreach (var batch in batches)
        {
            var batchDetails = details.Where(d => d.BatchId == batch.BatchId).ToList();
            var pendingProcessing = batchDetails.Count(d => d.RecordStatus == RecordStatus.Valid && !detailIdsWithProposal.Contains(d.DetailId));

            var batchDetailIds = batchDetails.Select(d => d.DetailId).ToHashSet();
            var batchProposalIds = proposals.Where(p => batchDetailIds.Contains(p.DetailId)).Select(p => p.ProposalId).ToList();

            var paymentProcessed = batchProposalIds.Count(pid => processedProposalIdSet.Contains(pid));
            var paymentPending = batchProposalIds.Count - paymentProcessed;

            result.Add(new BatchSummaryDto
            {
                BatchId = batch.BatchId,
                TotalRecords = batch.TotalRecords,
                ValidRecords = batch.ValidRecords,
                InvalidRecords = batch.InvalidRecords,
                PendingProcessing = pendingProcessing,
                PaymentPending = paymentPending,
                PaymentProcessed = paymentProcessed,
                Status = batch.Status,
                CreatedOn = batch.CreatedOn
            });
        }

        return result;
    }

    public async Task<BatchSummaryCountersDto> GetSummaryCountersAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var rows = await GetBatchSummaryAsync(fromDate, toDate, cancellationToken);

        return new BatchSummaryCountersDto
        {
            TotalRecords = rows.Sum(r => r.TotalRecords),
            ValidRecords = rows.Sum(r => r.ValidRecords),
            InvalidRecords = rows.Sum(r => r.InvalidRecords),
            PendingBatchProcessing = rows.Sum(r => r.PendingProcessing),
            PaymentPending = rows.Sum(r => r.PaymentPending),
            PaymentProcessed = rows.Sum(r => r.PaymentProcessed)
        };
    }

    private IQueryable<Domain.Entities.BatchMaster> FilteredBatchesQuery(DateOnly? fromDate, DateOnly? toDate)
    {
        var query = _context.BatchMasters.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            var fromDateTime = DateTime.SpecifyKind(fromDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(b => b.CreatedOn >= fromDateTime);
        }

        if (toDate.HasValue)
        {
            var toDateTimeExclusive = DateTime.SpecifyKind(toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(b => b.CreatedOn < toDateTimeExclusive);
        }

        return query;
    }
}

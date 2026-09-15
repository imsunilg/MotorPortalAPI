using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IBatchSummaryService
{
    Task<List<BatchSummaryDto>> GetBatchSummaryAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<BatchSummaryCountersDto> GetSummaryCountersAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
}

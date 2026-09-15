using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IBatchProcessingService
{
    Task<BatchProcessSummaryDto> ProcessBatchAsync(long batchId, long userId, CancellationToken cancellationToken = default);

    Task<BatchStatusDto> GetStatusAsync(long batchId, CancellationToken cancellationToken = default);

    Task<List<InvalidRecordDto>> GetInvalidRecordsAsync(long batchId, CancellationToken cancellationToken = default);

    Task ClearInvalidRecordsAsync(long batchId, long userId, CancellationToken cancellationToken = default);
}

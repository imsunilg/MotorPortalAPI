using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IPaymentTaggingService
{
    Task<PaymentBatchResultDto> TagPaymentsAsync(long batchId, long userId, CancellationToken cancellationToken = default);
}

using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IPolicyService
{
    Task<List<PolicySearchResultDto>> SearchAsync(string? engineNo, string? chassisNo, string? tcNo, string? policyNo, CancellationToken cancellationToken = default);

    Task<PolicyCancelUploadResultDto> CancelUploadAsync(Stream fileStream, string fileName, long userId, CancellationToken cancellationToken = default);
}

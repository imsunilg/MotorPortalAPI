using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IPolicyCertificateService
{
    /// <summary>Generates (or regenerates) the certificate PDF for a policy and persists the policy_certificate row. Returns the physical file path.</summary>
    Task<string> GenerateCertificateAsync(long policyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the physical path to the certificate, generating it on-demand if it doesn't exist yet.</summary>
    Task<string> GetOrGenerateCertificatePathAsync(long policyId, CancellationToken cancellationToken = default);

    Task<BulkPrintResultDto> BulkPrintAsync(long batchId, long userId, CancellationToken cancellationToken = default);
}

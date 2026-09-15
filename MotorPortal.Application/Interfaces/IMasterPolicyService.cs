using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IMasterPolicyService
{
    Task<List<MasterPolicyDto>> GetMasterPoliciesAsync(CancellationToken cancellationToken = default);

    Task<CdBalanceDto> GetCdBalanceAsync(long masterPolicyId, CancellationToken cancellationToken = default);
}

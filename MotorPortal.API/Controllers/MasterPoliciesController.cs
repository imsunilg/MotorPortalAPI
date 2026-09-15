using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorPortal.Application.Interfaces;

namespace MotorPortal.API.Controllers;

[ApiController]
[Route("api/master-policies")]
[Authorize]
public class MasterPoliciesController : ControllerBase
{
    private readonly IMasterPolicyService _masterPolicyService;

    public MasterPoliciesController(IMasterPolicyService masterPolicyService)
    {
        _masterPolicyService = masterPolicyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMasterPolicies(CancellationToken cancellationToken)
    {
        var result = await _masterPolicyService.GetMasterPoliciesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}/cd-balance")]
    public async Task<IActionResult> GetCdBalance(long id, CancellationToken cancellationToken)
    {
        var result = await _masterPolicyService.GetCdBalanceAsync(id, cancellationToken);
        return Ok(result);
    }
}

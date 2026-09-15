using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorPortal.API.Extensions;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Interfaces;

namespace MotorPortal.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost("policy-issue")]
    public async Task<IActionResult> PolicyIssueReport([FromBody] PolicyIssueReportRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var (bytes, fileName) = await _reportService.GeneratePolicyIssueReportAsync(request.FromDate, request.ToDate, userId, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

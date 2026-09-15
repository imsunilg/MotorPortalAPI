using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorPortal.API.Extensions;
using MotorPortal.Application.Interfaces;

namespace MotorPortal.API.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyCertificateService _policyCertificateService;
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyCertificateService policyCertificateService, IPolicyService policyService)
    {
        _policyCertificateService = policyCertificateService;
        _policyService = policyService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? engineNo, [FromQuery] string? chassisNo, [FromQuery] string? tcNo, [FromQuery] string? policyNo, CancellationToken cancellationToken)
    {
        var results = await _policyService.SearchAsync(engineNo, chassisNo, tcNo, policyNo, cancellationToken);
        return Ok(results);
    }

    [HttpPost("cancel-upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CancelUpload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A file is required." });
        }

        var userId = User.GetUserId();

        await using var stream = file.OpenReadStream();
        var result = await _policyService.CancelUploadAsync(stream, file.FileName, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:long}/certificate")]
    public async Task<IActionResult> GenerateCertificate(long id, CancellationToken cancellationToken)
    {
        var path = await _policyCertificateService.GenerateCertificateAsync(id, cancellationToken);
        return Ok(new { policyId = id, certPath = $"/certificates/{id}.pdf", generatedFile = Path.GetFileName(path) });
    }

    [HttpGet("{id:long}/certificate")]
    public async Task<IActionResult> GetCertificate(long id, CancellationToken cancellationToken)
    {
        var path = await _policyCertificateService.GetOrGenerateCertificatePathAsync(id, cancellationToken);
        var bytes = await System.IO.File.ReadAllBytesAsync(path, cancellationToken);
        return File(bytes, "application/pdf", $"policy-{id}-certificate.pdf");
    }
}

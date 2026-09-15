using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorPortal.Application.Interfaces;

namespace MotorPortal.API.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyCertificateService _policyCertificateService;

    public PoliciesController(IPolicyCertificateService policyCertificateService)
    {
        _policyCertificateService = policyCertificateService;
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

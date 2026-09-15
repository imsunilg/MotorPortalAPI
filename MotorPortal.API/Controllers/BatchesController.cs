using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorPortal.API.Extensions;
using MotorPortal.Application.Interfaces;

namespace MotorPortal.API.Controllers;

[ApiController]
[Route("api/batches")]
[Authorize]
public class BatchesController : ControllerBase
{
    private readonly IExcelBatchService _excelBatchService;
    private readonly IBatchProcessingService _batchProcessingService;
    private readonly IPaymentTaggingService _paymentTaggingService;
    private readonly IPolicyCertificateService _policyCertificateService;
    private readonly ILogger<BatchesController> _logger;

    public BatchesController(
        IExcelBatchService excelBatchService,
        IBatchProcessingService batchProcessingService,
        IPaymentTaggingService paymentTaggingService,
        IPolicyCertificateService policyCertificateService,
        ILogger<BatchesController> logger)
    {
        _excelBatchService = excelBatchService;
        _batchProcessingService = batchProcessingService;
        _paymentTaggingService = paymentTaggingService;
        _policyCertificateService = policyCertificateService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload([FromForm] int productId, [FromForm] int functionId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A file is required." });
        }

        var userId = User.GetUserId();

        await using var stream = file.OpenReadStream();
        var result = await _excelBatchService.UploadAsync(stream, file.FileName, productId, functionId, userId, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new { message = "The uploaded file is invalid.", errors = result.Errors });
        }

        return Ok(result.Result);
    }

    [HttpGet("{id:long}/sample-template")]
    public IActionResult SampleTemplate(long id)
    {
        var bytes = _excelBatchService.GenerateSampleTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "batch-upload-template.xlsx");
    }

    [HttpPost("{id:long}/process")]
    public async Task<IActionResult> Process(long id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var summary = await _batchProcessingService.ProcessBatchAsync(id, userId, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("{id:long}/status")]
    public async Task<IActionResult> Status(long id, CancellationToken cancellationToken)
    {
        var status = await _batchProcessingService.GetStatusAsync(id, cancellationToken);
        return Ok(status);
    }

    [HttpGet("{id:long}/invalid-records")]
    public async Task<IActionResult> InvalidRecords(long id, CancellationToken cancellationToken)
    {
        var records = await _batchProcessingService.GetInvalidRecordsAsync(id, cancellationToken);
        return Ok(records);
    }

    [HttpDelete("{id:long}/invalid-records")]
    public async Task<IActionResult> ClearInvalidRecords(long id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _batchProcessingService.ClearInvalidRecordsAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/payments")]
    public async Task<IActionResult> TagPayments(long id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _paymentTaggingService.TagPaymentsAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:long}/bulk-print")]
    public async Task<IActionResult> BulkPrint(long id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _policyCertificateService.BulkPrintAsync(id, userId, cancellationToken);
        return Ok(result);
    }
}

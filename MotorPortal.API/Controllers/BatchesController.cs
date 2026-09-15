using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorPortal.API.Extensions;
using MotorPortal.API.Models;
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
    private readonly IBatchSummaryService _batchSummaryService;
    private readonly ILogger<BatchesController> _logger;

    public BatchesController(
        IExcelBatchService excelBatchService,
        IBatchProcessingService batchProcessingService,
        IPaymentTaggingService paymentTaggingService,
        IPolicyCertificateService policyCertificateService,
        IBatchSummaryService batchSummaryService,
        ILogger<BatchesController> logger)
    {
        _excelBatchService = excelBatchService;
        _batchProcessingService = batchProcessingService;
        _paymentTaggingService = paymentTaggingService;
        _policyCertificateService = policyCertificateService;
        _batchSummaryService = batchSummaryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBatchSummary([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken cancellationToken)
    {
        var summary = await _batchSummaryService.GetBatchSummaryAsync(fromDate, toDate, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("summary-counters")]
    public async Task<IActionResult> GetSummaryCounters([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken cancellationToken)
    {
        var counters = await _batchSummaryService.GetSummaryCountersAsync(fromDate, toDate, cancellationToken);
        return Ok(counters);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload([FromForm] BatchUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "A file is required." });
        }

        var userId = User.GetUserId();

        await using var stream = request.File.OpenReadStream();
        var result = await _excelBatchService.UploadAsync(stream, request.File.FileName, request.ProductId, request.FunctionId, userId, cancellationToken);

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

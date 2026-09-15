using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Exceptions;
using MotorPortal.Application.Interfaces;
using MotorPortal.Domain.Constants;
using MotorPortal.Domain.Entities;
using MotorPortal.Infrastructure.Data;

namespace MotorPortal.Infrastructure.Services;

public class ExcelBatchService : IExcelBatchService
{
    private static readonly string[] ExpectedHeaders =
    {
        "MASTER_POLICY_NO", "ENGINE_NO", "CHASSIS_NO", "TC_NO",
        "INVOICE_NO", "TRANSIT_DATE", "MAKE", "MODEL"
    };

    private const int MaxRows = 200;

    private readonly AppDbContext _context;

    public ExcelBatchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExcelParseResult> UploadAsync(Stream fileStream, string fileName, int productId, int functionId, long userId, CancellationToken cancellationToken = default)
    {
        var result = new ExcelParseResult();

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            result.Errors.Add(new ExcelRowErrorDto { Row = 0, Reason = "File must be a .xlsx or .xls Excel workbook." });
            return result;
        }

        var product = await _context.ProductMasters.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product {productId} not found.");
        }

        var function = await _context.FunctionMasters.AsNoTracking().FirstOrDefaultAsync(f => f.FunctionId == functionId, cancellationToken);
        if (function is null)
        {
            throw new NotFoundException($"Function {functionId} not found.");
        }

        // Buffer the stream since ClosedXML needs a seekable stream.
        using var memory = new MemoryStream();
        await fileStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(memory);
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ExcelRowErrorDto { Row = 0, Reason = $"Unable to read the Excel file: {ex.Message}" });
            return result;
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet is null)
            {
                result.Errors.Add(new ExcelRowErrorDto { Row = 0, Reason = "The workbook has no worksheets." });
                return result;
            }

            var usedRange = worksheet.RangeUsed();
            if (usedRange is null)
            {
                result.Errors.Add(new ExcelRowErrorDto { Row = 0, Reason = "The worksheet is empty." });
                return result;
            }

            // Validate header row.
            for (var col = 1; col <= ExpectedHeaders.Length; col++)
            {
                var headerCell = worksheet.Cell(1, col).GetString().Trim();
                if (!string.Equals(headerCell, ExpectedHeaders[col - 1], StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new ExcelRowErrorDto
                    {
                        Row = 1,
                        Reason = $"Column {col} header must be '{ExpectedHeaders[col - 1]}' but found '{headerCell}'."
                    });
                }
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            var lastRow = usedRange.LastRow().RowNumber();
            var dataRows = new List<BatchDetail>();

            for (var row = 2; row <= lastRow; row++)
            {
                var cells = new string?[ExpectedHeaders.Length];
                var isBlankRow = true;
                for (var col = 1; col <= ExpectedHeaders.Length; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    var value = cell.IsEmpty() ? null : cell.GetString().Trim();
                    cells[col - 1] = string.IsNullOrWhiteSpace(value) ? null : value;
                    if (cells[col - 1] is not null)
                    {
                        isBlankRow = false;
                    }
                }

                if (isBlankRow)
                {
                    result.Errors.Add(new ExcelRowErrorDto { Row = row, Reason = "Row is completely blank." });
                    continue;
                }

                var masterPolicyNo = cells[0];
                var engineNo = cells[1];
                var chassisNo = cells[2];
                var tcNo = cells[3];
                var invoiceNo = cells[4];
                var transitDateRaw = cells[5];
                var make = cells[6];
                var model = cells[7];

                var rowErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(masterPolicyNo)) rowErrors.Add("MASTER_POLICY_NO is required.");
                if (string.IsNullOrWhiteSpace(engineNo)) rowErrors.Add("ENGINE_NO is required.");
                if (string.IsNullOrWhiteSpace(chassisNo)) rowErrors.Add("CHASSIS_NO is required.");
                if (string.IsNullOrWhiteSpace(invoiceNo)) rowErrors.Add("INVOICE_NO is required.");

                DateOnly? transitDate = null;
                var transitCell = worksheet.Cell(row, 6);
                if (transitCell.IsEmpty() || string.IsNullOrWhiteSpace(transitDateRaw))
                {
                    rowErrors.Add("TRANSIT_DATE is required.");
                }
                else if (transitCell.TryGetValue<DateTime>(out var dtValue))
                {
                    transitDate = DateOnly.FromDateTime(dtValue);
                }
                else if (DateTime.TryParse(transitDateRaw, out var parsedDate))
                {
                    transitDate = DateOnly.FromDateTime(parsedDate);
                }
                else
                {
                    rowErrors.Add($"TRANSIT_DATE '{transitDateRaw}' is not a valid date.");
                }

                if (rowErrors.Count > 0)
                {
                    result.Errors.Add(new ExcelRowErrorDto { Row = row, Reason = string.Join(" ", rowErrors) });
                    continue;
                }

                dataRows.Add(new BatchDetail
                {
                    MasterPolicyNo = masterPolicyNo!,
                    EngineNo = engineNo!,
                    ChassisNo = chassisNo!,
                    TcNo = tcNo,
                    InvoiceNo = invoiceNo,
                    TransitDate = transitDate,
                    Make = make,
                    Model = model,
                    RecordStatus = RecordStatus.Pending
                });
            }

            if (dataRows.Count == 0)
            {
                result.Errors.Add(new ExcelRowErrorDto { Row = 0, Reason = "The workbook contains no data rows." });
            }
            else if (dataRows.Count > MaxRows)
            {
                result.Errors.Add(new ExcelRowErrorDto { Row = 0, Reason = $"A batch cannot contain more than {MaxRows} records (found {dataRows.Count})." });
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            var batch = new BatchMaster
            {
                UserId = userId,
                ProductId = productId,
                FunctionId = functionId,
                FileName = fileName,
                TotalRecords = dataRows.Count,
                ValidRecords = 0,
                InvalidRecords = 0,
                Status = BatchStatus.Uploaded,
                CreatedOn = DateTime.UtcNow
            };

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            _context.BatchMasters.Add(batch);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var detail in dataRows)
            {
                detail.BatchId = batch.BatchId;
            }
            _context.BatchDetails.AddRange(dataRows);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                EntityName = "BATCH_MASTER",
                Action = "UPLOAD",
                RefId = batch.BatchId.ToString(),
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.IsValid = true;
            result.Result = new BatchUploadResultDto { BatchId = batch.BatchId, TotalRecords = batch.TotalRecords };
            return result;
        }
    }

    public byte[] GenerateSampleTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Batch Upload");

        for (var col = 0; col < ExpectedHeaders.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = ExpectedHeaders[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var sampleRows = new[]
        {
            new object?[] { "DL-3010/A/1485551", "ENG-SAMPLE-0001", "CHS-SAMPLE-0001", "TC0001", "INV-SAMPLE-0001", DateTime.Today.AddDays(-3), "Maruti", "Alto" },
            new object?[] { "DL-3010/A/1485552", "ENG-SAMPLE-0002", "CHS-SAMPLE-0002", "TC0002", "INV-SAMPLE-0002", DateTime.Today.AddDays(-2), "Hyundai", "i10" },
            new object?[] { "DL-3010/B/2285561", "ENG-SAMPLE-0003", "CHS-SAMPLE-0003", "TC0003", "INV-SAMPLE-0003", DateTime.Today.AddDays(-1), "Eicher", "Pro 2049" }
        };

        for (var r = 0; r < sampleRows.Length; r++)
        {
            for (var c = 0; c < sampleRows[r].Length; c++)
            {
                var value = sampleRows[r][c];
                if (value is DateTime dt)
                {
                    worksheet.Cell(r + 2, c + 1).Value = dt;
                    worksheet.Cell(r + 2, c + 1).Style.DateFormat.Format = "yyyy-mm-dd";
                }
                else
                {
                    worksheet.Cell(r + 2, c + 1).Value = value?.ToString();
                }
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public class ExcelParseResult
{
    public bool IsValid { get; set; }
    public List<ExcelRowErrorDto> Errors { get; set; } = new();
    public BatchUploadResultDto? Result { get; set; }
}

public interface IExcelBatchService
{
    Task<ExcelParseResult> UploadAsync(Stream fileStream, string fileName, int productId, int functionId, long userId, CancellationToken cancellationToken = default);

    byte[] GenerateSampleTemplate();
}

using MotorPortal.Application.DTOs;

namespace MotorPortal.Application.Interfaces;

public interface IReportService
{
    Task<(byte[] FileBytes, string FileName)> GeneratePolicyIssueReportAsync(DateOnly fromDate, DateOnly toDate, long userId, CancellationToken cancellationToken = default);
}

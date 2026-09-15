using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Interfaces;
using MotorPortal.Domain.Entities;
using MotorPortal.Infrastructure.Data;

namespace MotorPortal.Infrastructure.Services;

public class PolicyService : IPolicyService
{
    private const string CancelledStatus = "CANCELLED";

    private readonly AppDbContext _context;

    public PolicyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PolicySearchResultDto>> SearchAsync(string? engineNo, string? chassisNo, string? tcNo, string? policyNo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(engineNo) && string.IsNullOrWhiteSpace(chassisNo)
            && string.IsNullOrWhiteSpace(tcNo) && string.IsNullOrWhiteSpace(policyNo))
        {
            throw new ArgumentException("At least one search parameter (engineNo, chassisNo, tcNo, policyNo) is required.");
        }

        var query =
            from pm in _context.PolicyMasters.AsNoTracking()
            join p in _context.ProposalMasters.AsNoTracking() on pm.ProposalId equals p.ProposalId
            join d in _context.BatchDetails.AsNoTracking() on p.DetailId equals d.DetailId
            select new { pm, d };

        if (!string.IsNullOrWhiteSpace(engineNo))
        {
            query = query.Where(x => x.pm.EngineNo == engineNo);
        }

        if (!string.IsNullOrWhiteSpace(chassisNo))
        {
            query = query.Where(x => x.pm.ChassisNo == chassisNo);
        }

        if (!string.IsNullOrWhiteSpace(policyNo))
        {
            query = query.Where(x => x.pm.PolicyNo == policyNo);
        }

        if (!string.IsNullOrWhiteSpace(tcNo))
        {
            query = query.Where(x => x.d.TcNo == tcNo);
        }

        return await query
            .OrderBy(x => x.pm.PolicyId)
            .Select(x => new PolicySearchResultDto
            {
                PolicyId = x.pm.PolicyId,
                PolicyNo = x.pm.PolicyNo,
                Make = x.pm.Make,
                Model = x.pm.Model,
                EngineNo = x.pm.EngineNo,
                ChassisNo = x.pm.ChassisNo,
                Premium = x.pm.Premium,
                Status = x.pm.Status,
                IssuedOn = x.pm.IssuedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyCancelUploadResultDto> CancelUploadAsync(Stream fileStream, string fileName, long userId, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            throw new ArgumentException("File must be a .xlsx or .xls Excel workbook.");
        }

        using var memory = new MemoryStream();
        await fileStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        using var workbook = new XLWorkbook(memory);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            throw new ArgumentException("The workbook has no worksheets.");
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            throw new ArgumentException("The worksheet is empty.");
        }

        var header = worksheet.Cell(1, 1).GetString().Trim();
        if (!string.Equals(header, "POLICY_NO", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Column 1 header must be 'POLICY_NO'.");
        }

        var lastRow = usedRange.LastRow().RowNumber();
        var policyNumbers = new List<string>();
        for (var row = 2; row <= lastRow; row++)
        {
            var cell = worksheet.Cell(row, 1);
            var value = cell.IsEmpty() ? null : cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                policyNumbers.Add(value);
            }
        }

        var result = new PolicyCancelUploadResultDto();

        foreach (var policyNo in policyNumbers)
        {
            var policy = await _context.PolicyMasters.FirstOrDefaultAsync(p => p.PolicyNo == policyNo, cancellationToken);
            if (policy is null)
            {
                result.Rejected.Add(new PolicyCancelRejectionDto { PolicyNo = policyNo, Reason = "Policy not found" });
                continue;
            }

            if (string.Equals(policy.Status, CancelledStatus, StringComparison.OrdinalIgnoreCase))
            {
                result.Rejected.Add(new PolicyCancelRejectionDto { PolicyNo = policyNo, Reason = "Policy already cancelled" });
                continue;
            }

            policy.Status = CancelledStatus;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                EntityName = "POLICY_MASTER",
                Action = "CANCEL",
                RefId = policyNo,
                Timestamp = DateTime.UtcNow
            });

            result.Cancelled.Add(policyNo);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}

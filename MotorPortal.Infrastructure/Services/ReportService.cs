using System.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Interfaces;
using MotorPortal.Domain.Entities;
using MotorPortal.Infrastructure.Data;
using Npgsql;

namespace MotorPortal.Infrastructure.Services;

public class ReportService : IReportService
{
    private static readonly string[] ColumnHeaders =
    {
        "Batch ID", "Policy Number", "Proposal Number", "Master Policy", "Product",
        "Make", "Model", "Engine Number", "Chassis Number", "Premium", "Payment Status",
        "Issued Date", "User"
    };

    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(byte[] FileBytes, string FileName)> GeneratePolicyIssueReportAsync(DateOnly fromDate, DateOnly toDate, long userId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryPolicyIssueReportAsync(fromDate, toDate, cancellationToken);

        _context.ReportLogs.Add(new ReportLog
        {
            UserId = userId,
            ReportType = "POLICY_ISSUE",
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedOn = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        var bytes = BuildWorkbook(rows);
        var fileName = $"policy-issue-report-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx";
        return (bytes, fileName);
    }

    private async Task<List<PolicyIssueReportRowDto>> QueryPolicyIssueReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM \"motorportal\".vw_policy_issue_report " +
                               "WHERE \"Issued Date\" >= @fromDate AND \"Issued Date\" < @toDateExclusive " +
                               "ORDER BY \"Issued Date\"";
            cmd.Parameters.Add(new NpgsqlParameter("fromDate", fromDate.ToDateTime(TimeOnly.MinValue)));
            cmd.Parameters.Add(new NpgsqlParameter("toDateExclusive", toDate.AddDays(1).ToDateTime(TimeOnly.MinValue)));

            var rows = new List<PolicyIssueReportRowDto>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new PolicyIssueReportRowDto
                {
                    BatchId = reader.GetInt64(reader.GetOrdinal("Batch ID")),
                    PolicyNumber = reader.GetString(reader.GetOrdinal("Policy Number")),
                    ProposalNumber = reader.GetString(reader.GetOrdinal("Proposal Number")),
                    MasterPolicy = reader.GetString(reader.GetOrdinal("Master Policy")),
                    Product = reader.GetString(reader.GetOrdinal("Product")),
                    Make = reader.IsDBNull(reader.GetOrdinal("Make")) ? null : reader.GetString(reader.GetOrdinal("Make")),
                    Model = reader.IsDBNull(reader.GetOrdinal("Model")) ? null : reader.GetString(reader.GetOrdinal("Model")),
                    EngineNumber = reader.GetString(reader.GetOrdinal("Engine Number")),
                    ChassisNumber = reader.GetString(reader.GetOrdinal("Chassis Number")),
                    Premium = reader.GetDecimal(reader.GetOrdinal("Premium")),
                    PaymentStatus = reader.GetString(reader.GetOrdinal("Payment Status")),
                    IssuedDate = reader.GetDateTime(reader.GetOrdinal("Issued Date")),
                    User = reader.GetString(reader.GetOrdinal("User"))
                });
            }

            return rows;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static byte[] BuildWorkbook(List<PolicyIssueReportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Policy Issue Report");

        for (var col = 0; col < ColumnHeaders.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = ColumnHeaders[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var excelRow = r + 2;
            worksheet.Cell(excelRow, 1).Value = row.BatchId;
            worksheet.Cell(excelRow, 2).Value = row.PolicyNumber;
            worksheet.Cell(excelRow, 3).Value = row.ProposalNumber;
            worksheet.Cell(excelRow, 4).Value = row.MasterPolicy;
            worksheet.Cell(excelRow, 5).Value = row.Product;
            worksheet.Cell(excelRow, 6).Value = row.Make;
            worksheet.Cell(excelRow, 7).Value = row.Model;
            worksheet.Cell(excelRow, 8).Value = row.EngineNumber;
            worksheet.Cell(excelRow, 9).Value = row.ChassisNumber;
            worksheet.Cell(excelRow, 10).Value = row.Premium;
            worksheet.Cell(excelRow, 11).Value = row.PaymentStatus;
            worksheet.Cell(excelRow, 12).Value = row.IssuedDate;
            worksheet.Cell(excelRow, 12).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            worksheet.Cell(excelRow, 13).Value = row.User;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

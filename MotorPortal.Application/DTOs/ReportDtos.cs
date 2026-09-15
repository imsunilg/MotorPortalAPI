namespace MotorPortal.Application.DTOs;

public class PolicyIssueReportRequestDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class PolicyIssueReportRowDto
{
    public long BatchId { get; set; }
    public string PolicyNumber { get; set; } = null!;
    public string ProposalNumber { get; set; } = null!;
    public string MasterPolicy { get; set; } = null!;
    public string Product { get; set; } = null!;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string EngineNumber { get; set; } = null!;
    public string ChassisNumber { get; set; } = null!;
    public decimal Premium { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public DateTime IssuedDate { get; set; }
    public string User { get; set; } = null!;
}

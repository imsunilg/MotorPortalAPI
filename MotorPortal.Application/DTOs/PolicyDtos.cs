namespace MotorPortal.Application.DTOs;

public class PolicySearchResultDto
{
    public long PolicyId { get; set; }
    public string PolicyNo { get; set; } = null!;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string EngineNo { get; set; } = null!;
    public string ChassisNo { get; set; } = null!;
    public decimal Premium { get; set; }
    public string Status { get; set; } = null!;
    public DateTime IssuedOn { get; set; }
}

public class PolicyCancelRejectionDto
{
    public string PolicyNo { get; set; } = null!;
    public string Reason { get; set; } = null!;
}

public class PolicyCancelUploadResultDto
{
    public List<string> Cancelled { get; set; } = new();
    public List<PolicyCancelRejectionDto> Rejected { get; set; } = new();
}

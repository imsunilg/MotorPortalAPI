namespace MotorPortal.Application.DTOs;

public class MasterPolicyDto
{
    public long MasterPolicyId { get; set; }
    public string MasterPolicyNo { get; set; } = null!;
    public string CustomerNo { get; set; } = null!;
    public string? CdbgNo { get; set; }
    public int ProductId { get; set; }
}

public class CdBalanceDto
{
    public long MasterPolicyId { get; set; }
    public string MasterPolicyNo { get; set; } = null!;
    public decimal CdBalance { get; set; }
}

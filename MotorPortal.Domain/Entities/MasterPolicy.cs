namespace MotorPortal.Domain.Entities;

public class MasterPolicy
{
    public long MasterPolicyId { get; set; }
    public int ProductId { get; set; }
    public string MasterPolicyNo { get; set; } = null!;
    public string CustomerNo { get; set; } = null!;
    public string? CdbgNo { get; set; }
    public decimal CdBalance { get; set; }

    public ProductMaster? Product { get; set; }
}

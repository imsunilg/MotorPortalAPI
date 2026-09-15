namespace MotorPortal.Domain.Entities;

public class ProposalMaster
{
    public long ProposalId { get; set; }
    public long DetailId { get; set; }
    public string ProposalNo { get; set; } = null!;
    public decimal PremiumAmount { get; set; }
    public string Status { get; set; } = null!;
    public string? Remarks { get; set; }

    public BatchDetail? Detail { get; set; }
}

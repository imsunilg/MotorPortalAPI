using System;

namespace MotorPortal.Domain.Entities;

public class PaymentDetails
{
    public long PaymentId { get; set; }
    public long ProposalId { get; set; }
    public long MasterPolicyId { get; set; }
    public string? PfRefNo { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public DateTime? TaggedOn { get; set; }

    public ProposalMaster? Proposal { get; set; }
    public MasterPolicy? MasterPolicy { get; set; }
}

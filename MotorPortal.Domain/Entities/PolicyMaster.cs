using System;

namespace MotorPortal.Domain.Entities;

public class PolicyMaster
{
    public long PolicyId { get; set; }
    public long ProposalId { get; set; }
    public long PaymentId { get; set; }
    public string PolicyNo { get; set; } = null!;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string EngineNo { get; set; } = null!;
    public string ChassisNo { get; set; } = null!;
    public decimal Premium { get; set; }
    public string Status { get; set; } = null!;
    public DateTime IssuedOn { get; set; }

    public ProposalMaster? Proposal { get; set; }
    public PaymentDetails? Payment { get; set; }
}

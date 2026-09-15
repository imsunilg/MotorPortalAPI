using System;

namespace MotorPortal.Domain.Entities;

public class BatchDetail
{
    public long DetailId { get; set; }
    public long BatchId { get; set; }
    public string MasterPolicyNo { get; set; } = null!;
    public string EngineNo { get; set; } = null!;
    public string ChassisNo { get; set; } = null!;
    public string? TcNo { get; set; }
    public string? InvoiceNo { get; set; }
    public DateOnly? TransitDate { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string RecordStatus { get; set; } = null!;

    public BatchMaster? Batch { get; set; }
}

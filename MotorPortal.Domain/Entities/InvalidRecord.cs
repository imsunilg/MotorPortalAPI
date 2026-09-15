namespace MotorPortal.Domain.Entities;

public class InvalidRecord
{
    public long InvalidId { get; set; }
    public long BatchId { get; set; }
    public long DetailId { get; set; }
    public DateOnly? TransitDate { get; set; }
    public string? InvoiceNo { get; set; }
    public string? EngineNo { get; set; }
    public string? ChassisNo { get; set; }
    public string ErrorRemarks { get; set; } = null!;

    public BatchMaster? Batch { get; set; }
    public BatchDetail? Detail { get; set; }
}

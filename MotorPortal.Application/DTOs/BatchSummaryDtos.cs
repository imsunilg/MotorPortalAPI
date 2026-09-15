namespace MotorPortal.Application.DTOs;

public class BatchSummaryDto
{
    public long BatchId { get; set; }
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int PendingProcessing { get; set; }
    public int PaymentPending { get; set; }
    public int PaymentProcessed { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedOn { get; set; }
}

public class BatchSummaryCountersDto
{
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int PendingProcessing { get; set; }
    public int PaymentPending { get; set; }
    public int PaymentProcessed { get; set; }
}

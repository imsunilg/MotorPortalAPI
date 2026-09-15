namespace MotorPortal.Application.DTOs;

public class ExcelRowErrorDto
{
    public int Row { get; set; }
    public string Reason { get; set; } = null!;
}

public class BatchUploadResultDto
{
    public long BatchId { get; set; }
    public int TotalRecords { get; set; }
}

public class InvalidRecordDto
{
    public DateOnly? TransitDate { get; set; }
    public string? InvoiceNo { get; set; }
    public string? EngineNo { get; set; }
    public string? ChassisNo { get; set; }
    public string ErrorRemarks { get; set; } = null!;
}

public class BatchProcessSummaryDto
{
    public long BatchId { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int PremiumCalculated { get; set; }
    public int ProposalsCreated { get; set; }
    public string Status { get; set; } = null!;
}

public class BatchStatusDto
{
    public long BatchId { get; set; }
    public string FileName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public DateTime CreatedOn { get; set; }
    public int PremiumCalculatedCount { get; set; }
    public int GstCalculatedCount { get; set; }
    public int ProposalsCreatedCount { get; set; }
    public int PaymentsProcessedCount { get; set; }
    public int PoliciesCreatedCount { get; set; }
}

public class PaymentCaseResultDto
{
    public long ProposalId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

public class PaymentBatchResultDto
{
    public long BatchId { get; set; }
    public List<PaymentCaseResultDto> Results { get; set; } = new();
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public int PoliciesCreated { get; set; }
    public string BatchStatus { get; set; } = null!;
}

public class BulkPrintItemDto
{
    public long PolicyId { get; set; }
    public string PolicyNo { get; set; } = null!;
    public string CertPath { get; set; } = null!;
}

public class BulkPrintResultDto
{
    public long BatchId { get; set; }
    public string Status { get; set; } = null!;
    public List<BulkPrintItemDto> Certificates { get; set; } = new();
}

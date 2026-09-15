using System;

namespace MotorPortal.Domain.Entities;

public class ReportLog
{
    public long ReportId { get; set; }
    public long UserId { get; set; }
    public string ReportType { get; set; } = null!;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public DateTime GeneratedOn { get; set; }

    public UserMaster? User { get; set; }
}

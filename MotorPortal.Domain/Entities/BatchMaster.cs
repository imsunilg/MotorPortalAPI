using System;
using System.Collections.Generic;

namespace MotorPortal.Domain.Entities;

public class BatchMaster
{
    public long BatchId { get; set; }
    public long UserId { get; set; }
    public int ProductId { get; set; }
    public int FunctionId { get; set; }
    public string FileName { get; set; } = null!;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedOn { get; set; }

    public UserMaster? User { get; set; }
    public ProductMaster? Product { get; set; }
    public FunctionMaster? Function { get; set; }
    public ICollection<BatchDetail> BatchDetails { get; set; } = new List<BatchDetail>();
}

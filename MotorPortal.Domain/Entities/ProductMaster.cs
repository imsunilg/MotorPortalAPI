namespace MotorPortal.Domain.Entities;

public class ProductMaster
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public char Status { get; set; }
}

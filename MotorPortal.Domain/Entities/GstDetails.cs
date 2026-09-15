namespace MotorPortal.Domain.Entities;

public class GstDetails
{
    public long GstId { get; set; }
    public long PremiumId { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal FinalPremium { get; set; }

    public PremiumDetails? Premium { get; set; }
}

namespace MotorPortal.Domain.Entities;

public class PremiumDetails
{
    public long PremiumId { get; set; }
    public long DetailId { get; set; }
    public decimal BasePremium { get; set; }
    public decimal AddonPremium { get; set; }
    public decimal Discount { get; set; }
    public decimal NetPremium { get; set; }

    public BatchDetail? Detail { get; set; }
    public GstDetails? GstDetails { get; set; }
}

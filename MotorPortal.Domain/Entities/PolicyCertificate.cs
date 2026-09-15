using System;

namespace MotorPortal.Domain.Entities;

public class PolicyCertificate
{
    public long CertId { get; set; }
    public long PolicyId { get; set; }
    public string CertPath { get; set; } = null!;
    public DateTime GeneratedOn { get; set; }

    public PolicyMaster? Policy { get; set; }
}

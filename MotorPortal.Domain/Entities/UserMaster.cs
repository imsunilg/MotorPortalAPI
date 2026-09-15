using System;

namespace MotorPortal.Domain.Entities;

public class UserMaster
{
    public long UserId { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!;
    public char Status { get; set; }
    public DateTime CreatedOn { get; set; }
}

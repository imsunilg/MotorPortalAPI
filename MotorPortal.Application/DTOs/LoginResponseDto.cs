namespace MotorPortal.Application.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

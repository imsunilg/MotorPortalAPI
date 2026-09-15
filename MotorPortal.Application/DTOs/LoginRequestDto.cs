using System.ComponentModel.DataAnnotations;

namespace MotorPortal.Application.DTOs;

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

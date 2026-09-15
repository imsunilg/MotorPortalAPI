using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MotorPortal.API.Controllers;

/// <summary>
/// Trivial protected endpoint used to verify that [Authorize] correctly
/// rejects unauthenticated requests. Not part of the business API surface.
/// </summary>
[ApiController]
[Route("api/secure-ping")]
[Authorize]
public class SecureTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var username = User.Identity?.Name ?? User.FindFirst("username")?.Value;
        return Ok(new { message = "pong", user = username });
    }
}

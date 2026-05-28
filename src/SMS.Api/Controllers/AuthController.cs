using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;

namespace SMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>ورود به سیستم و دریافت توکن JWT</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var r = await _auth.LoginAsync(dto);
        return r.Success ? Ok(r.Data) : Unauthorized(new { message = r.Errors.FirstOrDefault() });
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SMS.Application.Common;

namespace SMS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _ctx;
    public CurrentUserService(IHttpContextAccessor ctx) => _ctx = ctx;

    private ClaimsPrincipal? User => _ctx.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var v = User?.FindFirst("UserId")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    public int? PersonId
    {
        get
        {
            var v = User?.FindFirst("PersonId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    public string? Username => User?.Identity?.Name;
    public string? FullName => User?.FindFirst("FullName")?.Value;

    public string? IpAddress => _ctx.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => _ctx.HttpContext?.Request.Headers["User-Agent"].ToString();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string roleName) => User?.IsInRole(roleName) ?? false;

    public IEnumerable<string> Roles =>
        User?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value) ?? Array.Empty<string>();
}

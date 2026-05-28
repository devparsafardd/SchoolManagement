namespace SMS.Application.Common;

/// <summary>
/// سرویس دریافت اطلاعات کاربر جاری از HttpContext
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    int? PersonId { get; }
    string? Username { get; }
    string? FullName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string roleName);
    IEnumerable<string> Roles { get; }
}

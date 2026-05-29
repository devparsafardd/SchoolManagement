using Microsoft.AspNetCore.Mvc;

namespace SMS.Web.Controllers;

/// <summary>تنظیم تم و حالت (روشن/تاریک)</summary>
public class ThemeController : Controller
{
    [HttpPost]
    public IActionResult Set(string? theme, string? mode)
    {
        var opts = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false,
            SameSite = SameSiteMode.Lax
        };
        var allowedThemes = new[] { "indigo", "emerald", "rose", "amber", "ocean" };
        var allowedModes = new[] { "light", "dark" };
        if (!string.IsNullOrEmpty(theme) && allowedThemes.Contains(theme))
            Response.Cookies.Append("sms.theme", theme, opts);
        if (!string.IsNullOrEmpty(mode) && allowedModes.Contains(mode))
            Response.Cookies.Append("sms.mode", mode, opts);
        return Ok();
    }
}

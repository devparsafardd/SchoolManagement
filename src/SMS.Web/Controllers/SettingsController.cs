using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.AdminGroup)]
public class SettingsController : Controller
{
    private readonly ISettingService _svc;
    private readonly ICurrentUserService _currentUser;

    public SettingsController(ISettingService svc, ICurrentUserService currentUser)
    {
        _svc = svc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(string? category)
    {
        ViewBag.Category = category;
        var list = await _svc.GetByCategoryAsync(category);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, string? value, string? category)
    {
        var userId = _currentUser.UserId;
        var r = await _svc.UpdateAsync(key, value, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { category });
    }
}

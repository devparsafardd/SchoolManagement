using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

/// <summary>کنترلر مدیریت حساب‌های کاربری (برای ساخت حساب دانش‌آموز/ولی/معلم)</summary>
[Authorize(Roles = RoleNames.ManagerGroup)]
public class UserAccountsController : Controller
{
    private readonly IAccountService _svc;
    public UserAccountsController(IAccountService svc) => _svc = svc;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserAccountDto dto, string? returnUrl)
    {
        var r = await _svc.CreateAccountForPersonAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Students");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int userId, string? returnUrl)
    {
        var r = await _svc.ResetPasswordAsync(userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Students");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int userId, string? returnUrl)
    {
        var r = await _svc.ToggleLockAsync(userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Students");
    }
}

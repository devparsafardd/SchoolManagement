using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly IAccountService _accountSvc;
    private readonly SMS.Web.Services.IFileUploadService _fileUpload;
    private readonly ICurrentUserService _currentUser;

    public AccountController(IAuthService auth, IAccountService accountSvc, ICurrentUserService currentUser, SMS.Web.Services.IFileUploadService fileUpload)
    {
        _auth = auth; _accountSvc = accountSvc; _fileUpload = fileUpload; _currentUser = currentUser;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToHome();
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _auth.LoginAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Errors.FirstOrDefault() ?? "ورود ناموفق");
            return View(dto);
        }

        var data = result.Data!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, data.Username),
            new("FullName", data.FullName),
            new("UserId", data.UserId.ToString()),
            new("PersonId", data.PersonId.ToString())
        };
        foreach (var role in data.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = data.ExpiresAt });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // ریدایرکت بر اساس نقش
        if (data.Roles.Contains(RoleNames.Parent))
            return RedirectToAction("Dashboard", "Parent");
        if (data.Roles.Contains(RoleNames.Student))
            return RedirectToAction("Index", "MyPortal");
        if (data.Roles.Contains(RoleNames.Teacher) || data.Roles.Contains(RoleNames.Counselor))
            return RedirectToAction("Dashboard", "Teacher");
        if (data.Roles.Contains(RoleNames.Principal) || data.Roles.Contains(RoleNames.VicePrincipal))
            return RedirectToAction("Index", "Principal");

        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(IFormFile? photo)
    {
        if (photo == null || photo.Length == 0)
        {
            TempData["Error"] = "فایلی انتخاب نشده";
            return RedirectToAction(nameof(Profile));
        }
        var ext = System.IO.Path.GetExtension(photo.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = "فقط فرمت‌های تصویر مجاز است (jpg, png, gif, webp)";
            return RedirectToAction(nameof(Profile));
        }
        if (photo.Length > 3 * 1024 * 1024)
        {
            TempData["Error"] = "حداکثر حجم عکس ۳ مگابایت";
            return RedirectToAction(nameof(Profile));
        }
        try
        {
            var path = await _fileUpload.UploadAsync(photo, "profiles", 3 * 1024 * 1024);
            if (path == null) { TempData["Error"] = "خطا در آپلود"; return RedirectToAction(nameof(Profile)); }
            var personId = _currentUser.PersonId ?? 0;
            var r = await _accountSvc.UpdatePhotoAsync(personId, path);
            TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePhoto()
    {
        var personId = _currentUser.PersonId ?? 0;
        var r = await _accountSvc.RemovePhotoAsync(personId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = _currentUser.UserId ?? 0;
        var r = await _accountSvc.GetProfileAsync(userId);
        if (!r.Success) return RedirectToAction("Logout");
        return View(r.Data);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string? mobile, string? email)
    {
        var personId = _currentUser.PersonId ?? 0;
        var r = await _accountSvc.UpdateProfileAsync(personId, firstName, lastName, mobile, email);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordDto());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var userId = _currentUser.UserId ?? 0;
        var r = await _accountSvc.ChangePasswordAsync(userId, dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Profile));
    }

    private IActionResult RedirectToHome()
    {
        if (User.IsInRole(RoleNames.Parent))
            return RedirectToAction("Dashboard", "Parent");
        if (User.IsInRole(RoleNames.Student))
            return RedirectToAction("Index", "MyPortal");
        if (User.IsInRole(RoleNames.Teacher) || User.IsInRole(RoleNames.Counselor))
            return RedirectToAction("Dashboard", "Teacher");
        if (User.IsInRole(RoleNames.Principal) || User.IsInRole(RoleNames.VicePrincipal))
            return RedirectToAction("Index", "Principal");
        return RedirectToAction("Index", "Home");
    }
}

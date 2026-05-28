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
    private readonly ICurrentUserService _currentUser;

    public AccountController(IAuthService auth, IAccountService accountSvc, ICurrentUserService currentUser)
    {
        _auth = auth; _accountSvc = accountSvc; _currentUser = currentUser;
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
        if (data.Roles.Contains(RoleNames.Student) || data.Roles.Contains(RoleNames.Parent))
            return RedirectToAction("Index", "MyPortal");

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
        if (User.IsInRole(RoleNames.Student) || User.IsInRole(RoleNames.Parent))
            return RedirectToAction("Index", "MyPortal");
        return RedirectToAction("Index", "Home");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

/// <summary>
/// پنل اختصاصی والدین - با Layout مخصوص و داشبورد چند فرزندی
/// همه عملیات تخصصی دانش‌آموز در MyPortal انجام می‌شود (DRY)
/// </summary>
[Authorize(Roles = RoleNames.Parent)]
public class ParentController : Controller
{
    private readonly IPortalService _svc;
    private readonly ICurrentUserService _currentUser;

    public ParentController(IPortalService svc, ICurrentUserService currentUser)
    {
        _svc = svc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index() => RedirectToAction(nameof(Dashboard));

    /// <summary>داشبورد ولی - لیست همه فرزندان با کارت خلاصه</summary>
    public async Task<IActionResult> Dashboard()
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return RedirectToAction("Login", "Account");

        var guardianId = await _svc.GetGuardianIdByPersonAsync(personId.Value);
        if (guardianId is null)
        {
            TempData["Error"] = "حساب کاربری شما به‌عنوان ولی شناسایی نشد.";
            return RedirectToAction("Logout", "Account");
        }

        var children = await _svc.GetChildrenAsync(guardianId.Value);
        return View(children);
    }

    /// <summary>انتخاب یک فرزند → ریدایرکت به MyPortal</summary>
    public IActionResult View(int studentId)
    {
        return RedirectToAction("Dashboard", "MyPortal", new { studentId });
    }
}

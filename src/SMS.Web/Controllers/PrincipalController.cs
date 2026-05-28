using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

/// <summary>
/// پنل مدیر مدرسه (Principal / VicePrincipal)
/// مدیر فقط به مدرسه/مدارس خودش دسترسی داره
/// </summary>
[Authorize(Roles = RoleNames.Principal + "," + RoleNames.VicePrincipal + "," + RoleNames.SchoolAdmin)]
public class PrincipalController : Controller
{
    private readonly IPrincipalPortalService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ILookupService _lookup;
    private readonly ICurrentUserService _currentUser;

    public PrincipalController(IPrincipalPortalService svc, ISchoolService schoolSvc,
        ILookupService lookup, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _lookup = lookup; _currentUser = currentUser;
    }

    /// <summary>اگه چند مدرسه داره، اول انتخاب کنه - وگرنه مستقیم بره داشبورد</summary>
    public async Task<IActionResult> Index()
    {
        // SuperAdmin/SchoolAdmin هم می‌تونن این پنل رو ببینن - اون‌ها می‌تونن همه مدارس رو انتخاب کنن
        if (User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.SchoolAdmin))
        {
            var allSchools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
            if (allSchools.Count == 1)
                return RedirectToAction(nameof(Dashboard), new { schoolId = allSchools[0].SchoolId });
            ViewBag.Schools = allSchools;
            return View("SelectSchool");
        }

        var personId = _currentUser.PersonId;
        if (personId is null) return View("NoAccess");

        var schoolIds = await _svc.GetManagedSchoolIdsAsync(personId.Value);
        if (!schoolIds.Any())
        {
            TempData["Error"] = "شما به عنوان مدیر یا معاون به هیچ مدرسه‌ای تخصیص نیافته‌اید.";
            return View("NoAccess");
        }
        if (schoolIds.Count == 1)
            return RedirectToAction(nameof(Dashboard), new { schoolId = schoolIds[0] });

        // چند مدرسه دارد - انتخاب کند
        var schools = new List<SchoolDto>();
        foreach (var sid in schoolIds)
        {
            var s = await _schoolSvc.GetByIdAsync(sid);
            if (s.Success) schools.Add(s.Data!);
        }
        ViewBag.Schools = schools;
        return View("SelectSchool");
    }

    public async Task<IActionResult> Dashboard(int schoolId, int? academicYearId)
    {
        if (!await CanAccess(schoolId)) return Forbid();

        var r = await _svc.GetDashboardAsync(schoolId, new PrincipalFilter { AcademicYearId = academicYearId });
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return View("NoAccess"); }
        ViewBag.AcademicYears = await _lookup.GetAcademicYearsAsync();
        return View(r.Data);
    }

    public async Task<IActionResult> Classrooms(int schoolId, int? academicYearId)
    {
        if (!await CanAccess(schoolId)) return Forbid();
        ViewBag.SchoolId = schoolId;
        ViewBag.AcademicYears = await _lookup.GetAcademicYearsAsync();
        ViewBag.AcademicYearId = academicYearId;
        var list = await _svc.GetClassroomsAsync(schoolId, academicYearId);
        return View(list);
    }

    public async Task<IActionResult> Students(int schoolId)
    {
        if (!await CanAccess(schoolId)) return Forbid();
        // ریدایرکت به Students کلی با فیلتر مدرسه
        return RedirectToAction("Index", "Students", new { schoolId });
    }

    public async Task<IActionResult> Staff(int schoolId)
    {
        if (!await CanAccess(schoolId)) return Forbid();
        return RedirectToAction("Index", "Staff", new { schoolId });
    }

    public async Task<IActionResult> Attendance(int schoolId)
    {
        if (!await CanAccess(schoolId)) return Forbid();
        return RedirectToAction("Index", "Attendance", new { schoolId });
    }

    public async Task<IActionResult> Finance(int schoolId)
    {
        if (!await CanAccess(schoolId)) return Forbid();
        return RedirectToAction("Index", "Finance", new { schoolId });
    }

    public async Task<IActionResult> Reports(int schoolId)
    {
        if (!await CanAccess(schoolId)) return Forbid();
        return RedirectToAction("Index", "Reports", new { schoolId });
    }

    // ---- helpers ----
    private async Task<bool> CanAccess(int schoolId)
    {
        if (User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.SchoolAdmin)) return true;
        var personId = _currentUser.PersonId;
        if (personId is null) return false;
        return await _svc.CanAccessSchoolAsync(personId.Value, schoolId);
    }
}

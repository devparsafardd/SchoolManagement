using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

/// <summary>
/// گزارش‌گیری و آمار جامع - برای مدیران (سراسری یا یک مدرسه)
/// </summary>
[Authorize]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ILookupService _lookup;
    private readonly ITeacherPortalService _teacher;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsController(IAnalyticsService svc, ISchoolService schoolSvc, ILookupService lookup,
        ITeacherPortalService teacher, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _lookup = lookup;
        _teacher = teacher; _currentUser = currentUser;
    }

    /// <summary>چک می‌کند اگر کاربر فقط معلم است، به کلاس خودش دسترسی دارد یا نه</summary>
    private async Task<bool> IsManagerOrOwnsClassroomAsync(int classroomId)
    {
        if (User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.SchoolAdmin)
            || User.IsInRole(RoleNames.Principal) || User.IsInRole(RoleNames.VicePrincipal))
            return true;
        var personId = _currentUser.PersonId;
        if (personId is null) return false;
        var staffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
        if (staffId is null) return false;
        return await _teacher.OwnsClassroomAsync(staffId.Value, classroomId);
    }

    [Authorize(Roles = RoleNames.EducatorGroup)]
    public IActionResult Index() => View();

    [Authorize(Roles = RoleNames.EducatorGroup)]
    public async Task<IActionResult> Attendance(int? schoolId, string? fromDate, string? toDate)
    {
        var from = string.IsNullOrEmpty(fromDate) ? DateTime.Today.AddDays(-30) : PersianDate.FromPersian(fromDate) ?? DateTime.Today.AddDays(-30);
        var to = string.IsNullOrEmpty(toDate) ? DateTime.Today : PersianDate.FromPersian(toDate) ?? DateTime.Today;

        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.FromDate = PersianDate.ToPersian(from);
        ViewBag.ToDate = PersianDate.ToPersian(to);

        var data = await _svc.GetAttendanceAnalyticsAsync(schoolId, from, to);
        return View(data);
    }

    [Authorize(Roles = RoleNames.EducatorGroup)]
    public async Task<IActionResult> Academic(int? schoolId, int? termId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;

        var data = await _svc.GetAcademicAnalyticsAsync(schoolId, termId);
        return View(data);
    }

    [Authorize(Roles = RoleNames.EducatorGroup)]
    public async Task<IActionResult> Financial(int? schoolId, int? academicYearId, string? fromDate, string? toDate)
    {
        var from = string.IsNullOrEmpty(fromDate) ? DateTime.Today.AddMonths(-6) : PersianDate.FromPersian(fromDate) ?? DateTime.Today.AddMonths(-6);
        var to = string.IsNullOrEmpty(toDate) ? DateTime.Today : PersianDate.FromPersian(toDate) ?? DateTime.Today;

        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.AcademicYears = await _lookup.GetAcademicYearsAsync();
        ViewBag.AcademicYearId = academicYearId;
        ViewBag.FromDate = PersianDate.ToPersian(from);
        ViewBag.ToDate = PersianDate.ToPersian(to);

        var data = await _svc.GetFinancialAnalyticsAsync(schoolId, academicYearId, from, to);
        return View(data);
    }

    [Authorize(Roles = RoleNames.EducatorGroup)]
    public async Task<IActionResult> Discipline(int? schoolId, string? fromDate, string? toDate)
    {
        var from = string.IsNullOrEmpty(fromDate) ? DateTime.Today.AddMonths(-3) : PersianDate.FromPersian(fromDate) ?? DateTime.Today.AddMonths(-3);
        var to = string.IsNullOrEmpty(toDate) ? DateTime.Today : PersianDate.FromPersian(toDate) ?? DateTime.Today;

        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.FromDate = PersianDate.ToPersian(from);
        ViewBag.ToDate = PersianDate.ToPersian(to);

        var data = await _svc.GetDisciplineAnalyticsAsync(schoolId, from, to);
        return View(data);
    }
    [Authorize(Roles = RoleNames.EducatorGroup)]
    public async Task<IActionResult> Classroom(int id, int? termId)
    {
        if (!await IsManagerOrOwnsClassroomAsync(id)) return Forbid();
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;
        var r = await _svc.GetClassroomAnalyticsAsync(id, termId);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        return View(r.Data);
    }

    [Authorize(Roles = RoleNames.EducatorGroup)]
    public async Task<IActionResult> Teacher(int id, int? termId)
    {
        // فقط مدیر یا خود معلم
        var canSee = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.SchoolAdmin)
            || User.IsInRole(RoleNames.Principal) || User.IsInRole(RoleNames.VicePrincipal);
        if (!canSee)
        {
            var personId = _currentUser.PersonId;
            if (personId is null) return Forbid();
            var myStaffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
            if (myStaffId != id) return Forbid();
        }
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;
        var r = await _svc.GetTeacherAnalyticsAsync(id, termId);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        return View(r.Data);
    }

    public async Task<IActionResult> Student(int id, int? termId, [FromServices] IPortalService portal)
    {
        // چک دسترسی:
        // - مدیر/معلم: همه
        // - ولی: فقط فرزندان خودش
        // - دانش‌آموز: فقط خودش
        var personId = _currentUser.PersonId;
        var isManager = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.SchoolAdmin)
            || User.IsInRole(RoleNames.Principal) || User.IsInRole(RoleNames.VicePrincipal)
            || User.IsInRole(RoleNames.Teacher) || User.IsInRole(RoleNames.Counselor);
        if (!isManager)
        {
            if (personId is null) return Forbid();
            var canAccess = await portal.CanAccessStudentAsync(personId.Value, id);
            if (!canAccess) return Forbid();
        }

        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;
        var r = await _svc.GetStudentAnalyticsAsync(id, termId);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        return View(r.Data);
    }
}

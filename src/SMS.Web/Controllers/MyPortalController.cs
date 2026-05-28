using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

/// <summary>
/// پنل دانش‌آموز و اولیا - فقط برای دسترسی به اطلاعات خود/فرزند
/// </summary>
[Authorize(Roles = RoleNames.Student + "," + RoleNames.Parent)]
public class MyPortalController : Controller
{
    private readonly IPortalService _svc;
    private readonly IReportCardService _reportSvc;
    private readonly ILookupService _lookup;
    private readonly ICurrentUserService _currentUser;

    public MyPortalController(IPortalService svc, IReportCardService reportSvc,
        ILookupService lookup, ICurrentUserService currentUser)
    {
        _svc = svc; _reportSvc = reportSvc; _lookup = lookup; _currentUser = currentUser;
    }

    /// <summary>صفحه اول: لیست فرزندان (ولی) یا انتقال مستقیم به داشبورد (دانش‌آموز)</summary>
    public async Task<IActionResult> Index()
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return RedirectToAction("Login", "Account");

        // اگر خود دانش‌آموز است
        var studentId = await _svc.GetStudentIdByPersonAsync(personId.Value);
        if (studentId.HasValue)
            return RedirectToAction(nameof(Dashboard), new { studentId });

        // اگر ولی است
        var guardianId = await _svc.GetGuardianIdByPersonAsync(personId.Value);
        if (guardianId.HasValue)
        {
            var children = await _svc.GetChildrenAsync(guardianId.Value);
            if (children.Count == 1)
                return RedirectToAction(nameof(Dashboard), new { studentId = children[0].StudentId });
            return View("MyChildren", children);
        }

        TempData["Error"] = "حساب کاربری شما به دانش‌آموز یا فرزندی متصل نیست. با مدرسه تماس بگیرید.";
        return View("NoAccess");
    }

    public async Task<IActionResult> Dashboard(int studentId, int? termId)
    {
        if (!await CheckAccess(studentId)) return Forbid();

        var r = await _svc.GetSummaryAsync(studentId, termId);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        ViewBag.StudentId = studentId;
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;
        return View(r.Data);
    }

    public async Task<IActionResult> Scores(int studentId, int? termId)
    {
        if (!await CheckAccess(studentId)) return Forbid();
        var scores = await _svc.GetScoresAsync(studentId, termId);
        ViewBag.StudentId = studentId;
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;
        return View(scores);
    }

    public async Task<IActionResult> Attendance(int studentId, string? fromDate, string? toDate)
    {
        if (!await CheckAccess(studentId)) return Forbid();

        var from = string.IsNullOrEmpty(fromDate) ? DateTime.Today.AddDays(-30) : PersianDate.FromPersian(fromDate) ?? DateTime.Today.AddDays(-30);
        var to = string.IsNullOrEmpty(toDate) ? DateTime.Today : PersianDate.FromPersian(toDate) ?? DateTime.Today;

        var list = await _svc.GetAttendanceAsync(studentId, from, to);
        ViewBag.StudentId = studentId;
        ViewBag.FromDate = PersianDate.ToPersian(from);
        ViewBag.ToDate = PersianDate.ToPersian(to);
        return View(list);
    }

    public async Task<IActionResult> Discipline(int studentId)
    {
        if (!await CheckAccess(studentId)) return Forbid();
        ViewBag.StudentId = studentId;
        return View(await _svc.GetDisciplinaryRecordsAsync(studentId));
    }

    public async Task<IActionResult> Finance(int studentId)
    {
        if (!await CheckAccess(studentId)) return Forbid();
        ViewBag.StudentId = studentId;
        return View(await _svc.GetInvoicesAsync(studentId));
    }

    public async Task<IActionResult> Announcements(int studentId)
    {
        if (!await CheckAccess(studentId)) return Forbid();
        var audience = User.IsInRole(RoleNames.Parent) ? "Parents" : "Students";
        ViewBag.StudentId = studentId;
        return View(await _svc.GetAnnouncementsAsync(studentId, audience));
    }

    public async Task<IActionResult> ReportCard(int studentId, int termId)
    {
        if (!await CheckAccess(studentId)) return Forbid();
        var r = await _reportSvc.GenerateAsync(studentId, termId);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Dashboard), new { studentId }); }
        return View(r.Data);
    }

    public async Task<IActionResult> ReportCardPdf(int studentId, int termId)
    {
        if (!await CheckAccess(studentId)) return Forbid();
        var r = await _reportSvc.GenerateAsync(studentId, termId);
        if (!r.Success) return NotFound();
        var bytes = await _reportSvc.ExportToPdfAsync(r.Data!);
        return File(bytes, "application/pdf", $"ReportCard-{r.Data!.StudentCode}.pdf");
    }

    /// <summary>تغییر فرزند (برای ولی با چند فرزند)</summary>
    public async Task<IActionResult> SwitchChild()
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return RedirectToAction("Login", "Account");

        var guardianId = await _svc.GetGuardianIdByPersonAsync(personId.Value);
        if (guardianId is null) return RedirectToAction(nameof(Index));

        var children = await _svc.GetChildrenAsync(guardianId.Value);
        return View("MyChildren", children);
    }

    private async Task<bool> CheckAccess(int studentId)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return false;
        return await _svc.CanAccessStudentAsync(personId.Value, studentId);
    }
}

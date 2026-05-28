using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

/// <summary>
/// پنل اختصاصی معلم - فقط نقش Teacher (یا EducatorGroup که مدیر مدرسه را هم شامل می‌شود)
/// همه عملیات از طریق TeacherPortalService محدود به StaffId کاربر جاری است
/// </summary>
[Authorize(Roles = RoleNames.Teacher + "," + RoleNames.Counselor)]
public class TeacherController : Controller
{
    private readonly ITeacherPortalService _svc;
    private readonly IAttendanceService _attSvc;
    private readonly IExamService _examSvc;
    private readonly ILookupService _lookup;
    private readonly ICurrentUserService _currentUser;
    private readonly IAnalyticsService _analytics;

    public TeacherController(ITeacherPortalService svc, IAttendanceService attSvc,
        IExamService examSvc, ILookupService lookup, ICurrentUserService currentUser,
        IAnalyticsService analytics)
    {
        _svc = svc; _attSvc = attSvc; _examSvc = examSvc;
        _lookup = lookup; _currentUser = currentUser; _analytics = analytics;
    }

    private async Task<int?> ResolveStaffIdAsync()
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return null;
        return await _svc.GetStaffIdByPersonAsync(personId.Value);
    }

    // -------- داشبورد --------
    public async Task<IActionResult> Index() => RedirectToAction(nameof(Dashboard));

    public async Task<IActionResult> Dashboard()
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null)
        {
            TempData["Error"] = "حساب کاربری شما به یک کارمند متصل نیست. با مدیریت تماس بگیرید.";
            return View("NoAccess");
        }

        var r = await _svc.GetDashboardAsync(staffId.Value);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return View("NoAccess"); }
        return View(r.Data);
    }

    // -------- کلاس‌های من --------
    public async Task<IActionResult> MyClasses()
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return View("NoAccess");

        var list = await _svc.GetMyClassesAsync(staffId.Value);
        return View(list);
    }

    public async Task<IActionResult> ClassDetail(int id)   // id = ClassSubjectId
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return View("NoAccess");

        var r = await _svc.GetClassDetailAsync(staffId.Value, id);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(MyClasses)); }
        return View(r.Data);
    }

    // -------- حضور و غیاب (Shortcut به AttendanceController با چک دسترسی) --------
    public async Task<IActionResult> Attendance(int classSubjectId, string? date)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null || !await _svc.OwnsClassSubjectAsync(staffId.Value, classSubjectId)) return Forbid();

        // دریافت classroomId از classSubjectId
        var detail = await _svc.GetClassDetailAsync(staffId.Value, classSubjectId);
        if (!detail.Success) return NotFound();

        var dt = string.IsNullOrEmpty(date) ? DateTime.Today : PersianDate.FromPersian(date) ?? DateTime.Today;
        var dto = await _attSvc.GetForDateAsync(detail.Data!.ClassroomId, dt, classSubjectId);
        ViewBag.Statuses = await _attSvc.GetStatusesAsync();
        ViewBag.PersianDate = PersianDate.ToPersian(dt);
        ViewBag.ClassSubjectId = classSubjectId;
        ViewBag.ClassName = detail.Data.ClassroomName + " - " + detail.Data.SubjectName;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttendance(TakeAttendanceDto dto)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return Forbid();

        if (dto.ClassSubjectId.HasValue && !await _svc.OwnsClassSubjectAsync(staffId.Value, dto.ClassSubjectId.Value))
            return Forbid();
        if (!await _svc.OwnsClassroomAsync(staffId.Value, dto.ClassroomId))
            return Forbid();

        var r = await _attSvc.SaveAsync(dto, staffId.Value);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Attendance), new
        {
            classSubjectId = dto.ClassSubjectId,
            date = PersianDate.ToPersian(dto.AttendanceDate)
        });
    }

    // -------- آزمون‌ها --------
    public async Task<IActionResult> Exams(int classSubjectId)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null || !await _svc.OwnsClassSubjectAsync(staffId.Value, classSubjectId)) return Forbid();

        ViewBag.ClassSubjectId = classSubjectId;
        var list = await _examSvc.GetByClassSubjectAsync(classSubjectId);
        return View(list);
    }

    public async Task<IActionResult> CreateExam(int classSubjectId)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null || !await _svc.OwnsClassSubjectAsync(staffId.Value, classSubjectId)) return Forbid();

        ViewBag.ClassSubjectId = classSubjectId;
        ViewBag.ExamTypes = await _examSvc.GetExamTypesAsync();
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.GradeScales = await _examSvc.GetGradeScalesAsync();
        return View(new ExamCreateDto { ClassSubjectId = classSubjectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExam(ExamCreateDto dto)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null || !await _svc.OwnsClassSubjectAsync(staffId.Value, dto.ClassSubjectId)) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.ExamTypes = await _examSvc.GetExamTypesAsync();
            ViewBag.Terms = await _lookup.GetTermsAsync();
            ViewBag.GradeScales = await _examSvc.GetGradeScalesAsync();
            return View(dto);
        }
        var r = await _examSvc.CreateAsync(dto, staffId.Value);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            ViewBag.ExamTypes = await _examSvc.GetExamTypesAsync();
            ViewBag.Terms = await _lookup.GetTermsAsync();
            ViewBag.GradeScales = await _examSvc.GetGradeScalesAsync();
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(ExamScores), new { examId = r.Data });
    }

    public async Task<IActionResult> ExamScores(long examId)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return Forbid();

        var exam = await _examSvc.GetByIdAsync(examId);
        if (!exam.Success) return NotFound();

        if (!await _svc.OwnsClassSubjectAsync(staffId.Value, exam.Data!.ClassSubjectId)) return Forbid();

        ViewBag.Exam = exam.Data;
        ViewBag.GradeScales = await _examSvc.GetGradeScalesAsync();
        var scores = await _examSvc.GetScoresAsync(examId);
        return View(new EnterScoresDto { ExamId = examId, Scores = scores });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExamScores(EnterScoresDto dto)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return Forbid();

        var exam = await _examSvc.GetByIdAsync(dto.ExamId);
        if (!exam.Success) return NotFound();
        if (!await _svc.OwnsClassSubjectAsync(staffId.Value, exam.Data!.ClassSubjectId)) return Forbid();

        var r = await _examSvc.SaveScoresAsync(dto, staffId.Value);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(ExamScores), new { examId = dto.ExamId });
    }

    // -------- برنامه هفتگی --------
    public async Task<IActionResult> Schedule()
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return View("NoAccess");

        var weekly = await _svc.GetWeeklyScheduleAsync(staffId.Value);
        return View(weekly);
    }

    // -------- لیست دانش‌آموزان یک کلاس --------
    public async Task<IActionResult> Students(int classSubjectId)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null || !await _svc.OwnsClassSubjectAsync(staffId.Value, classSubjectId)) return Forbid();

        ViewBag.ClassSubjectId = classSubjectId;
        var list = await _svc.GetClassStudentsAsync(staffId.Value, classSubjectId);
        return View(list);
    }
    // -------- گزارش جامع عملکرد خود معلم --------
    public async Task<IActionResult> MyReport(int? termId)
    {
        var staffId = await ResolveStaffIdAsync();
        if (staffId is null) return View("NoAccess");

        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;
        var r = await _analytics.GetTeacherAnalyticsAsync(staffId.Value, termId);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Dashboard)); }
        return View(r.Data);
    }
}

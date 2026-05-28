using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

/// <summary>
/// مدیریت تکالیف - معلم ایجاد/تصحیح می‌کند، دانش‌آموز تحویل می‌دهد
/// </summary>
[Authorize]
public class HomeworksController : Controller
{
    private readonly IHomeworkService _svc;
    private readonly ITeacherPortalService _teacher;
    private readonly IPortalService _portal;
    private readonly ICurrentUserService _currentUser;
    private readonly SMS.Web.Services.IFileUploadService _fileUpload;

    public HomeworksController(IHomeworkService svc, ITeacherPortalService teacher,
        IPortalService portal, ICurrentUserService currentUser,
        SMS.Web.Services.IFileUploadService fileUpload)
    {
        _svc = svc; _teacher = teacher; _portal = portal; _currentUser = currentUser;
        _fileUpload = fileUpload;
    }

    // ===== معلم =====
    [Authorize(Roles = RoleNames.Teacher + "," + RoleNames.Counselor)]
    public async Task<IActionResult> Teacher(int? classSubjectId)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var staffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
        if (staffId is null) return Forbid();

        if (classSubjectId.HasValue)
        {
            if (!await _teacher.OwnsClassSubjectAsync(staffId.Value, classSubjectId.Value)) return Forbid();
            ViewBag.ClassSubjectId = classSubjectId;
            var list = await _svc.GetByClassSubjectAsync(classSubjectId.Value);
            return View("TeacherList", list);
        }
        else
        {
            var list = await _svc.GetByTeacherAsync(staffId.Value, true);
            return View("TeacherList", list);
        }
    }

    [Authorize(Roles = RoleNames.Teacher + "," + RoleNames.Counselor)]
    public async Task<IActionResult> Create(int classSubjectId)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var staffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
        if (staffId is null || !await _teacher.OwnsClassSubjectAsync(staffId.Value, classSubjectId)) return Forbid();

        return View(new HomeworkCreateDto { ClassSubjectId = classSubjectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Teacher + "," + RoleNames.Counselor)]
    public async Task<IActionResult> Create(HomeworkCreateDto dto, IFormFile? attachment)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var staffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
        if (staffId is null) return Forbid();

        if (!ModelState.IsValid) return View(dto);

        // آپلود فایل ضمیمه معلم
        if (attachment != null && attachment.Length > 0)
        {
            try { dto.AttachmentPath = await _fileUpload.UploadAsync(attachment, $"homework_{dto.ClassSubjectId}"); }
            catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(dto); }
        }

        var r = await _svc.CreateAsync(dto, staffId.Value);
        if (!r.Success) { ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? ""); return View(dto); }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Submissions), new { id = r.Data });
    }

    [Authorize(Roles = RoleNames.Teacher + "," + RoleNames.Counselor)]
    public async Task<IActionResult> Submissions(long id)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var staffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
        if (staffId is null) return Forbid();

        var hw = await _svc.GetByIdAsync(id);
        if (!hw.Success) return NotFound();
        if (!await _teacher.OwnsClassSubjectAsync(staffId.Value, hw.Data!.ClassSubjectId)) return Forbid();

        ViewBag.Homework = hw.Data;
        var subs = await _svc.GetSubmissionsAsync(id);
        return View(subs);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Teacher + "," + RoleNames.Counselor)]
    public async Task<IActionResult> Grade(HomeworkGradeDto dto, long homeworkId)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var staffId = await _teacher.GetStaffIdByPersonAsync(personId.Value);
        if (staffId is null) return Forbid();

        var r = await _svc.GradeSubmissionAsync(dto, staffId.Value);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Submissions), new { id = homeworkId });
    }

    // ===== دانش‌آموز =====
    [Authorize(Roles = RoleNames.Student)]
    public async Task<IActionResult> Student()
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var studentId = await _portal.GetStudentIdByPersonAsync(personId.Value);
        if (studentId is null) return Forbid();

        var list = await _svc.GetForStudentAsync(studentId.Value);
        ViewBag.StudentId = studentId;
        return View("StudentList", list);
    }

    [Authorize(Roles = RoleNames.Student)]
    public async Task<IActionResult> Submit(long id)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var studentId = await _portal.GetStudentIdByPersonAsync(personId.Value);
        if (studentId is null) return Forbid();

        var hw = await _svc.GetByIdAsync(id);
        if (!hw.Success) return NotFound();
        var sub = await _svc.GetSubmissionAsync(id, studentId.Value);
        ViewBag.Homework = hw.Data;
        ViewBag.StudentId = studentId.Value;
        return View(sub.Data ?? new HomeworkSubmissionDto { HomeworkId = id, StudentId = studentId.Value });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Student)]
    public async Task<IActionResult> Submit(HomeworkSubmitDto dto, IFormFile? attachment)
    {
        var personId = _currentUser.PersonId;
        if (personId is null) return Forbid();
        var studentId = await _portal.GetStudentIdByPersonAsync(personId.Value);
        if (studentId is null || studentId.Value != dto.StudentId) return Forbid();

        // آپلود فایل ضمیمه دانش‌آموز
        if (attachment != null && attachment.Length > 0)
        {
            try { dto.AttachmentPath = await _fileUpload.UploadAsync(attachment, $"submissions_{dto.HomeworkId}"); }
            catch (Exception ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Submit), new { id = dto.HomeworkId }); }
        }

        var r = await _svc.SubmitAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Student));
    }
}

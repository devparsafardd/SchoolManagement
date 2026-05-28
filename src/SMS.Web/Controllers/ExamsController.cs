using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup + "," + RoleNames.Teacher)]
public class ExamsController : Controller
{
    private readonly IExamService _svc;
    private readonly ISubjectService _subjectSvc;
    private readonly IClassroomService _classSvc;
    private readonly ISchoolService _schoolSvc;
    private readonly ILookupService _lookup;
    private readonly ICurrentUserService _currentUser;

    public ExamsController(IExamService svc, ISubjectService subjectSvc, IClassroomService classSvc,
        ISchoolService schoolSvc, ILookupService lookup, ICurrentUserService currentUser)
    {
        _svc = svc; _subjectSvc = subjectSvc; _classSvc = classSvc;
        _schoolSvc = schoolSvc; _lookup = lookup; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int classSubjectId)
    {
        var list = await _svc.GetByClassSubjectAsync(classSubjectId);
        ViewBag.ClassSubjectId = classSubjectId;
        return View(list);
    }

    public async Task<IActionResult> Create(int classSubjectId)
    {
        ViewBag.ClassSubjectId = classSubjectId;
        ViewBag.ExamTypes = await _svc.GetExamTypesAsync();
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.GradeScales = await _svc.GetGradeScalesAsync();
        return View(new ExamCreateDto { ClassSubjectId = classSubjectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ExamTypes = await _svc.GetExamTypesAsync();
            ViewBag.Terms = await _lookup.GetTermsAsync();
            ViewBag.GradeScales = await _svc.GetGradeScalesAsync();
            return View(dto);
        }

        var userId = _currentUser.UserId ?? 0;
        var r = await _svc.CreateAsync(dto, userId);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            ViewBag.ExamTypes = await _svc.GetExamTypesAsync();
            ViewBag.Terms = await _lookup.GetTermsAsync();
            ViewBag.GradeScales = await _svc.GetGradeScalesAsync();
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Scores), new { id = r.Data });
    }

    public async Task<IActionResult> Scores(long id)
    {
        var exam = await _svc.GetByIdAsync(id);
        if (!exam.Success) return NotFound();
        ViewBag.Exam = exam.Data;
        ViewBag.GradeScales = await _svc.GetGradeScalesAsync();

        var scores = await _svc.GetScoresAsync(id);
        return View(new EnterScoresDto { ExamId = id, Scores = scores });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Scores(EnterScoresDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        var r = await _svc.SaveScoresAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Scores), new { id = dto.ExamId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(long id, int? classSubjectId)
    {
        var r = await _svc.FinalizeAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        if (classSubjectId.HasValue)
            return RedirectToAction(nameof(Index), new { classSubjectId });
        return RedirectToAction(nameof(Scores), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, int classSubjectId)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { classSubjectId });
    }

    // صفحه شروع: انتخاب کلاس و درس
    public async Task<IActionResult> Browse(int? schoolId, int? classroomId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.ClassroomId = classroomId;

        if (schoolId.HasValue)
            ViewBag.Classrooms = await _classSvc.GetBySchoolAsync(schoolId.Value);
        else
            ViewBag.Classrooms = new List<ClassroomDto>();

        if (classroomId.HasValue)
            ViewBag.ClassSubjects = await _subjectSvc.GetByClassroomAsync(classroomId.Value);
        else
            ViewBag.ClassSubjects = new List<ClassSubjectTeacherDto>();

        return View();
    }
}

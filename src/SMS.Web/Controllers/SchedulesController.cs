using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

/// <summary>مدیریت برنامه هفتگی و زنگ‌های مدرسه</summary>
[Authorize(Roles = RoleNames.ManagerGroup)]
public class SchedulesController : Controller
{
    private readonly IScheduleService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly IClassroomService _classSvc;
    private readonly ISubjectService _subjectSvc;

    public SchedulesController(IScheduleService svc, ISchoolService schoolSvc,
        IClassroomService classSvc, ISubjectService subjectSvc)
    {
        _svc = svc; _schoolSvc = schoolSvc; _classSvc = classSvc; _subjectSvc = subjectSvc;
    }

    public async Task<IActionResult> Index()
    {
        var classrooms = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        var all = new List<SMS.Application.DTOs.ClassroomDto>();
        foreach (var s in classrooms)
        {
            var list = await _classSvc.GetBySchoolAsync(s.SchoolId);
            all.AddRange(list);
        }
        return View(all);
    }

    public async Task<IActionResult> Periods(int? schoolId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        if (schoolId.HasValue)
        {
            var list = await _svc.GetPeriodsAsync(schoolId.Value);
            return View(list);
        }
        return View(new List<SchoolPeriodDto>());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePeriod(SchoolPeriodCreateDto dto)
    {
        var r = await _svc.CreatePeriodAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Periods), new { schoolId = dto.SchoolId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePeriod(int id, int schoolId)
    {
        var r = await _svc.DeletePeriodAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Periods), new { schoolId });
    }

    public async Task<IActionResult> Classroom(int classroomId)
    {
        var cls = await _classSvc.GetByIdAsync(classroomId);
        if (!cls.Success) return NotFound();
        ViewBag.Classroom = cls.Data;
        ViewBag.Periods = await _svc.GetPeriodsAsync(cls.Data!.SchoolId);
        ViewBag.Subjects = await _subjectSvc.GetByClassroomAsync(classroomId);
        var list = await _svc.GetClassroomScheduleAsync(classroomId);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(ClassScheduleCreateDto dto)
    {
        var r = await _svc.AssignScheduleAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Classroom), new { classroomId = dto.ClassroomId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, int classroomId)
    {
        var r = await _svc.RemoveScheduleAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Classroom), new { classroomId });
    }
}

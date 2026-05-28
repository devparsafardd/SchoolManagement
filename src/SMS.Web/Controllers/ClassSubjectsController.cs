using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class ClassSubjectsController : Controller
{
    private readonly ISubjectService _svc;
    private readonly IClassroomService _classSvc;
    private readonly IStaffService _staffSvc;

    public ClassSubjectsController(ISubjectService svc, IClassroomService classSvc, IStaffService staffSvc)
    {
        _svc = svc; _classSvc = classSvc; _staffSvc = staffSvc;
    }

    public async Task<IActionResult> Index(int classroomId)
    {
        var cls = await _classSvc.GetByIdAsync(classroomId);
        if (!cls.Success) return NotFound();

        ViewBag.Classroom = cls.Data;
        ViewBag.GradeSubjects = await _svc.GetByGradeAsync(cls.Data!.GradeId);
        ViewBag.Teachers = await _staffSvc.GetTeachersBySchoolAsync(cls.Data.SchoolId, cls.Data.AcademicYearId);

        var list = await _svc.GetByClassroomAsync(classroomId);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignTeacherDto dto)
    {
        var r = await _svc.AssignTeacherAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { classroomId = dto.ClassroomId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int classSubjectId, int classroomId)
    {
        var r = await _svc.UnassignTeacherAsync(classSubjectId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { classroomId });
    }
}

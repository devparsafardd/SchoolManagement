using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup + "," + RoleNames.Teacher)]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly IClassroomService _classSvc;
    private readonly ICurrentUserService _currentUser;

    public AttendanceController(IAttendanceService svc, ISchoolService schoolSvc, IClassroomService classSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _classSvc = classSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? schoolId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        if (schoolId.HasValue)
            ViewBag.Classrooms = await _classSvc.GetBySchoolAsync(schoolId.Value);
        else
            ViewBag.Classrooms = new List<ClassroomDto>();
        return View();
    }

    public async Task<IActionResult> Take(int classroomId, string? date)
    {
        var dt = string.IsNullOrEmpty(date) ? DateTime.Today : PersianDate.FromPersian(date) ?? DateTime.Today;
        var dto = await _svc.GetForDateAsync(classroomId, dt);
        ViewBag.Statuses = await _svc.GetStatusesAsync();
        ViewBag.PersianDate = PersianDate.ToPersian(dt);
        var cls = await _classSvc.GetByIdAsync(classroomId);
        ViewBag.Classroom = cls.Data;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(TakeAttendanceDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        var r = await _svc.SaveAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Take), new
        {
            classroomId = dto.ClassroomId,
            date = PersianDate.ToPersian(dto.AttendanceDate)
        });
    }

    public async Task<IActionResult> Report(int classroomId, string? fromDate, string? toDate)
    {
        var from = string.IsNullOrEmpty(fromDate) ? DateTime.Today.AddDays(-30) : PersianDate.FromPersian(fromDate) ?? DateTime.Today.AddDays(-30);
        var to = string.IsNullOrEmpty(toDate) ? DateTime.Today : PersianDate.FromPersian(toDate) ?? DateTime.Today;

        var list = await _svc.GetReportAsync(classroomId, from, to);
        ViewBag.FromDate = PersianDate.ToPersian(from);
        ViewBag.ToDate = PersianDate.ToPersian(to);
        var cls = await _classSvc.GetByIdAsync(classroomId);
        ViewBag.Classroom = cls.Data;
        return View(list);
    }
}

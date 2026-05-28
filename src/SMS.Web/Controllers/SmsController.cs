using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class SmsController : Controller
{
    private readonly ISmsLogService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly IClassroomService _classSvc;
    private readonly ICurrentUserService _currentUser;

    public SmsController(ISmsLogService svc, ISchoolService schoolSvc, IClassroomService classSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _classSvc = classSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var r = await _svc.GetPagedAsync(page, 50);
        return View(r);
    }

    public IActionResult Send()
    {
        return View(new SendSmsDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendSmsDto dto)
    {
        var userId = _currentUser.UserId;
        var r = await _svc.SendAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> SendBulk(int? schoolId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.Classrooms = schoolId.HasValue ? await _classSvc.GetBySchoolAsync(schoolId.Value) : new List<ClassroomDto>();
        return View(new SendBulkSmsDto { SchoolId = schoolId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendBulk(SendBulkSmsDto dto)
    {
        var userId = _currentUser.UserId;
        var r = await _svc.SendBulkAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

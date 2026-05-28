using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ICalendarService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ICurrentUserService _currentUser;

    public CalendarController(ICalendarService svc, ISchoolService schoolSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? schoolId, int? month)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        var today = DateTime.Today;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        if (month.HasValue) firstOfMonth = firstOfMonth.AddMonths(month.Value);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        ViewBag.Month = month ?? 0;
        ViewBag.MonthLabel = firstOfMonth.ToString("yyyy/MM");

        var events = await _svc.GetEventsAsync(firstOfMonth.AddDays(-7), lastOfMonth.AddDays(7), schoolId);
        ViewBag.UpcomingEvents = await _svc.GetUpcomingAsync(30, schoolId);
        return View(events);
    }

    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        return View(new CalendarEventCreateDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Create(CalendarEventCreateDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        var r = await _svc.CreateAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class AnnouncementsController : Controller
{
    private readonly IAnnouncementService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ICurrentUserService _currentUser;

    public AnnouncementsController(IAnnouncementService svc, ISchoolService schoolSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? schoolId, int page = 1)
    {
        var r = await _svc.GetPagedAsync(schoolId, page, 20);
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        return View(r);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        return View(new AnnouncementCreateDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementCreateDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        var r = await _svc.CreateAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

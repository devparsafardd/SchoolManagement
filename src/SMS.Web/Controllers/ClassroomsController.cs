using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class ClassroomsController : Controller
{
    private readonly IClassroomService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ILookupService _lookup;

    public ClassroomsController(IClassroomService svc, ISchoolService schoolSvc, ILookupService lookup)
    {
        _svc = svc; _schoolSvc = schoolSvc; _lookup = lookup;
    }

    public async Task<IActionResult> Index(int? schoolId)
    {
        var schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.Schools = schools;
        ViewBag.SchoolId = schoolId ?? schools.FirstOrDefault()?.SchoolId;

        if (ViewBag.SchoolId is int sid)
            return View(await _svc.GetBySchoolAsync(sid));

        return View(new List<ClassroomDto>());
    }

    public async Task<IActionResult> Create(int? schoolId)
    {
        await FillLookups(schoolId);
        return View(new ClassroomCreateDto { SchoolId = schoolId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassroomCreateDto dto)
    {
        if (!ModelState.IsValid) { await FillLookups(dto.SchoolId); return View(dto); }
        var r = await _svc.CreateAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            await FillLookups(dto.SchoolId);
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Index), new { schoolId = dto.SchoolId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int schoolId)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { schoolId });
    }

    private async Task FillLookups(int? schoolId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.Grades = await _lookup.GetGradesAsync();
        ViewBag.AcademicYears = await _lookup.GetAcademicYearsAsync();
        ViewBag.ActiveYear = await _lookup.GetActiveAcademicYearAsync();
    }
}

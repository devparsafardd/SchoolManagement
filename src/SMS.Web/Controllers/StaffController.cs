using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class StaffController : Controller
{
    private readonly IStaffService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ILookupService _lookup;

    public StaffController(IStaffService svc, ISchoolService schoolSvc, ILookupService lookup)
    {
        _svc = svc; _schoolSvc = schoolSvc; _lookup = lookup;
    }

    public async Task<IActionResult> Index(int? schoolId, string? position, string? search, int page = 1)
    {
        var result = await _svc.GetPagedAsync(schoolId, position, search, page, 20);
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.Position = position;
        ViewBag.Search = search;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success) return NotFound();
        ViewBag.Assignments = await _svc.GetAssignmentsAsync(id);
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.AcademicYears = await _lookup.GetAcademicYearsAsync();
        return View(r.Data);
    }

    public IActionResult Create() => View(new StaffCreateDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var r = await _svc.CreateAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Details), new { id = r.Data });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success) return NotFound();
        var d = r.Data!;
        return View(new StaffUpdateDto
        {
            StaffId = d.StaffId, PersonId = d.PersonId,
            FirstName = d.FirstName, LastName = d.LastName,
            NationalCode = d.NationalCode, Gender = d.Gender,
            Mobile = d.Mobile, Email = d.Email,
            PersonnelCode = d.PersonnelCode, EmploymentType = d.EmploymentType,
            Degree = d.Degree, FieldOfStudy = d.FieldOfStudy,
            HireDate = d.HireDate, IBAN = d.IBAN, IsActive = d.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StaffUpdateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var r = await _svc.UpdateAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(StaffAssignmentCreateDto dto)
    {
        var r = await _svc.AddAssignmentAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id = dto.StaffId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAssignment(int assignmentId, int staffId)
    {
        var r = await _svc.RemoveAssignmentAsync(assignmentId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id = staffId });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup + "," + RoleNames.Teacher)]
public class StudentsController : Controller
{
    private readonly IStudentService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly IClassroomService _classSvc;
    private readonly IGuardianService _guardianSvc;

    public StudentsController(IStudentService svc, ISchoolService schoolSvc,
        IClassroomService classSvc, IGuardianService guardianSvc)
    {
        _svc = svc; _schoolSvc = schoolSvc; _classSvc = classSvc; _guardianSvc = guardianSvc;
    }

    public async Task<IActionResult> Index(int? schoolId, int? classroomId, string? search, int page = 1)
    {
        var result = await _svc.GetPagedAsync(schoolId, classroomId, search, page, 20);
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.Classrooms = schoolId.HasValue ? await _classSvc.GetBySchoolAsync(schoolId.Value) : new List<ClassroomDto>();
        ViewBag.SchoolId = schoolId;
        ViewBag.ClassroomId = classroomId;
        ViewBag.Search = search;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success) return NotFound();
        ViewBag.Guardians = await _guardianSvc.GetStudentGuardiansAsync(id);
        return View(r.Data);
    }

    public IActionResult Create() => View(new StudentCreateDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentCreateDto dto)
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
        return View(new StudentUpdateDto
        {
            StudentId = d.StudentId, PersonId = d.PersonId,
            FirstName = d.FirstName, LastName = d.LastName, FatherName = d.FatherName,
            NationalCode = d.NationalCode, Gender = d.Gender, BirthDate = d.BirthDate,
            Mobile = d.Mobile, Address = d.Address,
            StudentCode = d.StudentCode, BloodType = d.BloodType,
            IsActive = d.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentUpdateDto dto)
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

    [HttpGet]
    public async Task<IActionResult> GetClassrooms(int schoolId)
    {
        var list = await _classSvc.GetBySchoolAsync(schoolId);
        return Json(list.Select(c => new { c.ClassroomId, c.Name, c.GradeName }));
    }
}

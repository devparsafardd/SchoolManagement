using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class GuardiansController : Controller
{
    private readonly IGuardianService _svc;
    private readonly IAccountService _accountSvc;

    public GuardiansController(IGuardianService svc, IAccountService accountSvc)
    {
        _svc = svc; _accountSvc = accountSvc;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var r = await _svc.GetPagedAsync(search, page, 20);
        ViewBag.Search = search;
        return View(r);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success) return NotFound();
        return View(r.Data);
    }

    public IActionResult Create(int? studentId)
    {
        ViewBag.StudentId = studentId;
        return View(new GuardianCreateDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GuardianCreateDto dto, int? studentId, string? relationship)
    {
        if (!ModelState.IsValid) return View(dto);
        var r = await _svc.CreateAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            return View(dto);
        }

        if (studentId.HasValue)
        {
            await _svc.AssignToStudentAsync(new AssignGuardianDto
            {
                StudentId = studentId.Value, GuardianId = r.Data,
                Relationship = relationship ?? "پدر", IsPrimary = true
            });
            TempData["Success"] = "ولی اضافه و به دانش‌آموز متصل شد";
            return RedirectToAction("Details", "Students", new { id = studentId });
        }

        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Details), new { id = r.Data });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignGuardianDto dto)
    {
        var r = await _svc.AssignToStudentAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction("Details", "Students", new { id = dto.StudentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int studentId, int guardianId)
    {
        var r = await _svc.RemoveFromStudentAsync(studentId, guardianId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction("Details", "Students", new { id = studentId });
    }
}

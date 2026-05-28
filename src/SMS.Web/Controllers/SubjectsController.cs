using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class SubjectsController : Controller
{
    private readonly ISubjectService _svc;
    private readonly ILookupService _lookup;

    public SubjectsController(ISubjectService svc, ILookupService lookup)
    {
        _svc = svc; _lookup = lookup;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _svc.GetAllAsync(false);
        return View(list);
    }

    public IActionResult Create() => View(new SubjectCreateDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var r = await _svc.CreateAsync(dto);
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

    public async Task<IActionResult> Grade(int gradeId)
    {
        ViewBag.Grades = await _lookup.GetGradesAsync();
        ViewBag.AllSubjects = await _svc.GetAllAsync(true);
        ViewBag.GradeId = gradeId;
        ViewBag.GradeName = (await _lookup.GetGradesAsync()).FirstOrDefault(g => g.Id == gradeId)?.Name;
        var list = await _svc.GetByGradeAsync(gradeId);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToGrade(GradeSubjectCreateDto dto)
    {
        var r = await _svc.AddToGradeAsync(dto);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Grade), new { gradeId = dto.GradeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromGrade(int gradeSubjectId, int gradeId)
    {
        var r = await _svc.RemoveFromGradeAsync(gradeSubjectId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Grade), new { gradeId });
    }
}

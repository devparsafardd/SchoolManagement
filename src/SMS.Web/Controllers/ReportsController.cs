using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.EducatorGroup)]
public class ReportsController : Controller
{
    private readonly IReportCardService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly IClassroomService _classSvc;
    private readonly ILookupService _lookup;

    public ReportsController(IReportCardService svc, ISchoolService schoolSvc, IClassroomService classSvc, ILookupService lookup)
    {
        _svc = svc; _schoolSvc = schoolSvc; _classSvc = classSvc; _lookup = lookup;
    }

    public async Task<IActionResult> Index(int? schoolId, int? classroomId, int? termId)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.Classrooms = schoolId.HasValue ? await _classSvc.GetBySchoolAsync(schoolId.Value) : new List<ClassroomDto>();
        ViewBag.ClassroomId = classroomId;
        ViewBag.Terms = await _lookup.GetTermsAsync();
        ViewBag.TermId = termId;

        if (classroomId.HasValue && termId.HasValue)
        {
            ViewBag.Reports = await _svc.GenerateClassReportAsync(classroomId.Value, termId.Value);
        }
        return View();
    }

    public async Task<IActionResult> Student(int studentId, int termId)
    {
        var r = await _svc.GenerateAsync(studentId, termId);
        if (!r.Success) return NotFound(r.Errors.FirstOrDefault());
        return View(r.Data);
    }

    public async Task<IActionResult> Pdf(int studentId, int termId)
    {
        var r = await _svc.GenerateAsync(studentId, termId);
        if (!r.Success) return NotFound();
        var bytes = await _svc.ExportToPdfAsync(r.Data!);
        var fileName = $"ReportCard-{r.Data!.StudentCode}-{DateTime.Now:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateGPAs(int classroomId, int termId)
    {
        var r = await _svc.CalculateClassroomGradesAsync(classroomId, termId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index), new { classroomId, termId });
    }
}

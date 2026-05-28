using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class BulkImportController : Controller
{
    private readonly IBulkImportService _svc;
    private readonly IClassroomService _classSvc;
    private readonly ISchoolService _schoolSvc;

    public BulkImportController(IBulkImportService svc, IClassroomService classSvc, ISchoolService schoolSvc)
    {
        _svc = svc; _classSvc = classSvc; _schoolSvc = schoolSvc;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        return View();
    }

    public IActionResult StudentsTemplate()
    {
        var bytes = _svc.GenerateStudentsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students-Template.xlsx");
    }

    public IActionResult StaffTemplate()
    {
        var bytes = _svc.GenerateStaffTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Staff-Template.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ImportStudents(IFormFile file, int classroomId)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "فایلی انتخاب نشده";
            return RedirectToAction(nameof(Index));
        }
        using var stream = file.OpenReadStream();
        var result = await _svc.ImportStudentsAsync(stream, classroomId);
        TempData["Success"] = $"✅ {result.SuccessCount} موفق / ❌ {result.FailedCount} ناموفق از {result.TotalRows} ردیف";
        TempData["ImportErrors"] = System.Text.Json.JsonSerializer.Serialize(result.Errors);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ImportStaff(IFormFile file, int schoolId)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "فایلی انتخاب نشده";
            return RedirectToAction(nameof(Index));
        }
        using var stream = file.OpenReadStream();
        var result = await _svc.ImportStaffAsync(stream, schoolId);
        TempData["Success"] = $"✅ {result.SuccessCount} موفق / ❌ {result.FailedCount} ناموفق از {result.TotalRows} ردیف";
        TempData["ImportErrors"] = System.Text.Json.JsonSerializer.Serialize(result.Errors);
        return RedirectToAction(nameof(Index));
    }
}

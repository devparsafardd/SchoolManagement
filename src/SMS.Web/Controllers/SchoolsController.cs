using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Export;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class SchoolsController : Controller
{
    private readonly ISchoolService _svc;
    private readonly ILookupService _lookup;
    private readonly IExcelExporter _excel;

    public SchoolsController(ISchoolService svc, ILookupService lookup, IExcelExporter excel)
    {
        _svc = svc; _lookup = lookup; _excel = excel;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var result = await _svc.GetPagedAsync(search, page, 20);
        ViewBag.Search = search;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success) return NotFound(r.Errors.FirstOrDefault());
        return View(r.Data);
    }

    public async Task<IActionResult> Create()
    {
        await FillLookups();
        return View(new SchoolCreateDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SchoolCreateDto dto)
    {
        if (!ModelState.IsValid) { await FillLookups(); return View(dto); }
        var r = await _svc.CreateAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            await FillLookups();
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success) return NotFound();
        await FillLookups();
        var dto = new SchoolUpdateDto
        {
            SchoolId = r.Data!.SchoolId, Name = r.Data.Name, Code = r.Data.Code,
            CityId = r.Data.CityId, Gender = r.Data.Gender, SchoolType = r.Data.SchoolType,
            EducationLevelId = r.Data.EducationLevelId, Address = r.Data.Address,
            Phone = r.Data.Phone, IsActive = r.Data.IsActive
        };
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SchoolUpdateDto dto)
    {
        if (!ModelState.IsValid) { await FillLookups(); return View(dto); }
        var r = await _svc.UpdateAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            await FillLookups();
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

    public async Task<IActionResult> ExportExcel(string? search)
    {
        var data = await _svc.GetPagedAsync(search, 1, 100000);

        var columns = new[] { "ردیف", "نام مدرسه", "کد", "استان", "شهر", "مقطع", "جنسیت", "نوع", "تلفن", "آدرس", "وضعیت" };
        var rows = data.Items.Select((s, i) => new object?[]
        {
            PersianDate.ToPersianDigits((i + 1).ToString()),
            s.Name, s.Code, s.ProvinceName, s.CityName,
            s.EducationLevelName, s.GenderText, s.SchoolType,
            s.Phone, s.Address, s.IsActive ? "فعال" : "غیرفعال"
        });

        var bytes = _excel.Export($"لیست مدارس - {PersianDate.ToPersian(DateTime.Now)}", columns, rows);
        var fileName = $"Schools-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task FillLookups()
    {
        ViewBag.Cities = await _lookup.GetCitiesAsync();
        ViewBag.EducationLevels = await _lookup.GetEducationLevelsAsync();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Services;

namespace SMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookup;
    public LookupsController(ILookupService lookup) => _lookup = lookup;

    [HttpGet("provinces")]
    public async Task<IActionResult> Provinces() => Ok(await _lookup.GetProvincesAsync());

    [HttpGet("cities")]
    public async Task<IActionResult> Cities([FromQuery] int? provinceId)
        => Ok(await _lookup.GetCitiesAsync(provinceId));

    [HttpGet("education-levels")]
    public async Task<IActionResult> EducationLevels() => Ok(await _lookup.GetEducationLevelsAsync());

    [HttpGet("grades")]
    public async Task<IActionResult> Grades([FromQuery] int? educationLevelId)
        => Ok(await _lookup.GetGradesAsync(educationLevelId));

    [HttpGet("academic-years")]
    public async Task<IActionResult> AcademicYears() => Ok(await _lookup.GetAcademicYearsAsync());

    [HttpGet("active-year")]
    public async Task<IActionResult> ActiveYear() => Ok(await _lookup.GetActiveAcademicYearAsync());
}

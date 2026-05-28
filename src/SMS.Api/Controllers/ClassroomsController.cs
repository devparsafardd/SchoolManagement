using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassroomsController : ControllerBase
{
    private readonly IClassroomService _svc;
    public ClassroomsController(IClassroomService svc) => _svc = svc;

    [HttpGet("by-school/{schoolId:int}")]
    public async Task<IActionResult> BySchool(int schoolId, [FromQuery] int? academicYearId)
        => Ok(await _svc.GetBySchoolAsync(schoolId, academicYearId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Create([FromBody] ClassroomCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto);
        return r.Success ? Created("", new { id = r.Data }) : BadRequest(new { errors = r.Errors });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id);
        return r.Success ? Ok() : BadRequest(new { errors = r.Errors });
    }
}

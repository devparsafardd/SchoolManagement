using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _svc;
    public StudentsController(IStudentService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int? schoolId, [FromQuery] int? classroomId,
                                              [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _svc.GetPagedAsync(schoolId, classroomId, search, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { message = r.Errors.FirstOrDefault() });
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Create([FromBody] StudentCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto);
        return r.Success ? Created("", new { id = r.Data, message = r.Message }) : BadRequest(new { errors = r.Errors });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Update(int id, [FromBody] StudentUpdateDto dto)
    {
        dto.StudentId = id;
        var r = await _svc.UpdateAsync(dto);
        return r.Success ? Ok(new { message = r.Message }) : BadRequest(new { errors = r.Errors });
    }

    [HttpPost("{id:int}/enroll/{classroomId:int}")]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Enroll(int id, int classroomId)
    {
        var r = await _svc.EnrollAsync(id, classroomId);
        return r.Success ? Ok(new { message = r.Message }) : BadRequest(new { errors = r.Errors });
    }
}

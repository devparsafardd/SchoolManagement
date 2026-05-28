using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchoolsController : ControllerBase
{
    private readonly ISchoolService _svc;
    public SchoolsController(ISchoolService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _svc.GetPagedAsync(search, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { message = r.Errors.FirstOrDefault() });
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminGroup)]
    public async Task<IActionResult> Create([FromBody] SchoolCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto);
        return r.Success ? Created("", new { id = r.Data, message = r.Message }) : BadRequest(new { errors = r.Errors });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdminGroup)]
    public async Task<IActionResult> Update(int id, [FromBody] SchoolUpdateDto dto)
    {
        dto.SchoolId = id;
        var r = await _svc.UpdateAsync(dto);
        return r.Success ? Ok(new { message = r.Message }) : BadRequest(new { errors = r.Errors });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.AdminGroup)]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id);
        return r.Success ? Ok(new { message = r.Message }) : BadRequest(new { errors = r.Errors });
    }
}

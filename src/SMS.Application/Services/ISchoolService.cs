using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface ISchoolService
{
    Task<PagedResult<SchoolDto>> GetPagedAsync(string? search, int page = 1, int pageSize = 20);
    Task<Result<SchoolDto>> GetByIdAsync(int id);
    Task<Result<int>> CreateAsync(SchoolCreateDto dto);
    Task<Result> UpdateAsync(SchoolUpdateDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result> ToggleActiveAsync(int id);
}

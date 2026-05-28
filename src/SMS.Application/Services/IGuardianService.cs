using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IGuardianService
{
    Task<PagedResult<GuardianDto>> GetPagedAsync(string? search, int page = 1, int pageSize = 20);
    Task<Result<GuardianDto>> GetByIdAsync(int id);
    Task<Result<int>> CreateAsync(GuardianCreateDto dto);
    Task<List<StudentGuardianDto>> GetStudentGuardiansAsync(int studentId);
    Task<Result> AssignToStudentAsync(AssignGuardianDto dto);
    Task<Result> RemoveFromStudentAsync(int studentId, int guardianId);
}

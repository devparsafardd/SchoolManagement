using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IStudentService
{
    Task<PagedResult<StudentDto>> GetPagedAsync(int? schoolId, int? classroomId, string? search, int page = 1, int pageSize = 20);
    Task<Result<StudentDto>> GetByIdAsync(int id);
    Task<Result<int>> CreateAsync(StudentCreateDto dto);
    Task<Result> UpdateAsync(StudentUpdateDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result> EnrollAsync(int studentId, int classroomId);
}

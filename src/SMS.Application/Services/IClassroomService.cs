using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IClassroomService
{
    Task<List<ClassroomDto>> GetBySchoolAsync(int schoolId, int? academicYearId = null);
    Task<Result<ClassroomDto>> GetByIdAsync(int id);
    Task<Result<int>> CreateAsync(ClassroomCreateDto dto);
    Task<Result> DeleteAsync(int id);
}

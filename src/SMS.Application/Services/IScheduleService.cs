using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IScheduleService
{
    // SchoolPeriod
    Task<List<SchoolPeriodDto>> GetPeriodsAsync(int schoolId);
    Task<Result<int>> CreatePeriodAsync(SchoolPeriodCreateDto dto);
    Task<Result> UpdatePeriodAsync(int periodId, SchoolPeriodCreateDto dto);
    Task<Result> DeletePeriodAsync(int periodId);

    // ClassSchedule
    Task<List<ClassScheduleDto>> GetClassroomScheduleAsync(int classroomId);
    Task<Result<int>> AssignScheduleAsync(ClassScheduleCreateDto dto);
    Task<Result> RemoveScheduleAsync(int scheduleId);
}

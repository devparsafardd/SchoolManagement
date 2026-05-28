using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IStaffService
{
    Task<PagedResult<StaffDto>> GetPagedAsync(int? schoolId, string? position, string? search, int page = 1, int pageSize = 20);
    Task<Result<StaffDto>> GetByIdAsync(int id);
    Task<Result<int>> CreateAsync(StaffCreateDto dto);
    Task<Result> UpdateAsync(StaffUpdateDto dto);
    Task<Result> DeleteAsync(int id);

    // Assignments
    Task<List<StaffAssignmentDto>> GetAssignmentsAsync(int staffId);
    Task<Result> AddAssignmentAsync(StaffAssignmentCreateDto dto);
    Task<Result> RemoveAssignmentAsync(int assignmentId);

    // List for dropdowns
    Task<List<StaffDto>> GetTeachersBySchoolAsync(int schoolId, int? academicYearId = null);
}

public interface ISubjectService
{
    Task<List<SubjectDto>> GetAllAsync(bool onlyActive = true);
    Task<Result<int>> CreateAsync(SubjectCreateDto dto);
    Task<Result> UpdateAsync(int id, SubjectCreateDto dto);
    Task<Result> DeleteAsync(int id);

    // Grade Subjects (تخصیص درس به پایه)
    Task<List<GradeSubjectDto>> GetByGradeAsync(int gradeId);
    Task<Result<int>> AddToGradeAsync(GradeSubjectCreateDto dto);
    Task<Result> RemoveFromGradeAsync(int gradeSubjectId);

    // Class Subject Teachers (تخصیص معلم به درس کلاس)
    Task<List<ClassSubjectTeacherDto>> GetByClassroomAsync(int classroomId);
    Task<Result<int>> AssignTeacherAsync(AssignTeacherDto dto);
    Task<Result> UnassignTeacherAsync(int classSubjectId);
}

public interface IAttendanceService
{
    Task<List<AttendanceStatusDto>> GetStatusesAsync();
    Task<TakeAttendanceDto> GetForDateAsync(int classroomId, DateTime date, int? classSubjectId = null);
    Task<Result> SaveAsync(TakeAttendanceDto dto, int recordedByStaffId);
    Task<List<AttendanceReportRow>> GetReportAsync(int classroomId, DateTime fromDate, DateTime toDate);
    Task<List<AttendanceReportRow>> GetStudentReportAsync(int studentId, DateTime fromDate, DateTime toDate);
}

public interface IExamService
{
    Task<List<ExamDto>> GetByClassSubjectAsync(int classSubjectId);
    Task<List<ExamDto>> GetByTeacherAsync(int staffId, int? termId = null);
    Task<Result<ExamDto>> GetByIdAsync(long id);
    Task<Result<long>> CreateAsync(ExamCreateDto dto, int createdByStaffId);
    Task<Result> UpdateAsync(long id, ExamCreateDto dto);
    Task<Result> DeleteAsync(long id);
    Task<Result> FinalizeAsync(long id);
    Task<Result> UnfinalizeAsync(long id);

    Task<List<ExamScoreRow>> GetScoresAsync(long examId);
    Task<Result> SaveScoresAsync(EnterScoresDto dto, int enteredByStaffId);

    Task<List<GradeScaleDto>> GetGradeScalesAsync();
    Task<List<ExamTypeDto>> GetExamTypesAsync();
}

public interface IDisciplineService
{
    Task<PagedResult<DisciplinaryRecordDto>> GetPagedAsync(int? studentId, int? classroomId, string? category, int page = 1, int pageSize = 20);
    Task<Result<long>> CreateAsync(DisciplinaryRecordCreateDto dto, int recordedByStaffId);
    Task<Result> DeleteAsync(long id);
    Task<List<DisciplinaryTypeDto>> GetTypesAsync();
}

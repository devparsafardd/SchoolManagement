using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>
/// سرویس گزارش‌گیری و آمار جامع
/// </summary>
public interface IAnalyticsService
{
    /// <summary>گزارش جامع حضور و غیاب</summary>
    Task<AttendanceAnalyticsDto> GetAttendanceAnalyticsAsync(int? schoolId, DateTime fromDate, DateTime toDate);

    /// <summary>گزارش عملکرد تحصیلی (نمرات و معدل)</summary>
    Task<AcademicAnalyticsDto> GetAcademicAnalyticsAsync(int? schoolId, int? termId);

    /// <summary>گزارش مالی</summary>
    Task<FinancialAnalyticsDto> GetFinancialAnalyticsAsync(int? schoolId, int? academicYearId, DateTime fromDate, DateTime toDate);

    /// <summary>گزارش انضباطی</summary>
    Task<DisciplineAnalyticsDto> GetDisciplineAnalyticsAsync(int? schoolId, DateTime fromDate, DateTime toDate);

    /// <summary>گزارش جامع یک کلاس</summary>
    Task<Result<ClassroomAnalyticsDto>> GetClassroomAnalyticsAsync(int classroomId, int? termId = null);

    /// <summary>گزارش عملکرد یک معلم</summary>
    Task<Result<TeacherAnalyticsDto>> GetTeacherAnalyticsAsync(int staffId, int? termId = null);

    /// <summary>گزارش جامع یک دانش‌آموز</summary>
    Task<Result<StudentAnalyticsDto>> GetStudentAnalyticsAsync(int studentId, int? termId = null);
}

using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>
/// سرویس پنل معلم - برای دسترسی به اطلاعات کلاس‌ها و دانش‌آموزان خودِ معلم
/// همه متدها بر اساس StaffId محدود می‌شوند تا معلم فقط داده‌های خود را ببیند
/// </summary>
public interface ITeacherPortalService
{
    /// <summary>پیدا کردن StaffId از روی PersonId کاربر جاری</summary>
    Task<int?> GetStaffIdByPersonAsync(int personId);

    /// <summary>بررسی اینکه آیا این درس کلاسی به این معلم تعلق دارد یا نه</summary>
    Task<bool> OwnsClassSubjectAsync(int staffId, int classSubjectId);

    /// <summary>بررسی اینکه آیا این کلاس متعلق به یکی از درس‌های این معلم است</summary>
    Task<bool> OwnsClassroomAsync(int staffId, int classroomId);

    /// <summary>اطلاعات داشبورد معلم</summary>
    Task<Result<TeacherDashboardDto>> GetDashboardAsync(int staffId, TeacherDashboardFilter? filter = null);

    /// <summary>لیست همه کلاس-درس‌های معلم در سال تحصیلی</summary>
    Task<List<TeacherClassBriefDto>> GetMyClassesAsync(int staffId, int? academicYearId = null);

    /// <summary>جزئیات یک کلاس درسی برای معلم</summary>
    Task<Result<TeacherClassDetailDto>> GetClassDetailAsync(int staffId, int classSubjectId);

    /// <summary>لیست دانش‌آموزان یک کلاس درسی</summary>
    Task<List<TeacherClassStudentRow>> GetClassStudentsAsync(int staffId, int classSubjectId);

    /// <summary>برنامه هفتگی معلم</summary>
    Task<List<TeacherTodayClassDto>> GetWeeklyScheduleAsync(int staffId, int? academicYearId = null);

    /// <summary>برنامه امروز معلم</summary>
    Task<List<TeacherTodayClassDto>> GetTodayScheduleAsync(int staffId);

    /// <summary>آزمون‌های پیش رو</summary>
    Task<List<TeacherUpcomingExamDto>> GetUpcomingExamsAsync(int staffId, int daysAhead = 14);

    /// <summary>تاریخچه فعالیت‌های اخیر معلم</summary>
    Task<List<TeacherRecentActivityDto>> GetRecentActivitiesAsync(int staffId, int limit = 10);
}

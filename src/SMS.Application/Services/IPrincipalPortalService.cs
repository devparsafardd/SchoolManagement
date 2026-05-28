using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>
/// سرویس پنل مدیر مدرسه (Principal/VicePrincipal)
/// همه متدها بر اساس SchoolId محدود می‌شوند تا مدیر فقط داده‌های مدرسه خود را ببیند
/// </summary>
public interface IPrincipalPortalService
{
    /// <summary>پیدا کردن لیست SchoolId هایی که کاربر در آن‌ها مدیر/معاون است</summary>
    Task<List<int>> GetManagedSchoolIdsAsync(int personId);

    /// <summary>بررسی اینکه آیا این کاربر روی این مدرسه دسترسی مدیریتی دارد</summary>
    Task<bool> CanAccessSchoolAsync(int personId, int schoolId);

    /// <summary>اطلاعات داشبورد مدیر مدرسه</summary>
    Task<Result<PrincipalDashboardDto>> GetDashboardAsync(int schoolId, PrincipalFilter? filter = null);

    /// <summary>لیست همه کلاس‌های مدرسه</summary>
    Task<List<PrincipalClassroomBrief>> GetClassroomsAsync(int schoolId, int? academicYearId = null);

    /// <summary>گزارش حضور و غیاب ۷ روز اخیر</summary>
    Task<List<PrincipalChartPoint>> GetAttendanceTrendAsync(int schoolId, int days = 7);

    /// <summary>تعداد دانش‌آموز هر پایه</summary>
    Task<List<PrincipalGradeStudentCount>> GetStudentDistributionAsync(int schoolId, int? academicYearId = null);

    /// <summary>فعالیت‌های اخیر مدرسه</summary>
    Task<List<PrincipalRecentActivity>> GetRecentActivitiesAsync(int schoolId, int limit = 15);
}

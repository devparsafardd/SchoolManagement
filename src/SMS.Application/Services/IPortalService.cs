using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس پنل دانش‌آموز و اولیا</summary>
public interface IPortalService
{
    /// <summary>پیدا کردن StudentId از PersonId (برای پنل دانش‌آموز)</summary>
    Task<int?> GetStudentIdByPersonAsync(int personId);

    /// <summary>پیدا کردن GuardianId از PersonId (برای پنل ولی)</summary>
    Task<int?> GetGuardianIdByPersonAsync(int personId);

    /// <summary>لیست فرزندان یک ولی</summary>
    Task<List<ChildSummaryDto>> GetChildrenAsync(int guardianId);

    /// <summary>بررسی اینکه آیا این ولی به این دانش‌آموز دسترسی دارد یا خود دانش‌آموز است</summary>
    Task<bool> CanAccessStudentAsync(int personId, int studentId);

    /// <summary>اطلاعات کلی برای داشبورد</summary>
    Task<Result<StudentPortalSummaryDto>> GetSummaryAsync(int studentId, int? termId = null);

    /// <summary>همه نمرات دانش‌آموز در یک ترم</summary>
    Task<List<StudentScoreRow>> GetScoresAsync(int studentId, int? termId = null);

    /// <summary>تاریخچه حضور و غیاب در بازه</summary>
    Task<List<StudentAttendanceRow>> GetAttendanceAsync(int studentId, DateTime fromDate, DateTime toDate);

    /// <summary>سوابق انضباطی</summary>
    Task<List<DisciplinaryRecordDto>> GetDisciplinaryRecordsAsync(int studentId);

    /// <summary>فاکتورهای مالی</summary>
    Task<List<InvoiceDto>> GetInvoicesAsync(int studentId);

    /// <summary>اعلان‌های مرتبط</summary>
    Task<List<AnnouncementDto>> GetAnnouncementsAsync(int studentId, string audience);
}

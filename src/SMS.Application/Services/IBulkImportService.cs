using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IBulkImportService
{
    /// <summary>ایمپورت دانش‌آموزان از Excel</summary>
    Task<BulkImportResult> ImportStudentsAsync(Stream excelStream, int classroomId);

    /// <summary>ایمپورت معلمان از Excel</summary>
    Task<BulkImportResult> ImportStaffAsync(Stream excelStream, int schoolId);

    /// <summary>دانلود قالب نمونه برای ثبت‌نام دانش‌آموزان</summary>
    byte[] GenerateStudentsTemplate();

    /// <summary>دانلود قالب نمونه برای ثبت‌نام معلمان</summary>
    byte[] GenerateStaffTemplate();
}

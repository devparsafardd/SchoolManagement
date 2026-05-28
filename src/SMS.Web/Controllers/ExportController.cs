using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Export;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

/// <summary>
/// خروجی گرفتن گزارش‌ها به‌صورت Excel و PDF
/// </summary>
[Authorize(Roles = RoleNames.EducatorGroup)]
public class ExportController : Controller
{
    private readonly IExcelExporter _excel;
    private readonly IPdfExporter _pdf;
    private readonly IAnalyticsService _analytics;
    private readonly IAttendanceService _attendance;
    private readonly IClassroomService _classSvc;
    private readonly IStudentService _studentSvc;
    private readonly IStaffService _staffSvc;
    private readonly IReportCardService _reportCard;

    public ExportController(IExcelExporter excel, IPdfExporter pdf, IAnalyticsService analytics,
        IAttendanceService attendance, IClassroomService classSvc,
        IStudentService studentSvc, IStaffService staffSvc, IReportCardService reportCard)
    {
        _excel = excel; _pdf = pdf; _analytics = analytics; _attendance = attendance;
        _classSvc = classSvc; _studentSvc = studentSvc; _staffSvc = staffSvc;
        _reportCard = reportCard;
    }

    // ===== لیست دانش‌آموزان به Excel =====
    public async Task<IActionResult> StudentsExcel(int? schoolId, string? search)
    {
        var data = (await _studentSvc.GetPagedAsync(schoolId, null, search, 1, 100000)).Items;
        var bytes = _excel.Export(
            "لیست دانش‌آموزان",
            new List<string> { "ردیف", "کد دانش‌آموزی", "نام و نام خانوادگی", "نام پدر", "جنسیت", "موبایل", "مدرسه", "کلاس", "وضعیت" },
            data.Select((s, i) => new object?[] {
                i + 1, s.StudentCode, s.FullName, s.FatherName,
                s.Gender == "M" ? "پسر" : "دختر", s.Mobile,
                s.CurrentSchoolName, s.CurrentClassName, s.IsActive ? "فعال" : "غیرفعال"
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Students-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    // ===== لیست معلمان به Excel =====
    public async Task<IActionResult> StaffExcel(int? schoolId, string? search)
    {
        var data = (await _staffSvc.GetPagedAsync(schoolId, null, search, 1, 100000)).Items;
        var bytes = _excel.Export(
            "لیست معلمان و کارکنان",
            new List<string> { "ردیف", "کد پرسنلی", "نام و نام خانوادگی", "کد ملی", "جنسیت", "مدرک", "رشته", "موبایل", "ایمیل", "تاریخ استخدام" },
            data.Select((s, i) => new object?[] {
                i + 1, s.PersonnelCode, s.FullName, s.NationalCode,
                s.GenderText, s.Degree, s.FieldOfStudy,
                s.Mobile, s.Email,
                s.HireDate.HasValue ? PersianDate.ToPersian(s.HireDate.Value) : ""
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Staff-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    // ===== لیست کلاس‌ها به Excel =====
    public async Task<IActionResult> ClassroomsExcel(int? schoolId)
    {
        var data = schoolId.HasValue
            ? await _classSvc.GetBySchoolAsync(schoolId.Value)
            : new List<Application.DTOs.ClassroomDto>();
        var bytes = _excel.Export(
            "لیست کلاس‌ها",
            new List<string> { "ردیف", "نام کلاس", "پایه", "مدرسه", "ظرفیت", "تعداد دانش‌آموز", "اتاق", "وضعیت" },
            data.Select((c, i) => new object?[] {
                i + 1, c.Name, c.GradeName, c.SchoolName,
                c.Capacity, c.CurrentStudentCount, c.RoomNumber,
                c.IsActive ? "فعال" : "غیرفعال"
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Classrooms-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    // ===== گزارش حضور و غیاب یک کلاس به Excel =====
    public async Task<IActionResult> ClassAttendanceExcel(int classroomId, string? fromDate, string? toDate)
    {
        var from = string.IsNullOrEmpty(fromDate) ? DateTime.Today.AddDays(-30) : PersianDate.FromPersian(fromDate) ?? DateTime.Today.AddDays(-30);
        var to = string.IsNullOrEmpty(toDate) ? DateTime.Today : PersianDate.FromPersian(toDate) ?? DateTime.Today;
        var rows = await _attendance.GetReportAsync(classroomId, from, to);

        var cls = await _classSvc.GetByIdAsync(classroomId);
        var bytes = _excel.Export(
            $"گزارش حضور و غیاب - {cls.Data?.Name} - {PersianDate.ToPersian(from)} تا {PersianDate.ToPersian(to)}",
            new List<string> { "ردیف", "کد", "نام و نام خانوادگی", "حاضر", "غایب غیرموجه", "غایب موجه", "تاخیر", "مرخصی", "کل غیبت" },
            rows.Select((r, i) => new object?[] {
                i + 1, r.StudentCode, r.FullName,
                r.PresentCount, r.UnexcusedAbsenceCount, r.ExcusedAbsenceCount,
                r.TardyCount, r.LeaveCount, r.TotalAbsences
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Attendance-{cls.Data?.Name}-{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ===== گزارش جامع یک کلاس به Excel =====
    public async Task<IActionResult> ClassroomReportExcel(int id, int? termId)
    {
        var r = await _analytics.GetClassroomAnalyticsAsync(id, termId);
        if (!r.Success) return NotFound();
        var d = r.Data!;
        var bytes = _excel.Export(
            $"گزارش جامع کلاس {d.ClassroomName}",
            new List<string> { "رتبه", "کد", "نام", "میانگین", "غیبت", "تاخیر", "تشویق", "تنبیه" },
            d.StudentRanking.Select(s => new object?[] {
                s.Rank, s.StudentCode, s.FullName,
                s.Average, s.AbsenceCount, s.TardyCount,
                s.RewardsCount, s.PunishmentsCount
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Class-{d.ClassroomName}-{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ===== کارنامه دانش‌آموز به PDF =====
    public async Task<IActionResult> StudentReportCardPdf(int studentId, int termId)
    {
        var r = await _reportCard.GenerateAsync(studentId, termId);
        if (!r.Success) return NotFound();
        var bytes = await _reportCard.ExportToPdfAsync(r.Data!);
        return File(bytes, "application/pdf", $"ReportCard-{r.Data!.StudentCode}.pdf");
    }

    // ===== کارنامه دانش‌آموز به Excel =====
    public async Task<IActionResult> StudentReportCardExcel(int studentId, int termId)
    {
        var r = await _reportCard.GenerateAsync(studentId, termId);
        if (!r.Success) return NotFound();
        var d = r.Data!;
        var bytes = _excel.Export(
            $"کارنامه {d.FullName} - {d.TermName}",
            new List<string> { "درس", "ضریب", "نمره عددی", "ارزشیابی توصیفی", "نمره معلم", "وضعیت" },
            d.Subjects.Select(s => new object?[] {
                s.SubjectName, s.Coefficient,
                s.NumericScore, s.DescriptiveLabel,
                s.TeacherComment,
                s.IsPassed == true ? "قبول" : (s.IsPassed == false ? "مردود" : "—")
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ReportCard-{d.StudentCode}.xlsx");
    }

    // ===== گزارش جامع دانش‌آموز به Excel =====
    public async Task<IActionResult> StudentReportExcel(int id, int? termId)
    {
        var r = await _analytics.GetStudentAnalyticsAsync(id, termId);
        if (!r.Success) return NotFound();
        var d = r.Data!;
        var bytes = _excel.Export(
            $"گزارش جامع {d.FullName}",
            new List<string> { "تاریخ", "درس", "عنوان آزمون", "نمره", "نمره کل", "توصیفی" },
            d.RecentExams.Select(e => new object?[] {
                PersianDate.ToPersian(e.Date), e.SubjectName, e.Title,
                e.Score, e.MaxScore, e.DescriptiveLabel
            })
        );
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Student-{d.StudentCode}.xlsx");
    }

    // ===== خروجی PDF =====
    public async Task<IActionResult> StudentsPdf(int? schoolId, string? search)
    {
        var data = (await _studentSvc.GetPagedAsync(schoolId, null, search, 1, 100000)).Items;
        var bytes = _pdf.ExportTable(
            "لیست دانش‌آموزان",
            new List<string> { "ردیف", "کد", "نام", "نام پدر", "جنسیت", "موبایل", "مدرسه", "کلاس" },
            data.Select((s, i) => new object?[] {
                i + 1, s.StudentCode, s.FullName, s.FatherName,
                s.Gender == "M" ? "پسر" : "دختر", s.Mobile,
                s.CurrentSchoolName, s.CurrentClassName
            }),
            subtitle: $"تعداد: {data.Count} | تاریخ: {SMS.Shared.Helpers.PersianDate.ToPersianLong(DateTime.Now)}"
        );
        return File(bytes, "application/pdf", $"Students-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    public async Task<IActionResult> StaffPdf(int? schoolId, string? search)
    {
        var data = (await _staffSvc.GetPagedAsync(schoolId, null, search, 1, 100000)).Items;
        var bytes = _pdf.ExportTable(
            "لیست معلمان و کارکنان",
            new List<string> { "ردیف", "کد پرسنلی", "نام", "جنسیت", "مدرک", "رشته", "موبایل" },
            data.Select((s, i) => new object?[] {
                i + 1, s.PersonnelCode, s.FullName, s.GenderText,
                s.Degree, s.FieldOfStudy, s.Mobile
            }),
            subtitle: $"تعداد: {data.Count} | تاریخ: {SMS.Shared.Helpers.PersianDate.ToPersianLong(DateTime.Now)}"
        );
        return File(bytes, "application/pdf", $"Staff-{DateTime.Now:yyyyMMdd}.pdf");
    }

    public async Task<IActionResult> ClassroomReportPdf(int id, int? termId)
    {
        var r = await _analytics.GetClassroomAnalyticsAsync(id, termId);
        if (!r.Success) return NotFound();
        var d = r.Data!;
        var bytes = _pdf.ExportTable(
            $"گزارش جامع کلاس {d.ClassroomName}",
            new List<string> { "رتبه", "کد", "نام", "میانگین", "غیبت", "تاخیر", "تشویق", "تنبیه" },
            d.StudentRanking.Select(s => new object?[] {
                s.Rank, s.StudentCode, s.FullName,
                s.Average?.ToString("0.00"), s.AbsenceCount, s.TardyCount,
                s.RewardsCount, s.PunishmentsCount
            }),
            subtitle: $"{d.SchoolName} | {d.GradeName} | تعداد: {d.TotalStudents} | میانگین کلاس: {d.ClassAverage:0.00}"
        );
        return File(bytes, "application/pdf", $"Class-{d.ClassroomName}.pdf");
    }
}
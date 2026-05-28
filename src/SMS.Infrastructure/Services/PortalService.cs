using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;
using SMS.Shared.Helpers;

namespace SMS.Infrastructure.Services;

public class PortalService : IPortalService
{
    private readonly SmsDbContext _db;
    public PortalService(SmsDbContext db) => _db = db;

    public async Task<int?> GetStudentIdByPersonAsync(int personId) =>
        await _db.Students.Where(s => s.PersonId == personId).Select(s => (int?)s.StudentId).FirstOrDefaultAsync();

    public async Task<int?> GetGuardianIdByPersonAsync(int personId) =>
        await _db.Guardians.Where(g => g.PersonId == personId).Select(g => (int?)g.GuardianId).FirstOrDefaultAsync();

    public async Task<List<ChildSummaryDto>> GetChildrenAsync(int guardianId)
    {
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);

        var studentIds = await _db.StudentGuardians
            .Where(sg => sg.GuardianId == guardianId)
            .Select(sg => sg.StudentId).ToListAsync();

        var children = await _db.Students
            .Include(s => s.Person)
            .Include(s => s.Enrollments).ThenInclude(e => e.Classroom).ThenInclude(c => c.School)
            .Where(s => studentIds.Contains(s.StudentId))
            .AsNoTracking()
            .ToListAsync();

        var result = new List<ChildSummaryDto>();
        var monthAgo = DateTime.Today.AddDays(-30);

        foreach (var s in children)
        {
            var currentEnrollment = s.Enrollments
                .Where(e => e.Status == "فعال" && (activeYear == null || e.AcademicYearId == activeYear.AcademicYearId))
                .OrderByDescending(e => e.EnrollmentDate)
                .FirstOrDefault();

            var gpa = await _db.StudentTermGPAs.AsNoTracking()
                .Where(g => g.StudentId == s.StudentId)
                .OrderByDescending(g => g.CalculatedAt)
                .Select(g => g.GPA).FirstOrDefaultAsync();

            var recentAbsences = await _db.Attendances.Include(a => a.Status)
                .CountAsync(a => a.StudentId == s.StudentId && a.AttendanceDate >= monthAgo && a.Status.IsAbsent);

            var invoices = await _db.StudentInvoices
                .Where(i => i.StudentId == s.StudentId && i.Status != "لغو")
                .Include(i => i.Payments)
                .AsNoTracking().ToListAsync();
            var totalDebt = invoices.Sum(i => i.NetAmount);
            var totalPaid = invoices.Sum(i => i.Payments.Sum(p => p.Amount));

            result.Add(new ChildSummaryDto
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.Person.FullName,
                SchoolName = currentEnrollment?.Classroom.School.Name ?? "—",
                ClassroomName = currentEnrollment?.Classroom.Name ?? "—",
                CurrentGPA = gpa,
                RecentAbsences = recentAbsences,
                RemainingDebt = totalDebt - totalPaid
            });
        }

        return result;
    }

    public async Task<bool> CanAccessStudentAsync(int personId, int studentId)
    {
        // اگر خود دانش‌آموز است
        var ownStudentId = await _db.Students.Where(s => s.PersonId == personId).Select(s => (int?)s.StudentId).FirstOrDefaultAsync();
        if (ownStudentId == studentId) return true;

        // اگر ولی این دانش‌آموز است
        var guardianId = await _db.Guardians.Where(g => g.PersonId == personId).Select(g => (int?)g.GuardianId).FirstOrDefaultAsync();
        if (guardianId.HasValue)
        {
            var isGuardian = await _db.StudentGuardians.AnyAsync(sg => sg.GuardianId == guardianId && sg.StudentId == studentId);
            if (isGuardian) return true;
        }

        return false;
    }

    public async Task<Result<StudentPortalSummaryDto>> GetSummaryAsync(int studentId, int? termId = null)
    {
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear is null) return Result<StudentPortalSummaryDto>.Fail("سال تحصیلی فعال یافت نشد");

        var student = await _db.Students.Include(s => s.Person)
            .AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student is null) return Result<StudentPortalSummaryDto>.Fail("دانش‌آموز یافت نشد");

        var enrollment = await _db.Enrollments
            .Include(e => e.Classroom).ThenInclude(c => c.School)
            .Include(e => e.Classroom).ThenInclude(c => c.Grade)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == studentId
                && e.AcademicYearId == activeYear.AcademicYearId && e.Status == "فعال");

        if (enrollment is null) return Result<StudentPortalSummaryDto>.Fail("ثبت‌نام فعال یافت نشد");

        var dto = new StudentPortalSummaryDto
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.Person.FullName,
            FatherName = student.Person.FatherName,
            PhotoPath = student.Person.PhotoPath,
            SchoolName = enrollment.Classroom.School.Name,
            ClassroomName = enrollment.Classroom.Name,
            GradeName = enrollment.Classroom.Grade.Name,
            AcademicYearTitle = activeYear.Title
        };

        // معدل و رتبه از StudentTermGPA
        if (termId.HasValue)
        {
            var gpa = await _db.StudentTermGPAs.AsNoTracking()
                .FirstOrDefaultAsync(g => g.StudentId == studentId && g.TermId == termId);
            if (gpa != null)
            {
                dto.CurrentGPA = gpa.GPA;
                dto.RankInClass = gpa.RankInClass;
            }
        }
        else
        {
            var latestGpa = await _db.StudentTermGPAs.AsNoTracking()
                .Where(g => g.StudentId == studentId)
                .OrderByDescending(g => g.CalculatedAt).FirstOrDefaultAsync();
            if (latestGpa != null)
            {
                dto.CurrentGPA = latestGpa.GPA;
                dto.RankInClass = latestGpa.RankInClass;
            }
        }

        // حضور و غیاب کل سال
        var attendances = await _db.Attendances.Include(a => a.Status)
            .Where(a => a.StudentId == studentId
                && a.AttendanceDate >= activeYear.StartDate && a.AttendanceDate <= activeYear.EndDate)
            .AsNoTracking().ToListAsync();
        dto.TotalAbsences = attendances.Count(a => a.Status.IsAbsent);
        dto.UnexcusedAbsences = attendances.Count(a => a.Status.Code == "ABSENT");
        dto.TotalTardies = attendances.Count(a => a.Status.IsTardy);

        var lastAtt = attendances.OrderByDescending(a => a.AttendanceDate).FirstOrDefault();
        if (lastAtt != null)
        {
            dto.LastAttendanceDate = lastAtt.AttendanceDate;
            dto.LastAttendanceStatus = lastAtt.Status.Name;
        }

        // انضباطی
        var discRecords = await _db.DisciplinaryRecords.Include(d => d.Type)
            .Where(d => d.StudentId == studentId && d.AcademicYearId == activeYear.AcademicYearId)
            .AsNoTracking().ToListAsync();
        dto.RewardsCount = discRecords.Count(d => d.Type.Category == "R");
        dto.PunishmentsCount = discRecords.Count(d => d.Type.Category == "P");

        // مالی
        var invoices = await _db.StudentInvoices
            .Where(i => i.StudentId == studentId && i.Status != "لغو")
            .Include(i => i.Payments)
            .AsNoTracking().ToListAsync();
        dto.TotalDebt = invoices.Sum(i => i.NetAmount);
        dto.TotalPaid = invoices.Sum(i => i.Payments.Sum(p => p.Amount));

        return Result<StudentPortalSummaryDto>.Ok(dto);
    }

    public async Task<List<StudentScoreRow>> GetScoresAsync(int studentId, int? termId = null)
    {
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear is null) return new List<StudentScoreRow>();

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId
                && e.AcademicYearId == activeYear.AcademicYearId && e.Status == "فعال");
        if (enrollment is null) return new List<StudentScoreRow>();

        var q = _db.ExamScores
            .Include(s => s.Exam).ThenInclude(e => e.ExamType)
            .Include(s => s.Exam).ThenInclude(e => e.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(s => s.Exam).ThenInclude(e => e.ClassSubject).ThenInclude(cs => cs.Staff).ThenInclude(st => st.Person)
            .Include(s => s.DescriptiveScaleItem)
            .Where(s => s.StudentId == studentId
                && s.Exam.ClassSubject.ClassroomId == enrollment.ClassroomId);

        if (termId.HasValue) q = q.Where(s => s.Exam.TermId == termId);

        return await q.AsNoTracking()
            .OrderByDescending(s => s.Exam.ExamDate)
            .Select(s => new StudentScoreRow
            {
                SubjectName = s.Exam.ClassSubject.GradeSubject.Subject.Name,
                TeacherName = s.Exam.ClassSubject.Staff.Person.FirstName + " " + s.Exam.ClassSubject.Staff.Person.LastName,
                ExamTitle = s.Exam.Title,
                ExamTypeName = s.Exam.ExamType.Name,
                ExamDate = s.Exam.ExamDate,
                NumericScore = s.NumericScore,
                DescriptiveLabel = s.DescriptiveScaleItem != null ? s.DescriptiveScaleItem.Label : null,
                MaxScore = s.Exam.MaxScore,
                IsDescriptive = s.Exam.IsDescriptive,
                IsAbsent = s.IsAbsent,
                IsExempt = s.IsExempt,
                Comment = s.Comment
            }).ToListAsync();
    }

    public async Task<List<StudentAttendanceRow>> GetAttendanceAsync(int studentId, DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        var list = await _db.Attendances.Include(a => a.Status)
            .Where(a => a.StudentId == studentId && a.AttendanceDate >= from && a.AttendanceDate <= to)
            .AsNoTracking()
            .OrderByDescending(a => a.AttendanceDate)
            .Select(a => new StudentAttendanceRow
            {
                AttendanceDate = a.AttendanceDate,
                StatusId = a.StatusId,
                StatusName = a.Status.Name,
                StatusColor = a.Status.Color,
                TardyMinutes = a.TardyMinutes,
                Description = a.Description
            }).ToListAsync();

        // نام روز هفته فارسی
        var dayNames = new[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" };
        foreach (var row in list)
        {
            row.DayOfWeek = dayNames[(int)row.AttendanceDate.DayOfWeek];
        }
        return list;
    }

    public async Task<List<DisciplinaryRecordDto>> GetDisciplinaryRecordsAsync(int studentId)
    {
        return await _db.DisciplinaryRecords
            .Include(d => d.Type)
            .Include(d => d.Classroom)
            .Include(d => d.RecordedBy).ThenInclude(s => s.Person)
            .Where(d => d.StudentId == studentId)
            .AsNoTracking()
            .OrderByDescending(d => d.RecordDate)
            .Select(d => new DisciplinaryRecordDto
            {
                RecordId = d.RecordId, StudentId = d.StudentId,
                ClassroomId = d.ClassroomId, ClassroomName = d.Classroom.Name,
                TypeId = d.TypeId, TypeName = d.Type.Name, Category = d.Type.Category,
                RecordDate = d.RecordDate, Description = d.Description,
                ActionTaken = d.ActionTaken, ScoreImpact = d.ScoreImpact,
                IsParentNotified = d.IsParentNotified,
                RecordedByName = d.RecordedBy.Person.FirstName + " " + d.RecordedBy.Person.LastName
            }).ToListAsync();
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(int studentId)
    {
        return await _db.StudentInvoices
            .Include(i => i.FeeType).Include(i => i.School).Include(i => i.AcademicYear)
            .Include(i => i.Payments)
            .Where(i => i.StudentId == studentId)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvoiceDto
            {
                InvoiceId = i.InvoiceId, InvoiceNumber = i.InvoiceNumber,
                StudentId = i.StudentId,
                FeeTypeId = i.FeeTypeId, FeeTypeName = i.FeeType.Name,
                SchoolId = i.SchoolId, SchoolName = i.School.Name,
                AcademicYearTitle = i.AcademicYear.Title,
                Amount = i.Amount, Discount = i.Discount, NetAmount = i.NetAmount,
                PaidAmount = i.Payments.Sum(p => p.Amount),
                DueDate = i.DueDate, Status = i.Status,
                Description = i.Description, CreatedAt = i.CreatedAt
            }).ToListAsync();
    }

    public async Task<List<AnnouncementDto>> GetAnnouncementsAsync(int studentId, string audience)
    {
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId
                && (activeYear == null || e.AcademicYearId == activeYear.AcademicYearId)
                && e.Status == "فعال");

        var schoolId = enrollment?.Classroom?.SchoolId;
        var classroomId = enrollment?.ClassroomId;
        var now = DateTime.UtcNow;

        var q = _db.Announcements.AsNoTracking()
            .Where(a => a.IsActive
                && a.PublishDate <= now
                && (!a.ExpiryDate.HasValue || a.ExpiryDate > now)
                && (a.TargetAudience == "All" || a.TargetAudience == audience)
                && (a.SchoolId == null || a.SchoolId == schoolId)
                && (a.ClassroomId == null || a.ClassroomId == classroomId));

        return await q.OrderByDescending(a => a.PublishDate)
            .Select(a => new AnnouncementDto
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title, Body = a.Body,
                TargetAudience = a.TargetAudience,
                PublishDate = a.PublishDate, ExpiryDate = a.ExpiryDate
            }).Take(20).ToListAsync();
    }
}

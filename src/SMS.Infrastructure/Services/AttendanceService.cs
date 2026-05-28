using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly SmsDbContext _db;
    public AttendanceService(SmsDbContext db) => _db = db;

    public async Task<List<AttendanceStatusDto>> GetStatusesAsync()
    {
        return await _db.AttendanceStatuses.AsNoTracking()
            .OrderBy(s => s.StatusId)
            .Select(s => new AttendanceStatusDto
            {
                StatusId = s.StatusId, Name = s.Name, Code = s.Code,
                IsAbsent = s.IsAbsent, IsTardy = s.IsTardy, Color = s.Color
            }).ToListAsync();
    }

    public async Task<TakeAttendanceDto> GetForDateAsync(int classroomId, DateTime date, int? classSubjectId = null)
    {
        var students = await _db.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Where(e => e.ClassroomId == classroomId && e.Status == "فعال")
            .AsNoTracking()
            .OrderBy(e => e.Student.Person.LastName).ThenBy(e => e.Student.Person.FirstName)
            .Select(e => new
            {
                e.StudentId,
                e.Student.StudentCode,
                FullName = e.Student.Person.FirstName + " " + e.Student.Person.LastName,
                e.Student.Person.FatherName
            }).ToListAsync();

        var dateOnly = date.Date;
        var existing = await _db.Attendances.AsNoTracking()
            .Where(a => a.ClassroomId == classroomId
                && a.AttendanceDate == dateOnly
                && a.ClassSubjectId == classSubjectId)
            .ToListAsync();

        var dto = new TakeAttendanceDto
        {
            ClassroomId = classroomId,
            AttendanceDate = dateOnly,
            ClassSubjectId = classSubjectId,
            Students = students.Select(s =>
            {
                var ex = existing.FirstOrDefault(a => a.StudentId == s.StudentId);
                return new AttendanceStudentRow
                {
                    StudentId = s.StudentId,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    FatherName = s.FatherName,
                    StatusId = ex?.StatusId ?? (byte)1, // پیش‌فرض حاضر
                    TardyMinutes = ex?.TardyMinutes,
                    Description = ex?.Description,
                    ExistingAttendanceId = ex?.AttendanceId
                };
            }).ToList()
        };

        return dto;
    }

    public async Task<Result> SaveAsync(TakeAttendanceDto dto, int recordedByStaffId)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dateOnly = dto.AttendanceDate.Date;

            // حذف رکوردهای قبلی همان روز (برای overwrite)
            var existing = await _db.Attendances
                .Where(a => a.ClassroomId == dto.ClassroomId
                    && a.AttendanceDate == dateOnly
                    && a.ClassSubjectId == dto.ClassSubjectId)
                .ToListAsync();
            if (existing.Any())
                _db.Attendances.RemoveRange(existing);

            // اضافه کردن رکوردهای جدید
            foreach (var s in dto.Students)
            {
                _db.Attendances.Add(new Attendance
                {
                    StudentId = s.StudentId,
                    ClassroomId = dto.ClassroomId,
                    ClassSubjectId = dto.ClassSubjectId,
                    AttendanceDate = dateOnly,
                    StatusId = s.StatusId,
                    TardyMinutes = s.TardyMinutes,
                    Description = s.Description,
                    RecordedByStaffId = recordedByStaffId
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Result.Ok("حضور و غیاب با موفقیت ثبت شد");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result.Fail($"خطا: {ex.Message}");
        }
    }

    public async Task<List<AttendanceReportRow>> GetReportAsync(int classroomId, DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        var students = await _db.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Where(e => e.ClassroomId == classroomId && e.Status == "فعال")
            .AsNoTracking()
            .Select(e => new
            {
                e.StudentId, e.Student.StudentCode,
                FullName = e.Student.Person.FirstName + " " + e.Student.Person.LastName
            }).ToListAsync();

        var attendances = await _db.Attendances
            .Include(a => a.Status)
            .Where(a => a.ClassroomId == classroomId && a.AttendanceDate >= from && a.AttendanceDate <= to)
            .AsNoTracking()
            .ToListAsync();

        return students.Select(st =>
        {
            var stAtt = attendances.Where(a => a.StudentId == st.StudentId).ToList();
            return new AttendanceReportRow
            {
                StudentId = st.StudentId,
                StudentCode = st.StudentCode,
                FullName = st.FullName,
                PresentCount = stAtt.Count(a => a.Status.Code == "PRESENT"),
                UnexcusedAbsenceCount = stAtt.Count(a => a.Status.Code == "ABSENT"),
                ExcusedAbsenceCount = stAtt.Count(a => a.Status.Code == "EXCUSED"),
                TardyCount = stAtt.Count(a => a.Status.Code == "TARDY"),
                LeaveCount = stAtt.Count(a => a.Status.Code == "LEAVE")
            };
        }).OrderBy(r => r.FullName).ToList();
    }

    public async Task<List<AttendanceReportRow>> GetStudentReportAsync(int studentId, DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        var student = await _db.Students.Include(s => s.Person)
            .AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student is null) return new List<AttendanceReportRow>();

        var stAtt = await _db.Attendances.Include(a => a.Status)
            .Where(a => a.StudentId == studentId && a.AttendanceDate >= from && a.AttendanceDate <= to)
            .AsNoTracking().ToListAsync();

        return new List<AttendanceReportRow>
        {
            new AttendanceReportRow
            {
                StudentId = studentId,
                StudentCode = student.StudentCode,
                FullName = student.Person.FirstName + " " + student.Person.LastName,
                PresentCount = stAtt.Count(a => a.Status.Code == "PRESENT"),
                UnexcusedAbsenceCount = stAtt.Count(a => a.Status.Code == "ABSENT"),
                ExcusedAbsenceCount = stAtt.Count(a => a.Status.Code == "EXCUSED"),
                TardyCount = stAtt.Count(a => a.Status.Code == "TARDY"),
                LeaveCount = stAtt.Count(a => a.Status.Code == "LEAVE")
            }
        };
    }
}

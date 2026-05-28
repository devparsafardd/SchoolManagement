using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly SmsDbContext _db;
    public ScheduleService(SmsDbContext db) => _db = db;

    public async Task<List<SchoolPeriodDto>> GetPeriodsAsync(int schoolId)
    {
        return await _db.SchoolPeriods.AsNoTracking()
            .Where(p => p.SchoolId == schoolId && p.IsActive)
            .OrderBy(p => p.PeriodNo)
            .Select(p => new SchoolPeriodDto
            {
                PeriodId = p.PeriodId, SchoolId = p.SchoolId,
                PeriodNo = p.PeriodNo, Name = p.Name,
                StartTime = p.StartTime.ToString(@"hh\:mm"),
                EndTime = p.EndTime.ToString(@"hh\:mm"),
                IsBreak = p.IsBreak, IsActive = p.IsActive
            }).ToListAsync();
    }

    public async Task<Result<int>> CreatePeriodAsync(SchoolPeriodCreateDto dto)
    {
        if (await _db.SchoolPeriods.AnyAsync(p => p.SchoolId == dto.SchoolId && p.PeriodNo == dto.PeriodNo))
            return Result<int>.Fail("این شماره زنگ قبلاً ثبت شده");

        if (!TimeSpan.TryParse(dto.StartTime, out var st) || !TimeSpan.TryParse(dto.EndTime, out var et))
            return Result<int>.Fail("ساعت نامعتبر");

        var p = new SchoolPeriod
        {
            SchoolId = dto.SchoolId, PeriodNo = dto.PeriodNo,
            Name = dto.Name, StartTime = st, EndTime = et, IsBreak = dto.IsBreak
        };
        _db.SchoolPeriods.Add(p);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(p.PeriodId, "زنگ ثبت شد");
    }

    public async Task<Result> UpdatePeriodAsync(int periodId, SchoolPeriodCreateDto dto)
    {
        var p = await _db.SchoolPeriods.FirstOrDefaultAsync(x => x.PeriodId == periodId);
        if (p is null) return Result.Fail("زنگ یافت نشد");
        if (!TimeSpan.TryParse(dto.StartTime, out var st) || !TimeSpan.TryParse(dto.EndTime, out var et))
            return Result.Fail("ساعت نامعتبر");
        p.Name = dto.Name; p.StartTime = st; p.EndTime = et; p.IsBreak = dto.IsBreak;
        await _db.SaveChangesAsync();
        return Result.Ok("به‌روزرسانی شد");
    }

    public async Task<Result> DeletePeriodAsync(int periodId)
    {
        var p = await _db.SchoolPeriods.FirstOrDefaultAsync(x => x.PeriodId == periodId);
        if (p is null) return Result.Fail("زنگ یافت نشد");
        if (await _db.ClassSchedules.AnyAsync(s => s.PeriodId == periodId && s.IsActive))
            return Result.Fail("این زنگ در برنامه هفتگی استفاده شده");
        p.IsActive = false;
        await _db.SaveChangesAsync();
        return Result.Ok("حذف شد");
    }

    public async Task<List<ClassScheduleDto>> GetClassroomScheduleAsync(int classroomId)
    {
        return await _db.ClassSchedules.AsNoTracking()
            .Include(s => s.Period)
            .Include(s => s.Classroom)
            .Include(s => s.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(s => s.ClassSubject).ThenInclude(cs => cs.Staff).ThenInclude(st => st.Person)
            .Where(s => s.ClassroomId == classroomId && s.IsActive)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.Period.StartTime)
            .Select(s => new ClassScheduleDto
            {
                ScheduleId = s.ScheduleId,
                ClassroomId = s.ClassroomId,
                ClassroomName = s.Classroom.Name,
                ClassSubjectId = s.ClassSubjectId,
                SubjectName = s.ClassSubject.GradeSubject.Subject.Name,
                TeacherName = s.ClassSubject.Staff.Person.FirstName + " " + s.ClassSubject.Staff.Person.LastName,
                PeriodId = s.PeriodId,
                PeriodNo = s.Period.PeriodNo,
                PeriodName = s.Period.Name,
                StartTime = s.Period.StartTime.ToString(@"hh\:mm"),
                EndTime = s.Period.EndTime.ToString(@"hh\:mm"),
                DayOfWeek = s.DayOfWeek,
                RoomNumber = s.RoomNumber
            }).ToListAsync();
    }

    public async Task<Result<int>> AssignScheduleAsync(ClassScheduleCreateDto dto)
    {
        if (await _db.ClassSchedules.AnyAsync(s => s.ClassroomId == dto.ClassroomId
            && s.DayOfWeek == dto.DayOfWeek && s.PeriodId == dto.PeriodId && s.IsActive))
            return Result<int>.Fail("این زنگ از قبل به درس دیگری تخصیص داده شده");

        var s = new ClassSchedule
        {
            ClassroomId = dto.ClassroomId,
            ClassSubjectId = dto.ClassSubjectId,
            PeriodId = dto.PeriodId,
            DayOfWeek = dto.DayOfWeek,
            RoomNumber = dto.RoomNumber
        };
        _db.ClassSchedules.Add(s);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(s.ScheduleId, "تخصیص داده شد");
    }

    public async Task<Result> RemoveScheduleAsync(int scheduleId)
    {
        var s = await _db.ClassSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);
        if (s is null) return Result.Fail("یافت نشد");
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return Result.Ok("حذف شد");
    }
}

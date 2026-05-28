using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly SmsDbContext _db;
    public CalendarService(SmsDbContext db) => _db = db;

    public async Task<List<CalendarEventDto>> GetEventsAsync(DateTime fromDate, DateTime toDate, int? schoolId = null, int? classroomId = null)
    {
        var q = _db.CalendarEvents.AsNoTracking()
            .Include(e => e.School).Include(e => e.Classroom)
            .Where(e => e.EndDate >= fromDate && e.StartDate <= toDate);
        if (schoolId.HasValue)
            q = q.Where(e => e.SchoolId == null || e.SchoolId == schoolId);
        if (classroomId.HasValue)
            q = q.Where(e => e.ClassroomId == null || e.ClassroomId == classroomId);

        return await q.OrderBy(e => e.StartDate)
            .Select(e => new CalendarEventDto
            {
                EventId = e.EventId, SchoolId = e.SchoolId,
                SchoolName = e.School != null ? e.School.Name : null,
                ClassroomId = e.ClassroomId,
                ClassroomName = e.Classroom != null ? e.Classroom.Name : null,
                Title = e.Title, Description = e.Description,
                StartDate = e.StartDate, EndDate = e.EndDate,
                IsAllDay = e.IsAllDay, EventType = e.EventType,
                Color = e.Color, TargetAudience = e.TargetAudience,
                SendNotification = e.SendNotification
            }).ToListAsync();
    }

    public async Task<List<CalendarEventDto>> GetUpcomingAsync(int days = 30, int? schoolId = null)
        => await GetEventsAsync(DateTime.Today, DateTime.Today.AddDays(days), schoolId);

    public async Task<Result<CalendarEventDto>> GetByIdAsync(int eventId)
    {
        var e = await _db.CalendarEvents.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == eventId);
        if (e is null) return Result<CalendarEventDto>.Fail("رویداد یافت نشد");
        return Result<CalendarEventDto>.Ok(new CalendarEventDto
        {
            EventId = e.EventId, SchoolId = e.SchoolId, ClassroomId = e.ClassroomId,
            Title = e.Title, Description = e.Description,
            StartDate = e.StartDate, EndDate = e.EndDate, IsAllDay = e.IsAllDay,
            EventType = e.EventType, Color = e.Color,
            TargetAudience = e.TargetAudience, SendNotification = e.SendNotification
        });
    }

    public async Task<Result<int>> CreateAsync(CalendarEventCreateDto dto, int createdByUserId)
    {
        var e = new CalendarEvent
        {
            SchoolId = dto.SchoolId, ClassroomId = dto.ClassroomId,
            Title = dto.Title, Description = dto.Description,
            StartDate = dto.StartDate.Date,
            EndDate = (dto.EndDate < dto.StartDate ? dto.StartDate : dto.EndDate).Date,
            IsAllDay = dto.IsAllDay, EventType = dto.EventType,
            Color = dto.Color, TargetAudience = dto.TargetAudience,
            SendNotification = dto.SendNotification,
            CreatedByUserId = createdByUserId
        };
        _db.CalendarEvents.Add(e);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(e.EventId, "رویداد ثبت شد");
    }

    public async Task<Result> UpdateAsync(int eventId, CalendarEventCreateDto dto)
    {
        var e = await _db.CalendarEvents.FirstOrDefaultAsync(x => x.EventId == eventId);
        if (e is null) return Result.Fail("رویداد یافت نشد");
        e.Title = dto.Title; e.Description = dto.Description;
        e.StartDate = dto.StartDate.Date; e.EndDate = dto.EndDate.Date;
        e.IsAllDay = dto.IsAllDay; e.EventType = dto.EventType;
        e.Color = dto.Color; e.TargetAudience = dto.TargetAudience;
        e.SendNotification = dto.SendNotification;
        await _db.SaveChangesAsync();
        return Result.Ok("به‌روزرسانی شد");
    }

    public async Task<Result> DeleteAsync(int eventId)
    {
        var e = await _db.CalendarEvents.FirstOrDefaultAsync(x => x.EventId == eventId);
        if (e is null) return Result.Fail("یافت نشد");
        _db.CalendarEvents.Remove(e);
        await _db.SaveChangesAsync();
        return Result.Ok("حذف شد");
    }
}

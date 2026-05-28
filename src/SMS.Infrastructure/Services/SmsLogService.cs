using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.Common.Sms;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class SmsLogService : ISmsLogService
{
    private readonly SmsDbContext _db;
    private readonly ISmsSender _sender;

    public SmsLogService(SmsDbContext db, ISmsSender sender)
    {
        _db = db; _sender = sender;
    }

    public async Task<PagedResult<SmsLogDto>> GetPagedAsync(int page = 1, int pageSize = 50)
    {
        var total = await _db.SmsLogs.CountAsync();
        var items = await _db.SmsLogs.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new SmsLogDto
            {
                SmsLogId = l.SmsLogId, Mobile = l.Mobile, Text = l.Text,
                Status = l.Status, FailReason = l.FailReason, CreatedAt = l.CreatedAt
            }).ToListAsync();

        return new PagedResult<SmsLogDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result> SendAsync(SendSmsDto dto, int? sentByUserId)
    {
        var result = await _sender.SendAsync(dto.Mobile, dto.Text);

        _db.SmsLogs.Add(new SmsLog
        {
            Mobile = dto.Mobile, Text = dto.Text,
            Status = result.Success ? "Sent" : "Failed",
            ProviderMessageId = result.ProviderMessageId,
            FailReason = result.Success ? null : result.Message,
            Cost = result.Cost, SentByUserId = sentByUserId
        });
        await _db.SaveChangesAsync();
        return result.Success ? Result.Ok("پیامک ارسال شد") : Result.Fail(result.Message ?? "ارسال ناموفق");
    }

    public async Task<Result> SendBulkAsync(SendBulkSmsDto dto, int? sentByUserId)
    {
        // پیدا کردن مخاطبان
        var mobiles = new List<(string mobile, int? studentId, int? guardianId)>();

        IQueryable<StudentEnrollment> enrollmentQ = _db.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Where(e => e.Status == "فعال");

        if (dto.ClassroomId.HasValue)
            enrollmentQ = enrollmentQ.Where(e => e.ClassroomId == dto.ClassroomId);
        else if (dto.SchoolId.HasValue)
            enrollmentQ = enrollmentQ.Where(e => e.Classroom.SchoolId == dto.SchoolId);

        var students = await enrollmentQ.Select(e => e.Student).ToListAsync();

        if (dto.Audience == "Students")
        {
            foreach (var s in students.Where(s => !string.IsNullOrEmpty(s.Person.Mobile)))
                mobiles.Add((s.Person.Mobile!, s.StudentId, null));
        }
        else // Parents
        {
            var studentIds = students.Select(s => s.StudentId).ToList();
            var guardians = await _db.StudentGuardians
                .Include(sg => sg.Guardian).ThenInclude(g => g.Person)
                .Where(sg => studentIds.Contains(sg.StudentId) && sg.IsPrimary)
                .ToListAsync();
            foreach (var g in guardians.Where(g => !string.IsNullOrEmpty(g.Guardian.Person.Mobile)))
                mobiles.Add((g.Guardian.Person.Mobile!, g.StudentId, g.GuardianId));
        }

        int success = 0, failed = 0;
        foreach (var (mobile, studentId, guardianId) in mobiles)
        {
            var result = await _sender.SendAsync(mobile, dto.Text);
            _db.SmsLogs.Add(new SmsLog
            {
                StudentId = studentId, GuardianId = guardianId,
                Mobile = mobile, Text = dto.Text,
                Status = result.Success ? "Sent" : "Failed",
                FailReason = result.Success ? null : result.Message,
                SentByUserId = sentByUserId
            });
            if (result.Success) success++; else failed++;
        }
        await _db.SaveChangesAsync();
        return Result.Ok($"تعداد {success} موفق، {failed} ناموفق از {mobiles.Count} مخاطب");
    }
}

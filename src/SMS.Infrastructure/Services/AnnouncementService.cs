using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly SmsDbContext _db;
    public AnnouncementService(SmsDbContext db) => _db = db;

    public async Task<PagedResult<AnnouncementDto>> GetPagedAsync(int? schoolId, int page = 1, int pageSize = 20)
    {
        var q = _db.Announcements.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) q = q.Where(a => a.SchoolId == null || a.SchoolId == schoolId);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.PublishDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AnnouncementDto
            {
                AnnouncementId = a.AnnouncementId,
                SchoolId = a.SchoolId,
                ClassroomId = a.ClassroomId,
                Title = a.Title, Body = a.Body,
                TargetAudience = a.TargetAudience,
                PublishDate = a.PublishDate, ExpiryDate = a.ExpiryDate,
                IsActive = a.IsActive
            }).ToListAsync();

        return new PagedResult<AnnouncementDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<AnnouncementDto>> GetActiveForAudienceAsync(string audience, int? schoolId = null)
    {
        var now = DateTime.UtcNow;
        var q = _db.Announcements.AsNoTracking()
            .Where(a => a.IsActive
                && a.PublishDate <= now
                && (!a.ExpiryDate.HasValue || a.ExpiryDate > now)
                && (a.TargetAudience == "All" || a.TargetAudience == audience));
        if (schoolId.HasValue) q = q.Where(a => a.SchoolId == null || a.SchoolId == schoolId);

        return await q.OrderByDescending(a => a.PublishDate)
            .Select(a => new AnnouncementDto
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title, Body = a.Body,
                TargetAudience = a.TargetAudience,
                PublishDate = a.PublishDate
            }).ToListAsync();
    }

    public async Task<Result<int>> CreateAsync(AnnouncementCreateDto dto, int createdByUserId)
    {
        var a = new Announcement
        {
            SchoolId = dto.SchoolId, ClassroomId = dto.ClassroomId,
            Title = dto.Title, Body = dto.Body,
            TargetAudience = dto.TargetAudience,
            PublishDate = DateTime.UtcNow,
            ExpiryDate = dto.ExpiryDate,
            CreatedByUserId = createdByUserId,
            IsActive = true
        };
        _db.Announcements.Add(a);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(a.AnnouncementId, "اعلان منتشر شد");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var a = await _db.Announcements.FindAsync(id);
        if (a is null) return Result.Fail("یافت نشد");
        _db.Announcements.Remove(a);
        await _db.SaveChangesAsync();
        return Result.Ok("اعلان حذف شد");
    }
}

using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class SettingService : ISettingService
{
    private readonly SmsDbContext _db;
    public SettingService(SmsDbContext db) => _db = db;

    public async Task<List<SystemSettingDto>> GetByCategoryAsync(string? category = null)
    {
        var q = _db.SystemSettings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(category)) q = q.Where(s => s.Category == category);

        return await q.OrderBy(s => s.Category).ThenBy(s => s.Key)
            .Select(s => new SystemSettingDto
            {
                SettingId = s.SettingId, Key = s.Key, Value = s.Value,
                Category = s.Category, Description = s.Description, ModifiedAt = s.ModifiedAt
            }).ToListAsync();
    }

    public async Task<string?> GetValueAsync(string key)
        => await _db.SystemSettings.AsNoTracking().Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();

    public async Task<Result> UpdateAsync(string key, string? value, int? userId)
    {
        var s = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (s is null) return Result.Fail("تنظیمات یافت نشد");
        s.Value = value;
        s.ModifiedAt = DateTime.UtcNow;
        s.ModifiedByUserId = userId;
        await _db.SaveChangesAsync();
        return Result.Ok("تنظیمات ذخیره شد");
    }
}

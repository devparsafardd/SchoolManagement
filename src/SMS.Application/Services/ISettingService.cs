using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس تنظیمات سیستم</summary>
public interface ISettingService
{
    Task<List<SystemSettingDto>> GetByCategoryAsync(string? category = null);
    Task<string?> GetValueAsync(string key);
    Task<Result> UpdateAsync(string key, string? value, int? userId);
}

using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس مدیریت پیامک‌های ارسالی</summary>
public interface ISmsLogService
{
    Task<PagedResult<SmsLogDto>> GetPagedAsync(int page = 1, int pageSize = 50);
    Task<Result> SendAsync(SendSmsDto dto, int? sentByUserId);
    Task<Result> SendBulkAsync(SendBulkSmsDto dto, int? sentByUserId);
}

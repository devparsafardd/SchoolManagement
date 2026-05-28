using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس لاگ‌های Audit (تاریخچه تغییرات سیستم)</summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(int? userId, string? entityName, string? action,
        DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50);
    Task<AuditLogDto?> GetByIdAsync(long auditId);
}

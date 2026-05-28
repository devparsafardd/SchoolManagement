using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly SmsDbContext _db;
    public AuditLogService(SmsDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(int? userId, string? entityName, string? action,
        DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 50)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (userId.HasValue) q = q.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(entityName)) q = q.Where(a => a.EntityName == entityName);
        if (!string.IsNullOrEmpty(action)) q = q.Where(a => a.Action == action);
        if (fromDate.HasValue) q = q.Where(a => a.CreatedAt >= fromDate);
        if (toDate.HasValue) q = q.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                AuditId = a.AuditId, UserId = a.UserId, Username = a.Username,
                Action = a.Action, EntityName = a.EntityName, EntityId = a.EntityId,
                ChangedColumns = a.ChangedColumns, IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            }).ToListAsync();

        return new PagedResult<AuditLogDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<AuditLogDto?> GetByIdAsync(long auditId)
    {
        var a = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.AuditId == auditId);
        if (a is null) return null;
        return new AuditLogDto
        {
            AuditId = a.AuditId, UserId = a.UserId, Username = a.Username,
            Action = a.Action, EntityName = a.EntityName, EntityId = a.EntityId,
            ChangedColumns = a.ChangedColumns,
            OldValues = a.OldValues, NewValues = a.NewValues,
            IpAddress = a.IpAddress, CreatedAt = a.CreatedAt
        };
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SMS.Application.Common;
using SMS.Domain.Common;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor خودکار برای:
/// 1. پر کردن CreatedAt/ModifiedAt/CreatedByUserId/ModifiedByUserId
/// 2. تبدیل Delete به SoftDelete برای ISoftDeletable
/// 3. ثبت AuditLog برای تغییرات
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    // جدول‌هایی که نمی‌خواهیم برای آن‌ها AuditLog ثبت شود (جلوگیری از حلقه)
    private static readonly HashSet<string> IgnoredEntities = new()
    {
        nameof(AuditLog)
    };

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditAndSoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditAndSoftDelete(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var username = _currentUser.Username;
        var ip = _currentUser.IpAddress;
        var ua = _currentUser.UserAgent;

        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog) continue;
            var entityName = entry.Entity.GetType().Name;

            // 1) Audit fields
            if (entry.Entity is IAuditableEntity audit)
            {
                if (entry.State == EntityState.Added)
                {
                    audit.CreatedAt = now;
                    audit.CreatedByUserId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    audit.ModifiedAt = now;
                    audit.ModifiedByUserId = userId;
                    // CreatedAt قابل تغییر نیست
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.CreatedByUserId)).IsModified = false;
                }
            }

            // 2) Soft Delete
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable soft)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedAt = now;
                soft.DeletedByUserId = userId;
            }

            // 3) Audit Log
            if (IgnoredEntities.Contains(entityName)) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var log = new AuditLog
            {
                UserId = userId,
                Username = username,
                EntityName = entityName,
                Action = entry.State.ToString(),
                IpAddress = ip,
                UserAgent = ua,
                CreatedAt = now,
                EntityId = GetPrimaryKey(entry)
            };

            if (entry.State == EntityState.Modified)
            {
                var changed = entry.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)).ToList();
                if (changed.Count == 0) continue;
                log.ChangedColumns = string.Join(",", changed.Select(p => p.Metadata.Name));
                log.OldValues = SafeSerialize(changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                log.NewValues = SafeSerialize(changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
            else if (entry.State == EntityState.Added)
            {
                log.NewValues = SafeSerialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
            else // Deleted
            {
                log.OldValues = SafeSerialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
            }

            auditLogs.Add(log);
        }

        if (auditLogs.Count > 0)
            context.Set<AuditLog>().AddRange(auditLogs);
    }

    private static string? GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;
        var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString());
        return string.Join(",", values);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string? SafeSerialize(object? obj)
    {
        try { return JsonSerializer.Serialize(obj, JsonOpts); }
        catch { return null; }
    }
}

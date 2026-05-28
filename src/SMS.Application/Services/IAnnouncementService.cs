using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس مدیریت اعلان‌ها (Announcements)</summary>
public interface IAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetPagedAsync(int? schoolId, int page = 1, int pageSize = 20);
    Task<List<AnnouncementDto>> GetActiveForAudienceAsync(string audience, int? schoolId = null);
    Task<Result<int>> CreateAsync(AnnouncementCreateDto dto, int createdByUserId);
    Task<Result> DeleteAsync(int id);
}

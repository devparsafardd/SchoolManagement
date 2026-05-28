using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس پیام‌رسان داخلی بین کاربران سیستم</summary>
public interface IMessageService
{
    Task<MessageInboxSummary> GetInboxSummaryAsync(int userId);
    Task<List<MessageDto>> GetInboxAsync(int userId, bool unreadOnly = false);
    Task<List<MessageDto>> GetSentAsync(int userId);
    Task<Result<MessageDto>> GetByIdAsync(long id, int userId);
    Task<Result<long>> SendAsync(MessageSendDto dto, int fromUserId);
    Task<Result> MarkAsReadAsync(long id, int userId);
    Task<Result> DeleteAsync(long id, int userId);
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>کاربرانی که این کاربر می‌تواند به آن‌ها پیام دهد</summary>
    Task<List<MessageContactDto>> GetContactsAsync(int userId);
}

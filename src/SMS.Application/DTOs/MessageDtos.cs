namespace SMS.Application.DTOs;

public class MessageDto
{
    public long MessageId { get; set; }
    public int FromUserId { get; set; }
    public string? FromUserName { get; set; }
    public int ToUserId { get; set; }
    public string? ToUserName { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
    public string? Category { get; set; }
    public long? ReplyToMessageId { get; set; }
}

public class MessageSendDto
{
    public int ToUserId { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = null!;
    public string? Category { get; set; }
    public long? ReplyToMessageId { get; set; }
}

public class MessageInboxSummary
{
    public int TotalMessages { get; set; }
    public int UnreadCount { get; set; }
    public List<MessageDto> RecentMessages { get; set; } = new();
}

public class MessageContactDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? RoleDisplay { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>پیام بین کاربران سیستم (مثلاً معلم به ولی، مدیر به معلم و ...)</summary>
[Table("Messages")]
public class Message
{
    [Key]
    public long MessageId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }

    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required]
    public string Body { get; set; } = null!;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public bool IsDeletedBySender { get; set; }
    public bool IsDeletedByReceiver { get; set; }

    /// <summary>اگر پاسخ به پیام دیگری است</summary>
    public long? ReplyToMessageId { get; set; }

    /// <summary>دسته یا برچسب پیام (Notice/Question/Complaint/...)</summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}

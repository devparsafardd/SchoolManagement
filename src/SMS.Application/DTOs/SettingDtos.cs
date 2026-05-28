namespace SMS.Application.DTOs;

public class SystemSettingDto
{
    public int SettingId { get; set; }
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class AnnouncementDto
{
    public int AnnouncementId { get; set; }
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string TargetAudience { get; set; } = "All";
    public DateTime PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
}

public class AnnouncementCreateDto
{
    public int? SchoolId { get; set; }
    public int? ClassroomId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string TargetAudience { get; set; } = "All";
    public DateTime? ExpiryDate { get; set; }
}

public class SmsLogDto
{
    public long SmsLogId { get; set; }
    public string Mobile { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? FailReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendSmsDto
{
    public string Mobile { get; set; } = null!;
    public string Text { get; set; } = null!;
}

public class SendBulkSmsDto
{
    public int? ClassroomId { get; set; }
    public int? SchoolId { get; set; }
    public string Audience { get; set; } = "Parents"; // Parents / Students
    public string Text { get; set; } = null!;
}

namespace SMS.Application.DTOs;

public class CalendarEventDto
{
    public int EventId { get; set; }
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string EventType { get; set; } = "Other";
    public string EventTypeText => EventType switch
    {
        "Holiday" => "تعطیلی",
        "Exam" => "آزمون",
        "Trip" => "اردو",
        "Meeting" => "جلسه",
        "Ceremony" => "مراسم",
        _ => "سایر"
    };
    public string? Color { get; set; }
    public string TargetAudience { get; set; } = "All";
    public bool SendNotification { get; set; }
}

public class CalendarEventCreateDto
{
    public int? SchoolId { get; set; }
    public int? ClassroomId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public bool IsAllDay { get; set; } = true;
    public string EventType { get; set; } = "Other";
    public string? Color { get; set; }
    public string TargetAudience { get; set; } = "All";
    public bool SendNotification { get; set; }
}

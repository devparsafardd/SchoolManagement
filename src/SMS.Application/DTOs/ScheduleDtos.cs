namespace SMS.Application.DTOs;

public class SchoolPeriodDto
{
    public int PeriodId { get; set; }
    public int SchoolId { get; set; }
    public byte PeriodNo { get; set; }
    public string Name { get; set; } = null!;
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public bool IsBreak { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SchoolPeriodCreateDto
{
    public int SchoolId { get; set; }
    public byte PeriodNo { get; set; }
    public string Name { get; set; } = null!;
    public string StartTime { get; set; } = "08:00";
    public string EndTime { get; set; } = "08:45";
    public bool IsBreak { get; set; }
}

public class ClassScheduleDto
{
    public int ScheduleId { get; set; }
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public int ClassSubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string? TeacherName { get; set; }
    public int PeriodId { get; set; }
    public byte PeriodNo { get; set; }
    public string PeriodName { get; set; } = null!;
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public byte DayOfWeek { get; set; }
    public string DayName => DayOfWeek switch
    {
        0 => "شنبه", 1 => "یکشنبه", 2 => "دوشنبه", 3 => "سه‌شنبه",
        4 => "چهارشنبه", 5 => "پنج‌شنبه", 6 => "جمعه",
        _ => "—"
    };
    public string? RoomNumber { get; set; }
}

public class ClassScheduleCreateDto
{
    public int ClassroomId { get; set; }
    public int ClassSubjectId { get; set; }
    public int PeriodId { get; set; }
    public byte DayOfWeek { get; set; }
    public string? RoomNumber { get; set; }
}

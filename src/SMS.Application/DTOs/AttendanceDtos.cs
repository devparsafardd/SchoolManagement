namespace SMS.Application.DTOs;

public class AttendanceStudentRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FatherName { get; set; }

    // وضعیت فعلی (اگر قبلاً ثبت شده باشد)
    public byte StatusId { get; set; } = 1; // پیش‌فرض: حاضر
    public short? TardyMinutes { get; set; }
    public string? Description { get; set; }
    public long? ExistingAttendanceId { get; set; }
}

public class TakeAttendanceDto
{
    public int ClassroomId { get; set; }
    public DateTime AttendanceDate { get; set; } = DateTime.Today;
    public int? ClassSubjectId { get; set; }
    public List<AttendanceStudentRow> Students { get; set; } = new();
}

public class AttendanceStatusDto
{
    public byte StatusId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsAbsent { get; set; }
    public bool IsTardy { get; set; }
    public string? Color { get; set; }
}

public class AttendanceReportRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public int PresentCount { get; set; }
    public int UnexcusedAbsenceCount { get; set; }
    public int ExcusedAbsenceCount { get; set; }
    public int TardyCount { get; set; }
    public int LeaveCount { get; set; }
    public int TotalAbsences => UnexcusedAbsenceCount + ExcusedAbsenceCount;
}

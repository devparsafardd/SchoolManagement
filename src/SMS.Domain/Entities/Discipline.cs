using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>انواع رخدادهای انضباطی (تشویق/تنبیه)</summary>
[Table("DisciplinaryTypes")]
public class DisciplinaryType
{
    [Key]
    public int TypeId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = null!;

    /// <summary>'R' = Reward (تشویق) | 'P' = Punishment (تنبیه)</summary>
    [Required, MaxLength(1)] public string Category { get; set; } = "R";

    public byte? Severity { get; set; }  // ۱ تا ۵
    public decimal? DefaultScoreImpact { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("DisciplinaryRecords")]
public class DisciplinaryRecord
{
    [Key]
    public long RecordId { get; set; }
    public int StudentId { get; set; }
    public int ClassroomId { get; set; }
    public int AcademicYearId { get; set; }
    public int TypeId { get; set; }

    public DateTime RecordDate { get; set; }
    [Required, MaxLength(1000)] public string Description { get; set; } = null!;
    [MaxLength(500)] public string? ActionTaken { get; set; }
    public decimal? ScoreImpact { get; set; }

    public bool IsParentNotified { get; set; }
    public DateTime? NotifiedAt { get; set; }

    public int RecordedByStaffId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
    public DisciplinaryType Type { get; set; } = null!;
    public Staff RecordedBy { get; set; } = null!;
}

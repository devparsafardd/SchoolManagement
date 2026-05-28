using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("Students")]
public class Student
{
    [Key]
    public int StudentId { get; set; }
    public int PersonId { get; set; }

    [Required, MaxLength(30)]
    public string StudentCode { get; set; } = null!;

    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

    [MaxLength(5)]
    public string? BloodType { get; set; }

    [MaxLength(500)]
    public string? SpecialNeeds { get; set; }

    public bool IsActive { get; set; } = true;

    public Person Person { get; set; } = null!;
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
}

[Table("StudentEnrollments")]
public class StudentEnrollment
{
    [Key]
    public long EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int ClassroomId { get; set; }
    public int AcademicYearId { get; set; }
    public DateTime EnrollmentDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "فعال";

    public DateTime? LeaveDate { get; set; }
    [MaxLength(300)]
    public string? LeaveReason { get; set; }

    public Student Student { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
}

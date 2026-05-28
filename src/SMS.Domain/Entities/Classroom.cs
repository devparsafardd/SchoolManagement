using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("Classrooms")]
public class Classroom
{
    [Key]
    public int ClassroomId { get; set; }
    public int SchoolId { get; set; }
    public int AcademicYearId { get; set; }
    public int GradeId { get; set; }
    public int? MajorId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = null!; // "ششم الف"

    public short? Capacity { get; set; }
    public int? HeadTeacherStaffId { get; set; }

    [MaxLength(20)]
    public string? RoomNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public School School { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
    public Staff? HeadTeacher { get; set; }
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
}

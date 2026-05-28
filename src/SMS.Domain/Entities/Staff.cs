using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("Staff")]
public class Staff
{
    [Key]
    public int StaffId { get; set; }
    public int PersonId { get; set; }

    [MaxLength(30)]
    public string? PersonnelCode { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    [MaxLength(100)]
    public string? Degree { get; set; }

    [MaxLength(200)]
    public string? FieldOfStudy { get; set; }

    public DateTime? HireDate { get; set; }

    [MaxLength(26)]
    public string? IBAN { get; set; }

    public bool IsActive { get; set; } = true;

    public Person Person { get; set; } = null!;
    public ICollection<StaffSchoolAssignment> Assignments { get; set; } = new List<StaffSchoolAssignment>();
}

[Table("StaffSchoolAssignments")]
public class StaffSchoolAssignment
{
    [Key]
    public int AssignmentId { get; set; }
    public int StaffId { get; set; }
    public int SchoolId { get; set; }
    public int AcademicYearId { get; set; }

    /// <summary>Teacher / VicePrincipal / Principal / Counselor / Admin</summary>
    [Required, MaxLength(50)]
    public string Position { get; set; } = "Teacher";

    public decimal? WeeklyHours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Staff Staff { get; set; } = null!;
    public School School { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
}

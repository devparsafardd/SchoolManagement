using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("Provinces")]
public class Province
{
    [Key]
    public int ProvinceId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public ICollection<City> Cities { get; set; } = new List<City>();
}

[Table("Cities")]
public class City
{
    [Key]
    public int CityId { get; set; }
    public int ProvinceId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public Province Province { get; set; } = null!;
    public ICollection<School> Schools { get; set; } = new List<School>();
}

[Table("EducationLevels")]
public class EducationLevel
{
    [Key]
    public int EducationLevelId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    [Required, MaxLength(20)]
    public string Code { get; set; } = null!;
    /// <summary>ارزشیابی توصیفی است؟ (ابتدایی)</summary>
    public bool IsDescriptive { get; set; }
    public byte? MinGrade { get; set; }
    public byte? MaxGrade { get; set; }

    public ICollection<School> Schools { get; set; } = new List<School>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}

[Table("Grades")]
public class Grade
{
    [Key]
    public int GradeId { get; set; }
    public int EducationLevelId { get; set; }
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;
    public byte OrderNo { get; set; }

    public EducationLevel EducationLevel { get; set; } = null!;
}

[Table("Schools")]
public class School
{
    [Key]
    public int SchoolId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Code { get; set; } = null!;

    public int CityId { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    /// <summary>'M'=پسرانه، 'F'=دخترانه، 'B'=مختلط</summary>
    [Required, MaxLength(1)]
    public string Gender { get; set; } = "B";

    [MaxLength(50)]
    public string? SchoolType { get; set; }

    public int EducationLevelId { get; set; }
    public int? PrincipalUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public City City { get; set; } = null!;
    public EducationLevel EducationLevel { get; set; } = null!;
    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
}

[Table("AcademicYears")]
public class AcademicYear
{
    [Key]
    public int AcademicYearId { get; set; }

    [Required, MaxLength(30)]
    public string Title { get; set; } = null!; // "1404-1405"

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>اولیا (پدر، مادر، سرپرست)</summary>
[Table("Guardians")]
public class Guardian
{
    [Key]
    public int GuardianId { get; set; }
    public int PersonId { get; set; }

    [MaxLength(150)] public string? Occupation { get; set; }
    [MaxLength(30)] public string? WorkplacePhone { get; set; }
    [MaxLength(100)] public string? EducationLevel { get; set; }

    public Person Person { get; set; } = null!;
    public ICollection<StudentGuardian> Students { get; set; } = new List<StudentGuardian>();
}

[Table("StudentGuardians")]
public class StudentGuardian
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }

    /// <summary>پدر / مادر / عمو / خاله / ...</summary>
    [Required, MaxLength(30)] public string Relationship { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public bool HasCustody { get; set; } = true;
    public bool CanPickup { get; set; } = true;

    public Student Student { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
}

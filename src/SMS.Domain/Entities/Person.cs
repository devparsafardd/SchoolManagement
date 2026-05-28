using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>
/// جدول مرجع همه افراد سیستم (دانش‌آموز، معلم، اولیا، ...)
/// </summary>
[Table("Persons")]
public class Person
{
    [Key]
    public int PersonId { get; set; }

    [MaxLength(10)]
    public string? NationalCode { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(100)]
    public string? FatherName { get; set; }

    /// <summary>'M' = مرد، 'F' = زن</summary>
    [Required, MaxLength(1)]
    public string Gender { get; set; } = "M";

    public DateTime? BirthDate { get; set; }

    [MaxLength(100)]
    public string? BirthPlace { get; set; }

    [MaxLength(15)]
    public string? Mobile { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    // Navigation
    public Student? Student { get; set; }
    public Staff? Staff { get; set; }
    public User? User { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}

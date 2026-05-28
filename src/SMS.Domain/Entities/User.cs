using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("Users")]
public class User
{
    [Key]
    public int UserId { get; set; }
    public int PersonId { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(100)]
    public string? PasswordSalt { get; set; }

    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public short FailedLoginCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Person Person { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

[Table("Roles")]
public class Role
{
    [Key]
    public int RoleId { get; set; }
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = null!;
    [MaxLength(300)]
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

[Table("UserRoles")]
public class UserRole
{
    [Key]
    public long UserRoleId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int? SchoolId { get; set; }
    public int? AcademicYearId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public School? School { get; set; }
    public AcademicYear? AcademicYear { get; set; }
}

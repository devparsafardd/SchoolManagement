using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("SystemSettings")]
public class SystemSetting
{
    [Key]
    public int SettingId { get; set; }
    [Required, MaxLength(100)] public string Key { get; set; } = null!;
    public string? Value { get; set; }
    [MaxLength(50)] public string? Category { get; set; }
    [MaxLength(300)] public string? Description { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int? ModifiedByUserId { get; set; }
}

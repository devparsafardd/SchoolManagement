using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("FeeTypes")]
public class FeeType
{
    [Key]
    public int FeeTypeId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
    public bool IsRecurring { get; set; }
    public decimal? DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("StudentInvoices")]
public class StudentInvoice
{
    [Key]
    public long InvoiceId { get; set; }
    public int StudentId { get; set; }
    public int AcademicYearId { get; set; }
    public int SchoolId { get; set; }
    public int FeeTypeId { get; set; }

    [Required, MaxLength(30)] public string InvoiceNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; } // محاسباتی در سرویس
    public DateTime? DueDate { get; set; }

    /// <summary>صادر شده / پرداخت شده / لغو / پرداخت ناقص</summary>
    [Required, MaxLength(20)] public string Status { get; set; } = "صادرشده";

    [MaxLength(500)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public School School { get; set; } = null!;
    public FeeType FeeType { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

[Table("Payments")]
public class Payment
{
    [Key]
    public long PaymentId { get; set; }
    public long InvoiceId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>نقد / کارت / حواله / آنلاین</summary>
    [Required, MaxLength(30)] public string PaymentMethod { get; set; } = "نقد";

    [MaxLength(50)] public string? ReferenceNumber { get; set; }
    [MaxLength(300)] public string? Description { get; set; }

    public int? RecordedByStaffId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentInvoice Invoice { get; set; } = null!;
}

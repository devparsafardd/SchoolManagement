using FluentValidation;

namespace SMS.Application.DTOs;

public class FeeTypeDto
{
    public int FeeTypeId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsRecurring { get; set; }
    public decimal? DefaultAmount { get; set; }
    public bool IsActive { get; set; }
}

public class InvoiceDto
{
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public int FeeTypeId { get; set; }
    public string? FeeTypeName { get; set; }
    public int SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public string? AcademicYearTitle { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => NetAmount - PaidAmount;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InvoiceCreateDto
{
    public int StudentId { get; set; }
    public int FeeTypeId { get; set; }
    public int? SchoolId { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
}

public class PaymentDto
{
    public long PaymentId { get; set; }
    public long InvoiceId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
}

public class PaymentCreateDto
{
    public long InvoiceId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "نقد";
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
}

public class InvoiceCreateValidator : AbstractValidator<InvoiceCreateDto>
{
    public InvoiceCreateValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0);
        RuleFor(x => x.FeeTypeId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
    }
}

public class PaymentCreateValidator : AbstractValidator<PaymentCreateDto>
{
    public PaymentCreateValidator()
    {
        RuleFor(x => x.InvoiceId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty();
    }
}

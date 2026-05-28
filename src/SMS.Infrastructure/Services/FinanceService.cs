using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class FinanceService : IFinanceService
{
    private readonly SmsDbContext _db;
    private readonly INotificationService? _notification;
    public FinanceService(SmsDbContext db, INotificationService? notification = null)
    {
        _db = db; _notification = notification;
    }

    public async Task<List<FeeTypeDto>> GetFeeTypesAsync() =>
        await _db.FeeTypes.AsNoTracking().OrderBy(f => f.Name)
            .Select(f => new FeeTypeDto
            {
                FeeTypeId = f.FeeTypeId, Name = f.Name,
                IsRecurring = f.IsRecurring, DefaultAmount = f.DefaultAmount,
                IsActive = f.IsActive
            }).ToListAsync();

    public async Task<Result<int>> CreateFeeTypeAsync(string name, bool isRecurring, decimal? defaultAmount)
    {
        if (await _db.FeeTypes.AnyAsync(f => f.Name == name))
            return Result<int>.Fail("نوع شهریه با این نام موجود است");
        var f = new FeeType { Name = name, IsRecurring = isRecurring, DefaultAmount = defaultAmount };
        _db.FeeTypes.Add(f);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(f.FeeTypeId, "نوع شهریه اضافه شد");
    }

    public async Task<Result> ToggleFeeTypeAsync(int feeTypeId)
    {
        var f = await _db.FeeTypes.FindAsync(feeTypeId);
        if (f is null) return Result.Fail("یافت نشد");
        f.IsActive = !f.IsActive;
        await _db.SaveChangesAsync();
        return Result.Ok(f.IsActive ? "فعال شد" : "غیرفعال شد");
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(int? studentId, int? schoolId, string? status, int page = 1, int pageSize = 20)
    {
        var q = _db.StudentInvoices
            .Include(i => i.Student).ThenInclude(s => s.Person)
            .Include(i => i.School)
            .Include(i => i.FeeType)
            .Include(i => i.AcademicYear)
            .AsNoTracking().AsQueryable();

        if (studentId.HasValue) q = q.Where(i => i.StudentId == studentId);
        if (schoolId.HasValue) q = q.Where(i => i.SchoolId == schoolId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(i => i.Status == status);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InvoiceDto
            {
                InvoiceId = i.InvoiceId, InvoiceNumber = i.InvoiceNumber,
                StudentId = i.StudentId,
                StudentName = i.Student.Person.FirstName + " " + i.Student.Person.LastName,
                StudentCode = i.Student.StudentCode,
                FeeTypeId = i.FeeTypeId, FeeTypeName = i.FeeType.Name,
                SchoolId = i.SchoolId, SchoolName = i.School.Name,
                AcademicYearTitle = i.AcademicYear.Title,
                Amount = i.Amount, Discount = i.Discount, NetAmount = i.NetAmount,
                PaidAmount = i.Payments.Sum(p => p.Amount),
                DueDate = i.DueDate, Status = i.Status,
                Description = i.Description, CreatedAt = i.CreatedAt
            }).ToListAsync();

        return new PagedResult<InvoiceDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result<InvoiceDto>> GetInvoiceByIdAsync(long invoiceId)
    {
        var i = await _db.StudentInvoices
            .Include(x => x.Student).ThenInclude(s => s.Person)
            .Include(x => x.School).Include(x => x.FeeType).Include(x => x.AcademicYear)
            .Include(x => x.Payments)
            .AsNoTracking().FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
        if (i is null) return Result<InvoiceDto>.Fail("فاکتور یافت نشد");

        return Result<InvoiceDto>.Ok(new InvoiceDto
        {
            InvoiceId = i.InvoiceId, InvoiceNumber = i.InvoiceNumber,
            StudentId = i.StudentId,
            StudentName = i.Student.Person.FirstName + " " + i.Student.Person.LastName,
            StudentCode = i.Student.StudentCode,
            FeeTypeId = i.FeeTypeId, FeeTypeName = i.FeeType.Name,
            SchoolId = i.SchoolId, SchoolName = i.School.Name,
            AcademicYearTitle = i.AcademicYear.Title,
            Amount = i.Amount, Discount = i.Discount, NetAmount = i.NetAmount,
            PaidAmount = i.Payments.Sum(p => p.Amount),
            DueDate = i.DueDate, Status = i.Status,
            Description = i.Description, CreatedAt = i.CreatedAt
        });
    }

    public async Task<Result<long>> CreateInvoiceAsync(InvoiceCreateDto dto)
    {
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear is null) return Result<long>.Fail("سال تحصیلی فعال یافت نشد");

        int schoolId = dto.SchoolId ?? 0;
        if (schoolId == 0)
        {
            var enrollment = await _db.Enrollments.Include(e => e.Classroom)
                .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId
                    && e.AcademicYearId == activeYear.AcademicYearId && e.Status == "فعال");
            if (enrollment is null) return Result<long>.Fail("دانش‌آموز در کلاس فعالی نیست");
            schoolId = enrollment.Classroom.SchoolId;
        }

        var invoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        var invoice = new StudentInvoice
        {
            StudentId = dto.StudentId,
            AcademicYearId = activeYear.AcademicYearId,
            SchoolId = schoolId,
            FeeTypeId = dto.FeeTypeId,
            InvoiceNumber = invoiceNumber,
            Amount = dto.Amount,
            Discount = dto.Discount,
            NetAmount = dto.Amount - dto.Discount,
            DueDate = dto.DueDate,
            Status = "صادرشده",
            Description = dto.Description
        };
        _db.StudentInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        if (_notification != null) { try { await _notification.NotifyInvoiceCreatedAsync(invoice.InvoiceId); } catch { } }
        return Result<long>.Ok(invoice.InvoiceId, "فاکتور صادر شد");
    }

    public async Task<Result> DeleteInvoiceAsync(long invoiceId)
    {
        var i = await _db.StudentInvoices.Include(x => x.Payments).FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
        if (i is null) return Result.Fail("فاکتور یافت نشد");
        if (i.Payments.Any()) return Result.Fail("فاکتور دارای پرداخت است. ابتدا پرداخت‌ها را حذف کنید");

        _db.StudentInvoices.Remove(i);
        await _db.SaveChangesAsync();
        return Result.Ok("فاکتور حذف شد");
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(long invoiceId) =>
        await _db.Payments.Where(p => p.InvoiceId == invoiceId)
            .AsNoTracking().OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId, InvoiceId = p.InvoiceId,
                PaymentDate = p.PaymentDate, Amount = p.Amount,
                PaymentMethod = p.PaymentMethod, ReferenceNumber = p.ReferenceNumber,
                Description = p.Description
            }).ToListAsync();

    public async Task<Result<long>> AddPaymentAsync(PaymentCreateDto dto, int? recordedByStaffId)
    {
        var invoice = await _db.StudentInvoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId);
        if (invoice is null) return Result<long>.Fail("فاکتور یافت نشد");

        var totalPaid = invoice.Payments.Sum(p => p.Amount);
        var remaining = invoice.NetAmount - totalPaid;
        if (dto.Amount > remaining)
            return Result<long>.Fail($"مبلغ پرداخت بیشتر از باقی‌مانده ({remaining:N0}) است");

        var payment = new Payment
        {
            InvoiceId = dto.InvoiceId,
            PaymentDate = dto.PaymentDate.Date,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber,
            Description = dto.Description,
            RecordedByStaffId = recordedByStaffId
        };
        _db.Payments.Add(payment);

        var newTotal = totalPaid + dto.Amount;
        invoice.Status = newTotal >= invoice.NetAmount ? "پرداخت‌شده" : "پرداخت ناقص";

        await _db.SaveChangesAsync();
        if (_notification != null) { try { await _notification.NotifyPaymentReceivedAsync(payment.PaymentId); } catch { } }
        return Result<long>.Ok(payment.PaymentId, "پرداخت ثبت شد");
    }

    public async Task<Result> DeletePaymentAsync(long paymentId)
    {
        var p = await _db.Payments.Include(x => x.Invoice).ThenInclude(i => i.Payments).FirstOrDefaultAsync(x => x.PaymentId == paymentId);
        if (p is null) return Result.Fail("پرداخت یافت نشد");

        var invoice = p.Invoice;
        _db.Payments.Remove(p);

        var newTotal = invoice.Payments.Where(x => x.PaymentId != paymentId).Sum(x => x.Amount);
        invoice.Status = newTotal == 0 ? "صادرشده" : (newTotal >= invoice.NetAmount ? "پرداخت‌شده" : "پرداخت ناقص");

        await _db.SaveChangesAsync();
        return Result.Ok("پرداخت حذف شد");
    }

    public async Task<decimal> GetTotalDebtAsync(int? schoolId = null)
    {
        var q = _db.StudentInvoices.Where(i => i.Status != "لغو" && i.Status != "پرداخت‌شده");
        if (schoolId.HasValue) q = q.Where(i => i.SchoolId == schoolId);

        var totalInvoices = await q.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var invoiceIds = await q.Select(i => i.InvoiceId).ToListAsync();
        var totalPaid = await _db.Payments.Where(p => invoiceIds.Contains(p.InvoiceId)).SumAsync(p => (decimal?)p.Amount) ?? 0;
        return totalInvoices - totalPaid;
    }

    public async Task<decimal> GetTotalCollectedAsync(int? schoolId = null)
    {
        var q = _db.Payments.Include(p => p.Invoice).AsQueryable();
        if (schoolId.HasValue) q = q.Where(p => p.Invoice.SchoolId == schoolId);
        return await q.SumAsync(p => (decimal?)p.Amount) ?? 0;
    }
}

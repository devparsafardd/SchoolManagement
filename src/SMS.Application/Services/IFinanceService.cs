using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس مدیریت مالی (شهریه، فاکتور، پرداخت)</summary>
public interface IFinanceService
{
    // FeeTypes
    Task<List<FeeTypeDto>> GetFeeTypesAsync();
    Task<Result<int>> CreateFeeTypeAsync(string name, bool isRecurring, decimal? defaultAmount);
    Task<Result> ToggleFeeTypeAsync(int feeTypeId);

    // Invoices
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(int? studentId, int? schoolId, string? status, int page = 1, int pageSize = 20);
    Task<Result<InvoiceDto>> GetInvoiceByIdAsync(long invoiceId);
    Task<Result<long>> CreateInvoiceAsync(InvoiceCreateDto dto);
    Task<Result> DeleteInvoiceAsync(long invoiceId);

    // Payments
    Task<List<PaymentDto>> GetPaymentsAsync(long invoiceId);
    Task<Result<long>> AddPaymentAsync(PaymentCreateDto dto, int? recordedByStaffId);
    Task<Result> DeletePaymentAsync(long paymentId);

    // Summaries
    Task<decimal> GetTotalDebtAsync(int? schoolId = null);
    Task<decimal> GetTotalCollectedAsync(int? schoolId = null);
}

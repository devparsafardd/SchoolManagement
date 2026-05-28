using SMS.Application.Common;

namespace SMS.Application.Services;

/// <summary>
/// سرویس ارسال نوتیفیکیشن (SMS + Internal Message) بر اساس رویدادهای سیستم
/// </summary>
public interface INotificationService
{
    /// <summary>اعلان غیبت دانش‌آموز به ولی</summary>
    Task<Result> NotifyAbsenceAsync(int studentId, DateTime date);

    /// <summary>اعلان فاکتور جدید به ولی</summary>
    Task<Result> NotifyInvoiceCreatedAsync(long invoiceId);

    /// <summary>اعلان پرداخت موفق به ولی</summary>
    Task<Result> NotifyPaymentReceivedAsync(long paymentId);

    /// <summary>اعلان نمره نهایی شده به ولی</summary>
    Task<Result> NotifyExamFinalizedAsync(long examId);

    /// <summary>اعلان موارد انضباطی به ولی</summary>
    Task<Result> NotifyDisciplineAsync(long recordId);

    /// <summary>یادآوری دسته‌جمعی پرداخت معوقات</summary>
    Task<Result> NotifyOverduePaymentsAsync(int? schoolId = null);

    /// <summary>یادآوری تکالیف نزدیک به سررسید</summary>
    Task<Result> NotifyUpcomingHomeworksAsync();
}

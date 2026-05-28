using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Sms;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

/// <summary>
/// نوتیفیکیشن خودکار: SMS + پیام داخلی
/// چک می‌کند که آیا تنظیمات سیستم اجازه می‌دهد یا نه (SystemSetting)
/// </summary>
public class NotificationService : INotificationService
{
    private readonly SmsDbContext _db;
    private readonly ISmsSender _sms;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(SmsDbContext db, ISmsSender sms, ILogger<NotificationService> logger)
    {
        _db = db; _sms = sms; _logger = logger;
    }

    private async Task<bool> IsSettingEnabledAsync(string key, bool defaultValue = false)
    {
        var v = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(v)) return defaultValue;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }

    private async Task SendSmsToGuardiansAsync(int studentId, string text)
    {
        var mobiles = await _db.StudentGuardians.AsNoTracking()
            .Where(sg => sg.StudentId == studentId && sg.IsPrimary)
            .Include(sg => sg.Guardian).ThenInclude(g => g.Person)
            .Select(sg => new { sg.GuardianId, Mobile = sg.Guardian.Person.Mobile })
            .Where(x => x.Mobile != null)
            .ToListAsync();

        foreach (var m in mobiles)
        {
            try
            {
                var result = await _sms.SendAsync(m.Mobile!, text);
                _db.SmsLogs.Add(new SmsLog
                {
                    StudentId = studentId, GuardianId = m.GuardianId,
                    Mobile = m.Mobile!, Text = text,
                    Status = result.Success ? "Sent" : "Failed",
                    FailReason = result.Success ? null : result.Message,
                    ProviderMessageId = result.ProviderMessageId
                });
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS failed for guardian {GuardianId}", m.GuardianId); }
        }
        await _db.SaveChangesAsync();
    }

    private async Task SendInternalMessageToGuardiansAsync(int studentId, string subject, string body, string? category = null)
    {
        var guardianUserIds = await _db.StudentGuardians.AsNoTracking()
            .Where(sg => sg.StudentId == studentId && sg.IsPrimary)
            .Join(_db.Users, sg => sg.Guardian.PersonId, u => u.PersonId, (sg, u) => u.UserId)
            .ToListAsync();

        // فرستنده: یک "سیستم" - از اولین SuperAdmin استفاده می‌کنیم
        var systemUserId = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.Role.Name == "SuperAdmin" && ur.IsActive)
            .Select(ur => ur.UserId).FirstOrDefaultAsync();
        if (systemUserId == 0) return;

        foreach (var uid in guardianUserIds)
        {
            _db.Messages.Add(new Message
            {
                FromUserId = systemUserId, ToUserId = uid,
                Subject = subject, Body = body, Category = category ?? "Notice",
                SentAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<Result> NotifyAbsenceAsync(int studentId, DateTime date)
    {
        var enabled = await IsSettingEnabledAsync("AutoNotifyAbsence", true);
        if (!enabled) return Result.Ok("غیرفعال در تنظیمات");

        var student = await _db.Students.AsNoTracking().Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student is null) return Result.Fail("دانش‌آموز یافت نشد");

        var dateStr = SMS.Shared.Helpers.PersianDate.ToPersian(date);
        var text = $"اولیای محترم {student.Person.FirstName} {student.Person.LastName}، فرزند شما در تاریخ {dateStr} غایب بوده است.";

        await SendSmsToGuardiansAsync(studentId, text);
        await SendInternalMessageToGuardiansAsync(studentId, "غیبت فرزند شما", text, "Notice");
        return Result.Ok();
    }

    public async Task<Result> NotifyInvoiceCreatedAsync(long invoiceId)
    {
        var enabled = await IsSettingEnabledAsync("AutoNotifyInvoice", true);
        if (!enabled) return Result.Ok();

        var inv = await _db.StudentInvoices.AsNoTracking()
            .Include(i => i.Student).ThenInclude(s => s.Person)
            .Include(i => i.FeeType)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        if (inv is null) return Result.Fail("فاکتور یافت نشد");

        var due = inv.DueDate.HasValue ? SMS.Shared.Helpers.PersianDate.ToPersian(inv.DueDate.Value) : "—";
        var text = $"فاکتور جدید '{inv.FeeType.Name}' به مبلغ {inv.NetAmount:N0} تومان برای {inv.Student.Person.FirstName} {inv.Student.Person.LastName} صادر شد. مهلت پرداخت: {due}";

        await SendSmsToGuardiansAsync(inv.StudentId, text);
        await SendInternalMessageToGuardiansAsync(inv.StudentId, "فاکتور جدید صادر شد", text, "Notice");
        return Result.Ok();
    }

    public async Task<Result> NotifyPaymentReceivedAsync(long paymentId)
    {
        var enabled = await IsSettingEnabledAsync("AutoNotifyPayment", true);
        if (!enabled) return Result.Ok();

        var pay = await _db.Payments.AsNoTracking()
            .Include(p => p.Invoice).ThenInclude(i => i.Student).ThenInclude(s => s.Person)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        if (pay is null) return Result.Fail("پرداخت یافت نشد");

        var text = $"پرداخت {pay.Amount:N0} تومان برای {pay.Invoice.Student.Person.FirstName} {pay.Invoice.Student.Person.LastName} با موفقیت ثبت شد. کد رهگیری: {pay.ReferenceNumber ?? pay.PaymentId.ToString()}";

        await SendSmsToGuardiansAsync(pay.Invoice.StudentId, text);
        await SendInternalMessageToGuardiansAsync(pay.Invoice.StudentId, "پرداخت ثبت شد", text, "Notice");
        return Result.Ok();
    }

    public async Task<Result> NotifyExamFinalizedAsync(long examId)
    {
        var enabled = await IsSettingEnabledAsync("AutoNotifyExamScore", false);
        if (!enabled) return Result.Ok();

        var exam = await _db.Exams.AsNoTracking()
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .FirstOrDefaultAsync(e => e.ExamId == examId);
        if (exam is null) return Result.Fail("آزمون یافت نشد");

        var scores = await _db.ExamScores.AsNoTracking()
            .Include(s => s.Student).ThenInclude(st => st.Person)
            .Include(s => s.DescriptiveScaleItem)
            .Where(s => s.ExamId == examId).ToListAsync();

        foreach (var s in scores)
        {
            string scoreText = s.NumericScore.HasValue
                ? $"{s.NumericScore:0.##} از {exam.MaxScore:0}"
                : (s.DescriptiveScaleItem?.Label ?? "—");
            var text = $"نمره '{exam.Title}' (درس {exam.ClassSubject.GradeSubject.Subject.Name}) برای {s.Student.Person.FirstName} {s.Student.Person.LastName}: {scoreText}";
            await SendSmsToGuardiansAsync(s.StudentId, text);
            await SendInternalMessageToGuardiansAsync(s.StudentId, $"نمره آزمون '{exam.Title}'", text, "Notice");
        }
        return Result.Ok();
    }

    public async Task<Result> NotifyDisciplineAsync(long recordId)
    {
        var enabled = await IsSettingEnabledAsync("AutoNotifyDiscipline", true);
        if (!enabled) return Result.Ok();

        var rec = await _db.DisciplinaryRecords.AsNoTracking()
            .Include(d => d.Student).ThenInclude(s => s.Person)
            .Include(d => d.Type)
            .FirstOrDefaultAsync(d => d.RecordId == recordId);
        if (rec is null) return Result.Fail("یافت نشد");

        var verb = rec.Type.Category == "R" ? "تشویق" : "تذکر";
        var text = $"اولیای محترم {rec.Student.Person.FirstName} {rec.Student.Person.LastName}، {verb} برای فرزند شما ثبت شد: {rec.Type.Name}. توضیح: {rec.Description}";
        await SendSmsToGuardiansAsync(rec.StudentId, text);
        await SendInternalMessageToGuardiansAsync(rec.StudentId, $"{verb} انضباطی", text, "Notice");
        return Result.Ok();
    }

    public async Task<Result> NotifyOverduePaymentsAsync(int? schoolId = null)
    {
        var today = DateTime.Today;
        var q = _db.StudentInvoices.AsNoTracking()
            .Where(i => i.DueDate != null && i.DueDate < today && i.Status != "پرداخت‌شده");
        if (schoolId.HasValue) q = q.Where(i => i.SchoolId == schoolId);
        var overdueByStudent = await q.GroupBy(i => i.StudentId)
            .Select(g => new { StudentId = g.Key, Total = g.Sum(x => x.NetAmount), Count = g.Count() })
            .ToListAsync();

        int count = 0;
        foreach (var s in overdueByStudent)
        {
            var student = await _db.Students.AsNoTracking().Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.StudentId == s.StudentId);
            if (student is null) continue;
            var text = $"اولیای محترم {student.Person.FirstName} {student.Person.LastName}، شما {s.Count} فاکتور سررسید گذشته به مبلغ کل {s.Total:N0} تومان دارید. لطفاً نسبت به پرداخت اقدام کنید.";
            await SendSmsToGuardiansAsync(s.StudentId, text);
            await SendInternalMessageToGuardiansAsync(s.StudentId, "یادآوری پرداخت معوقات", text, "Notice");
            count++;
        }
        return Result.Ok($"به {count} ولی یادآوری ارسال شد");
    }

    public async Task<Result> NotifyUpcomingHomeworksAsync()
    {
        var tomorrow = DateTime.Today.AddDays(1);
        var upcoming = await _db.Homeworks.AsNoTracking()
            .Include(h => h.ClassSubject)
            .Where(h => h.IsActive && h.DueDate.Date == tomorrow)
            .ToListAsync();

        int count = 0;
        foreach (var hw in upcoming)
        {
            var students = await _db.Enrollments
                .Where(e => e.ClassroomId == hw.ClassSubject.ClassroomId && e.Status == "فعال")
                .Select(e => e.StudentId).ToListAsync();
            foreach (var sid in students)
            {
                var text = $"یادآوری: تکلیف '{hw.Title}' فردا تحویل دارد.";
                await SendInternalMessageToGuardiansAsync(sid, "یادآوری تکلیف", text, "Notice");
                count++;
            }
        }
        return Result.Ok($"{count} یادآوری ارسال شد");
    }
}

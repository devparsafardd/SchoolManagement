namespace SMS.Application.Common.Sms;

/// <summary>
/// سرویس ارسال پیامک - با Interface مجزا تا بشود بعداً به کاوه‌نگار/ملی پیامک/... متصل شد
/// </summary>
public interface ISmsSender
{
    Task<SmsResult> SendAsync(string mobile, string text, CancellationToken ct = default);
    Task<SmsResult> SendBulkAsync(IEnumerable<string> mobiles, string text, CancellationToken ct = default);
}

public class SmsResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ProviderMessageId { get; set; }
    public int? Cost { get; set; }

    public static SmsResult Ok(string? id = null) => new() { Success = true, ProviderMessageId = id };
    public static SmsResult Fail(string msg) => new() { Success = false, Message = msg };
}

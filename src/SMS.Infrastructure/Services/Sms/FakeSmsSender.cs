using Microsoft.Extensions.Logging;
using SMS.Application.Common.Sms;

namespace SMS.Infrastructure.Services.Sms;

/// <summary>
/// پیاده‌سازی موقت - فقط لاگ می‌کند
/// بعداً با KavehNegarSmsSender یا MelliPayamakSmsSender جایگزین می‌شود
/// </summary>
public class FakeSmsSender : ISmsSender
{
    private readonly ILogger<FakeSmsSender> _logger;
    public FakeSmsSender(ILogger<FakeSmsSender> logger) => _logger = logger;

    public Task<SmsResult> SendAsync(string mobile, string text, CancellationToken ct = default)
    {
        _logger.LogInformation("📱 [FAKE SMS] To: {Mobile} | Text: {Text}", mobile, text);
        return Task.FromResult(SmsResult.Ok(Guid.NewGuid().ToString()));
    }

    public async Task<SmsResult> SendBulkAsync(IEnumerable<string> mobiles, string text, CancellationToken ct = default)
    {
        int count = 0;
        foreach (var m in mobiles)
        {
            await SendAsync(m, text, ct);
            count++;
        }
        return SmsResult.Ok($"Sent to {count} recipients");
    }
}

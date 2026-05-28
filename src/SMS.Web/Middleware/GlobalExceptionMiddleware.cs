using System.Net;
using System.Text.Json;

namespace SMS.Web.Middleware;

/// <summary>
/// Middleware برای گرفتن خطاهای پیش‌بینی‌نشده
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next; _logger = logger; _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای پیش‌بینی‌نشده در {Path}", context.Request.Path);

            // اگر API بود (JSON)
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = new
                {
                    success = false,
                    message = _env.IsDevelopment() ? ex.Message : "خطایی در پردازش درخواست رخ داد",
                    detail = _env.IsDevelopment() ? ex.ToString() : null
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                return;
            }

            // اگر MVC بود → ریدایرکت به صفحه خطا
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}

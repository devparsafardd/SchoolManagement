using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Infrastructure;
using Serilog;
using SMS.Application.DTOs;
using SMS.Infrastructure;
using SMS.Infrastructure.Persistence;
using SMS.Web.Middleware;

// تنظیم لایسنس QuestPDF (رایگان برای کاربردهای متن‌باز/شخصی)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ====== Serilog ======
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/sms-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
);

// ====== MVC + Validation ======
builder.Services.AddControllersWithViews(opt =>
{
    // فعال کردن AntiForgery به صورت سراسری برای POST
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

// ====== Infrastructure ======
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<SMS.Web.Services.IFileUploadService, SMS.Web.Services.FileUploadService>();

// ====== Cookie Authentication ======
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Account/Login";
        opt.LogoutPath = "/Account/Logout";
        opt.AccessDeniedPath = "/Account/AccessDenied";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "SMS.Auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.Name = "SMS.Session";
});

// ====== Localization فارسی ======
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(opt =>
{
    var culture = new System.Globalization.CultureInfo("fa-IR");
    culture.NumberFormat.NumberDecimalSeparator = ".";
    opt.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture);
    opt.SupportedCultures = new[] { culture };
    opt.SupportedUICultures = new[] { culture };
});

var app = builder.Build();

// ====== Seed Data + Migration ======
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<SmsDbContext>();
        await DbInitializer.SeedAsync(db);
        Log.Information("✅ Database seeded successfully");

        // داده‌های نمونه فقط در محیط Development
        if (app.Environment.IsDevelopment())
        {
            var demoLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder");
            await DemoDataSeeder.SeedDemoAsync(db, demoLogger);
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "❌ Database seeding failed");
        throw;
    }
}

// ====== Middleware Pipeline ======
app.UseGlobalExceptionHandler(); // باید اول باشد

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseSerilogRequestLogging();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("🚀 SMS.Web starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

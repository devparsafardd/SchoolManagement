using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common;
using SMS.Application.Common.Export;
using SMS.Application.Common.Repositories;
using SMS.Application.Common.Sms;
using SMS.Application.Services;
using SMS.Infrastructure.Identity;
using SMS.Infrastructure.Persistence;
using SMS.Infrastructure.Persistence.Interceptors;
using SMS.Infrastructure.Persistence.Repositories;
using SMS.Infrastructure.Services;
using SMS.Infrastructure.Services.Export;
using SMS.Infrastructure.Services.Sms;

namespace SMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<SmsDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"), sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.CommandTimeout(60);
            });
            opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        var jwt = config.GetSection("JwtSettings").Get<JwtSettings>()!;
        services.AddSingleton(jwt);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Business Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISchoolService, SchoolService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IClassroomService, ClassroomService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IDisciplineService, DisciplineService>();
        services.AddScoped<IGuardianService, GuardianService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IReportCardService, ReportCardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<ISmsLogService, SmsLogService>();
        services.AddScoped<IPortalService, PortalService>();

        // Helpers
        services.AddScoped<IExcelExporter, ExcelExporter>();
        services.AddScoped<ISmsSender, FakeSmsSender>();

        return services;
    }
}

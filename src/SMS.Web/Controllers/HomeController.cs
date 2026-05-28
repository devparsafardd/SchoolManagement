using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Infrastructure.Persistence;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly SmsDbContext _db;
    public HomeController(SmsDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // ریدایرکت کاربران غیرادمین به پنل مخصوص خودشان
        if (!User.IsInRole(RoleNames.SuperAdmin) && !User.IsInRole(RoleNames.SchoolAdmin))
        {
            if (User.IsInRole(RoleNames.Parent))
                return RedirectToAction("Dashboard", "Parent");
            if (User.IsInRole(RoleNames.Student))
                return RedirectToAction("Index", "MyPortal");
            if (User.IsInRole(RoleNames.Teacher) || User.IsInRole(RoleNames.Counselor))
                return RedirectToAction("Dashboard", "Teacher");
            if (User.IsInRole(RoleNames.Principal) || User.IsInRole(RoleNames.VicePrincipal))
                return RedirectToAction("Index", "Principal");
        }

        // ===== آمار کلی =====
        ViewBag.SchoolCount = await _db.Schools.CountAsync(s => s.IsActive);
        ViewBag.StudentCount = await _db.Students.CountAsync(s => s.IsActive);
        ViewBag.ClassroomCount = await _db.Classrooms.CountAsync(c => c.IsActive);
        ViewBag.StaffCount = await _db.Staff.CountAsync(s => s.IsActive);
        ViewBag.GuardianCount = await _db.Guardians.CountAsync();
        ViewBag.SubjectCount = await _db.Subjects.CountAsync(s => s.IsActive);
        ViewBag.ExamCount = await _db.Exams.CountAsync();
        ViewBag.UserCount = await _db.Users.CountAsync(u => !u.IsLocked);

        // ===== امروز =====
        var today = DateTime.Today;
        ViewBag.TodayAttendanceCount = await _db.Attendances.CountAsync(a => a.AttendanceDate == today);
        ViewBag.TodayAbsentCount = await _db.Attendances
            .CountAsync(a => a.AttendanceDate == today && a.Status.IsAbsent);
        ViewBag.TodayTardyCount = await _db.Attendances
            .CountAsync(a => a.AttendanceDate == today && a.Status.IsTardy);

        // ===== انضباطی هفته اخیر =====
        var weekAgo = today.AddDays(-7);
        ViewBag.WeekRewardCount = await _db.DisciplinaryRecords
            .CountAsync(d => d.RecordDate >= weekAgo && d.Type.Category == "R");
        ViewBag.WeekPunishmentCount = await _db.DisciplinaryRecords
            .CountAsync(d => d.RecordDate >= weekAgo && d.Type.Category == "P");

        // ===== مالی کل =====
        var totalInvoiced = await _db.StudentInvoices.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalPaid = await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
        ViewBag.TotalInvoiced = totalInvoiced;
        ViewBag.TotalPaid = totalPaid;
        ViewBag.TotalDebt = totalInvoiced - totalPaid;
        ViewBag.CollectionRate = totalInvoiced > 0 ? Math.Round(totalPaid / totalInvoiced * 100, 1) : 0;

        // ===== نمودار حضور و غیاب ۷ روز اخیر =====
        var since = today.AddDays(-6);
        var attRaw = await _db.Attendances
            .Where(a => a.AttendanceDate >= since && a.AttendanceDate <= today)
            .GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new {
                Date = g.Key,
                Present = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy)
            }).ToListAsync();
        var labels = new List<string>();
        var presents = new List<int>();
        var absents = new List<int>();
        var tardies = new List<int>();
        for (var d = since; d <= today; d = d.AddDays(1))
        {
            var r = attRaw.FirstOrDefault(x => x.Date == d);
            labels.Add(d.ToString("MM/dd"));
            presents.Add(r?.Present ?? 0);
            absents.Add(r?.Absent ?? 0);
            tardies.Add(r?.Tardy ?? 0);
        }
        ViewBag.AttLabels = labels;
        ViewBag.AttPresent = presents;
        ViewBag.AttAbsent = absents;
        ViewBag.AttTardy = tardies;

        // ===== توزیع دانش‌آموز در مدارس =====
        ViewBag.SchoolDistribution = await _db.Schools
            .Where(s => s.IsActive)
            .Select(s => new {
                s.Name,
                Count = _db.Enrollments.Count(e => e.Classroom.SchoolId == s.SchoolId && e.Status == "فعال")
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync();

        // ===== توزیع نقش‌ها =====
        ViewBag.RoleDistribution = await _db.UserRoles
            .Where(ur => ur.IsActive)
            .GroupBy(ur => ur.Role.DisplayName)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // ===== فعالیت‌های اخیر =====
        ViewBag.RecentEnrollments = await _db.Enrollments
            .OrderByDescending(e => e.EnrollmentDate).Take(8)
            .Select(e => new {
                StudentName = e.Student.Person.FirstName + " " + e.Student.Person.LastName,
                ClassName = e.Classroom.Name,
                SchoolName = e.Classroom.School.Name,
                Date = e.EnrollmentDate
            }).ToListAsync();

        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}

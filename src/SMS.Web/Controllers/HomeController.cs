using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Infrastructure.Persistence;

namespace SMS.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly SmsDbContext _db;
    public HomeController(SmsDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.SchoolCount = await _db.Schools.CountAsync(s => s.IsActive);
        ViewBag.StudentCount = await _db.Students.CountAsync(s => s.IsActive);
        ViewBag.ClassroomCount = await _db.Classrooms.CountAsync(c => c.IsActive);
        ViewBag.StaffCount = await _db.Staff.CountAsync(s => s.IsActive);
        ViewBag.SubjectCount = await _db.Subjects.CountAsync(s => s.IsActive);
        ViewBag.ExamCount = await _db.Exams.CountAsync();

        // امروز
        var today = DateTime.Today;
        ViewBag.TodayAttendanceCount = await _db.Attendances.CountAsync(a => a.AttendanceDate == today);
        ViewBag.TodayAbsentCount = await _db.Attendances
            .Include(a => a.Status)
            .CountAsync(a => a.AttendanceDate == today && a.Status.IsAbsent);

        // انضباطی هفته اخیر
        var weekAgo = today.AddDays(-7);
        ViewBag.WeekRewardCount = await _db.DisciplinaryRecords
            .Include(d => d.Type)
            .CountAsync(d => d.RecordDate >= weekAgo && d.Type.Category == "R");
        ViewBag.WeekPunishmentCount = await _db.DisciplinaryRecords
            .Include(d => d.Type)
            .CountAsync(d => d.RecordDate >= weekAgo && d.Type.Category == "P");

        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}

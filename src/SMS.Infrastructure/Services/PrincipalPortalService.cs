using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

/// <summary>پیاده‌سازی سرویس پنل مدیر مدرسه با Row-Level Security روی SchoolId</summary>
public class PrincipalPortalService : IPrincipalPortalService
{
    private readonly SmsDbContext _db;

    public PrincipalPortalService(SmsDbContext db) => _db = db;

    public async Task<List<int>> GetManagedSchoolIdsAsync(int personId)
    {
        var staffId = await _db.Staff
            .Where(s => s.PersonId == personId && s.IsActive)
            .Select(s => (int?)s.StaffId).FirstOrDefaultAsync();

        if (staffId is null) return new List<int>();

        return await _db.StaffAssignments
            .Where(a => a.StaffId == staffId && a.IsActive
                && (a.Position == "Principal" || a.Position == "VicePrincipal" || a.Position == "Admin"))
            .Select(a => a.SchoolId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> CanAccessSchoolAsync(int personId, int schoolId)
    {
        var ids = await GetManagedSchoolIdsAsync(personId);
        return ids.Contains(schoolId);
    }

    public async Task<Result<PrincipalDashboardDto>> GetDashboardAsync(int schoolId, PrincipalFilter? filter = null)
    {
        var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.SchoolId == schoolId);
        if (school is null) return Result<PrincipalDashboardDto>.Fail("مدرسه یافت نشد");

        var academicYearId = filter?.AcademicYearId
            ?? await _db.AcademicYears.Where(a => a.IsActive).Select(a => (int?)a.AcademicYearId).FirstOrDefaultAsync();
        var ayTitle = await _db.AcademicYears.Where(a => a.AcademicYearId == academicYearId)
            .Select(a => a.Title).FirstOrDefaultAsync() ?? "—";

        // کلاس‌های مدرسه در سال جاری
        var classrooms = _db.Classrooms.Where(c => c.SchoolId == schoolId
            && (academicYearId == null || c.AcademicYearId == academicYearId));

        var totalClassrooms = await classrooms.CountAsync();

        // دانش‌آموزان فعال
        var studentsQuery = _db.Enrollments
            .Where(e => classrooms.Any(c => c.ClassroomId == e.ClassroomId)
                && e.Status == "فعال");

        var totalStudents = await studentsQuery.CountAsync();
        var male = await studentsQuery.CountAsync(e => e.Student.Person.Gender == "M");
        var female = await studentsQuery.CountAsync(e => e.Student.Person.Gender == "F");

        // معلمان و کارکنان
        var teachersInSchool = _db.StaffAssignments.Where(a => a.SchoolId == schoolId && a.IsActive);
        var totalTeachers = await teachersInSchool.Where(a => a.Position == "Teacher").Select(a => a.StaffId).Distinct().CountAsync();
        var totalStaff = await teachersInSchool.Select(a => a.StaffId).Distinct().CountAsync();

        // ولی‌های دانش‌آموزان مدرسه
        var totalGuardians = await _db.StudentGuardians
            .Where(sg => studentsQuery.Any(e => e.StudentId == sg.StudentId))
            .Select(sg => sg.GuardianId).Distinct().CountAsync();

        var totalSubjects = await _db.ClassSubjects
            .Where(cs => cs.Classroom.SchoolId == schoolId && cs.IsActive)
            .Select(cs => cs.GradeSubject.SubjectId)
            .Distinct().CountAsync();

        // حضور و غیاب امروز
        var today = DateTime.Today;
        var todayAttendances = _db.Attendances
            .Where(a => a.AttendanceDate.Date == today && a.Classroom.SchoolId == schoolId);

        var todayPresent = await todayAttendances.CountAsync(a => !a.Status.IsAbsent && !a.Status.IsTardy);
        var todayAbsent = await todayAttendances.CountAsync(a => a.Status.IsAbsent);
        var todayTardy = await todayAttendances.CountAsync(a => a.Status.IsTardy);
        var todayTotal = todayPresent + todayAbsent + todayTardy;
        var todayRate = todayTotal > 0 ? Math.Round((decimal)todayPresent / todayTotal * 100, 1) : 0;

        // مالی
        var invoices = _db.StudentInvoices.Where(i => i.SchoolId == schoolId
            && (academicYearId == null || i.AcademicYearId == academicYearId));
        var totalRevenue = await invoices.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalPaid = await _db.Payments
            .Where(p => invoices.Any(i => i.InvoiceId == p.InvoiceId))
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        var totalUnpaid = totalRevenue - totalPaid;
        var overdue = await invoices
            .CountAsync(i => i.DueDate != null && i.DueDate < today && i.Status != "پرداخت‌شده");

        // انضباطی این ماه
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var rewards = await _db.DisciplinaryRecords
            .CountAsync(d => d.Classroom.SchoolId == schoolId
                && d.Type.Category == "R" && d.RecordDate >= monthStart);
        var punishments = await _db.DisciplinaryRecords
            .CountAsync(d => d.Classroom.SchoolId == schoolId
                && d.Type.Category == "P" && d.RecordDate >= monthStart);

        var dto = new PrincipalDashboardDto
        {
            SchoolId = schoolId,
            SchoolName = school.Name,
            SchoolCode = school.Code,
            AcademicYearTitle = ayTitle,
            AcademicYearId = academicYearId,
            TotalStudents = totalStudents,
            TotalMaleStudents = male,
            TotalFemaleStudents = female,
            TotalClassrooms = totalClassrooms,
            TotalTeachers = totalTeachers,
            TotalStaff = totalStaff,
            TotalGuardians = totalGuardians,
            TotalSubjects = totalSubjects,
            TodayPresent = todayPresent,
            TodayAbsent = todayAbsent,
            TodayTardy = todayTardy,
            TodayPresentRate = todayRate,
            TotalRevenue = totalRevenue,
            TotalPaid = totalPaid,
            TotalUnpaid = totalUnpaid,
            OverdueInvoicesCount = overdue,
            RewardsThisMonth = rewards,
            PunishmentsThisMonth = punishments,
            AttendanceLast7Days = await GetAttendanceTrendAsync(schoolId, 7),
            StudentsByGrade = await GetStudentDistributionAsync(schoolId, academicYearId),
            TopClassrooms = (await GetClassroomsAsync(schoolId, academicYearId)).Take(10).ToList(),
            RecentActivities = await GetRecentActivitiesAsync(schoolId, 10)
        };

        return Result<PrincipalDashboardDto>.Ok(dto);
    }

    public async Task<List<PrincipalClassroomBrief>> GetClassroomsAsync(int schoolId, int? academicYearId = null)
    {
        academicYearId ??= await _db.AcademicYears.Where(a => a.IsActive)
            .Select(a => (int?)a.AcademicYearId).FirstOrDefaultAsync();

        var q = _db.Classrooms
            .Include(c => c.Grade)
            .Include(c => c.HeadTeacher).ThenInclude(h => h!.Person)
            .Where(c => c.SchoolId == schoolId);

        if (academicYearId.HasValue)
            q = q.Where(c => c.AcademicYearId == academicYearId);

        return await q.Select(c => new PrincipalClassroomBrief
        {
            ClassroomId = c.ClassroomId,
            ClassroomName = c.Name,
            GradeName = c.Grade.Name,
            StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.ClassroomId && e.Status == "فعال"),
            HeadTeacherName = c.HeadTeacher != null ? (c.HeadTeacher.Person.FirstName + " " + c.HeadTeacher.Person.LastName) : null,
            AverageGPA = _db.StudentTermGPAs
                .Where(g => g.ClassroomId == c.ClassroomId)
                .Average(g => (decimal?)g.GPA)
        })
        .OrderBy(c => c.GradeName).ThenBy(c => c.ClassroomName)
        .ToListAsync();
    }

    public async Task<List<PrincipalChartPoint>> GetAttendanceTrendAsync(int schoolId, int days = 7)
    {
        var since = DateTime.Today.AddDays(-(days - 1));
        var raw = await _db.Attendances
            .Where(a => a.Classroom.SchoolId == schoolId && a.AttendanceDate >= since)
            .GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Present = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy)
            }).ToListAsync();

        var points = new List<PrincipalChartPoint>();
        for (var d = 0; d < days; d++)
        {
            var date = since.AddDays(d);
            var r = raw.FirstOrDefault(x => x.Date == date);
            points.Add(new PrincipalChartPoint
            {
                Label = date.ToString("MM/dd"),
                Present = r?.Present ?? 0,
                Absent = r?.Absent ?? 0,
                Tardy = r?.Tardy ?? 0
            });
        }
        return points;
    }

    public async Task<List<PrincipalGradeStudentCount>> GetStudentDistributionAsync(int schoolId, int? academicYearId = null)
    {
        academicYearId ??= await _db.AcademicYears.Where(a => a.IsActive)
            .Select(a => (int?)a.AcademicYearId).FirstOrDefaultAsync();

        return await _db.Classrooms
            .Where(c => c.SchoolId == schoolId
                && (academicYearId == null || c.AcademicYearId == academicYearId))
            .GroupBy(c => new { c.GradeId, c.Grade.Name, c.Grade.OrderNo })
            .OrderBy(g => g.Key.OrderNo)
            .Select(g => new PrincipalGradeStudentCount
            {
                GradeId = g.Key.GradeId,
                GradeName = g.Key.Name,
                ClassroomCount = g.Count(),
                StudentCount = _db.Enrollments.Count(e =>
                    g.Select(x => x.ClassroomId).Contains(e.ClassroomId) && e.Status == "فعال")
            }).ToListAsync();
    }

    public async Task<List<PrincipalRecentActivity>> GetRecentActivitiesAsync(int schoolId, int limit = 15)
    {
        var list = new List<PrincipalRecentActivity>();

        // ثبت‌نام‌های جدید
        var newEnrolls = await _db.Enrollments
            .Where(e => e.Classroom.SchoolId == schoolId)
            .OrderByDescending(e => e.EnrollmentDate)
            .Take(limit)
            .Select(e => new PrincipalRecentActivity
            {
                Type = "enrollment",
                Description = $"ثبت‌نام دانش‌آموز {e.Student.Person.FirstName} {e.Student.Person.LastName} در کلاس {e.Classroom.Name}",
                At = e.EnrollmentDate,
                Link = $"/Students/Details/{e.StudentId}"
            }).ToListAsync();
        list.AddRange(newEnrolls);

        // پرداخت‌های اخیر
        var payments = await _db.Payments
            .Where(p => p.Invoice.SchoolId == schoolId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new PrincipalRecentActivity
            {
                Type = "payment",
                Description = $"پرداخت {p.Amount:N0} تومان - فاکتور {p.Invoice.InvoiceNumber}",
                At = p.CreatedAt,
                Link = $"/Finance/Details/{p.InvoiceId}"
            }).ToListAsync();
        list.AddRange(payments);

        // موارد انضباطی
        var disciplines = await _db.DisciplinaryRecords
            .Where(d => d.Classroom.SchoolId == schoolId)
            .OrderByDescending(d => d.RecordDate)
            .Take(limit)
            .Select(d => new PrincipalRecentActivity
            {
                Type = "discipline",
                Description = $"{(d.Type.Category == "R" ? "تشویق" : "تذکر")}: {d.Student.Person.FirstName} {d.Student.Person.LastName} - {d.Type.Name}",
                At = d.RecordDate,
                Link = "/Discipline"
            }).ToListAsync();
        list.AddRange(disciplines);

        return list.OrderByDescending(x => x.At).Take(limit).ToList();
    }
}

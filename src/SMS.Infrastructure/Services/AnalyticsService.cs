using Microsoft.EntityFrameworkCore;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

/// <summary>پیاده‌سازی سرویس گزارش‌گیری جامع با Query های بهینه</summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly SmsDbContext _db;

    public AnalyticsService(SmsDbContext db) => _db = db;

    public async Task<AttendanceAnalyticsDto> GetAttendanceAnalyticsAsync(int? schoolId, DateTime fromDate, DateTime toDate)
    {
        var q = _db.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate >= fromDate && a.AttendanceDate <= toDate);
        if (schoolId.HasValue) q = q.Where(a => a.Classroom.SchoolId == schoolId);

        var total = await q.CountAsync();
        var present = await q.CountAsync(x => !x.Status.IsAbsent && !x.Status.IsTardy);
        var absent = await q.CountAsync(x => x.Status.IsAbsent);
        var tardy = await q.CountAsync(x => x.Status.IsTardy);
        var leave = await q.CountAsync(x => x.Status.Code == "LEAVE" || x.Status.Code == "EXCUSED");

        // Daily
        var dailyRaw = await q.GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Present = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy)
            }).ToListAsync();

        var daily = new List<DailyAttendancePoint>();
        for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
        {
            var r = dailyRaw.FirstOrDefault(x => x.Date == d);
            daily.Add(new DailyAttendancePoint
            {
                Date = d, Label = d.ToString("MM/dd"),
                Present = r?.Present ?? 0, Absent = r?.Absent ?? 0, Tardy = r?.Tardy ?? 0
            });
        }

        // By Classroom
        var byClass = await q.GroupBy(a => a.ClassroomId)
            .Select(g => new
            {
                ClassroomId = g.Key,
                Present = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy),
                Total = g.Count()
            })
            .Join(_db.Classrooms.Include(c => c.Grade), x => x.ClassroomId, c => c.ClassroomId,
                (x, c) => new ClassAttendanceRow
                {
                    ClassroomId = c.ClassroomId,
                    ClassroomName = c.Name,
                    GradeName = c.Grade.Name,
                    TotalStudents = _db.Enrollments.Count(e => e.ClassroomId == c.ClassroomId && e.Status == "فعال"),
                    PresentCount = x.Present,
                    AbsentCount = x.Absent,
                    TardyCount = x.Tardy,
                    PresentRate = x.Total > 0 ? Math.Round((decimal)x.Present / x.Total * 100, 1) : 0
                })
            .OrderByDescending(c => c.AbsentCount)
            .ToListAsync();

        // Top Absentees
        var topAbsent = await q.GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy),
                Unexcused = g.Count(x => x.Status.Code == "ABSENT")
            })
            .OrderByDescending(x => x.Absent)
            .Take(10)
            .Join(_db.Students.Include(s => s.Person).Include(s => s.Enrollments).ThenInclude(e => e.Classroom),
                x => x.StudentId, s => s.StudentId,
                (x, s) => new StudentAttendanceProblemRow
                {
                    StudentId = s.StudentId,
                    StudentCode = s.StudentCode,
                    FullName = s.Person.FirstName + " " + s.Person.LastName,
                    ClassroomName = s.Enrollments.Where(e => e.Status == "فعال")
                        .Select(e => e.Classroom.Name).FirstOrDefault() ?? "—",
                    AbsentCount = x.Absent,
                    TardyCount = x.Tardy,
                    UnexcusedCount = x.Unexcused
                })
            .ToListAsync();

        var schoolName = schoolId.HasValue
            ? await _db.Schools.Where(s => s.SchoolId == schoolId).Select(s => s.Name).FirstOrDefaultAsync()
            : null;

        return new AttendanceAnalyticsDto
        {
            SchoolId = schoolId,
            SchoolName = schoolName,
            FromDate = fromDate, ToDate = toDate,
            TotalRecords = total,
            PresentCount = present, AbsentCount = absent, TardyCount = tardy, LeaveCount = leave,
            PresentRate = total > 0 ? Math.Round((decimal)present / total * 100, 1) : 0,
            AbsentRate = total > 0 ? Math.Round((decimal)absent / total * 100, 1) : 0,
            Daily = daily, ByClassroom = byClass, TopAbsentees = topAbsent
        };
    }

    public async Task<AcademicAnalyticsDto> GetAcademicAnalyticsAsync(int? schoolId, int? termId)
    {
        termId ??= await _db.Terms.OrderByDescending(t => t.AcademicYearId).ThenByDescending(t => t.OrderNo)
            .Select(t => (int?)t.TermId).FirstOrDefaultAsync();

        var gpaQ = _db.StudentTermGPAs.AsNoTracking()
            .Where(g => g.TermId == termId);
        if (schoolId.HasValue) gpaQ = gpaQ.Where(g => g.Classroom.SchoolId == schoolId);

        var totalGpa = await gpaQ.Where(g => g.GPA != null).AverageAsync(g => (decimal?)g.GPA);
        var passed = await gpaQ.CountAsync(g => g.GPA >= 10);
        var failed = await gpaQ.CountAsync(g => g.GPA < 10);
        var totalCount = passed + failed;

        // میانگین هر درس
        var subjectAvg = await _db.ExamScores
            .Where(s => s.NumericScore != null && s.Exam.TermId == termId
                && (!schoolId.HasValue || s.Exam.ClassSubject.Classroom.SchoolId == schoolId))
            .GroupBy(s => new { s.Exam.ClassSubject.GradeSubject.SubjectId, s.Exam.ClassSubject.GradeSubject.Subject.Name })
            .Select(g => new SubjectAveragePoint
            {
                SubjectId = g.Key.SubjectId,
                SubjectName = g.Key.Name,
                AverageScore = (decimal)g.Average(x => x.NumericScore)!,
                TotalScores = g.Count(),
                PassedCount = g.Count(x => x.NumericScore >= 10)
            })
            .OrderByDescending(s => s.AverageScore)
            .Take(15)
            .ToListAsync();

        // میانگین هر پایه
        var gradeAvg = await gpaQ.Include(g => g.Classroom).ThenInclude(c => c.Grade)
            .GroupBy(g => new { g.Classroom.GradeId, g.Classroom.Grade.Name })
            .Select(g => new GradeAveragePoint
            {
                GradeId = g.Key.GradeId,
                GradeName = g.Key.Name,
                AverageGPA = (decimal)g.Average(x => (decimal?)x.GPA)!,
                StudentCount = g.Count()
            })
            .ToListAsync();

        // بهترین و ضعیف‌ترین دانش‌آموزان
        var top = await gpaQ.Where(g => g.GPA != null)
            .Include(g => g.Classroom).ThenInclude(c => c.Grade)
            .Join(_db.Students.Include(s => s.Person),
                g => g.StudentId, s => s.StudentId,
                (g, s) => new TopStudentRow
                {
                    StudentId = s.StudentId,
                    FullName = s.Person.FirstName + " " + s.Person.LastName,
                    ClassroomName = g.Classroom.Name,
                    GradeName = g.Classroom.Grade.Name,
                    GPA = g.GPA!.Value,
                    Rank = g.RankInClass
                })
            .OrderByDescending(x => x.GPA)
            .Take(10).ToListAsync();

        var weak = await gpaQ.Where(g => g.GPA != null)
            .Include(g => g.Classroom).ThenInclude(c => c.Grade)
            .Join(_db.Students.Include(s => s.Person),
                g => g.StudentId, s => s.StudentId,
                (g, s) => new TopStudentRow
                {
                    StudentId = s.StudentId,
                    FullName = s.Person.FirstName + " " + s.Person.LastName,
                    ClassroomName = g.Classroom.Name,
                    GradeName = g.Classroom.Grade.Name,
                    GPA = g.GPA!.Value,
                    Rank = g.RankInClass
                })
            .OrderBy(x => x.GPA)
            .Take(10).ToListAsync();

        // توزیع نمرات
        var scoreBuckets = new[] {
            new { Min = 0m, Max = 5m, Label = "0-5" },
            new { Min = 5m, Max = 10m, Label = "5-10" },
            new { Min = 10m, Max = 14m, Label = "10-14" },
            new { Min = 14m, Max = 17m, Label = "14-17" },
            new { Min = 17m, Max = 20.01m, Label = "17-20" },
        };
        var dist = new List<ScoreDistributionBucket>();
        foreach (var b in scoreBuckets)
        {
            var cnt = await _db.ExamScores.CountAsync(s => s.NumericScore != null && s.NumericScore >= b.Min && s.NumericScore < b.Max
                && s.Exam.TermId == termId
                && (!schoolId.HasValue || s.Exam.ClassSubject.Classroom.SchoolId == schoolId));
            dist.Add(new ScoreDistributionBucket { Range = b.Label, Count = cnt });
        }

        var schoolName = schoolId.HasValue
            ? await _db.Schools.Where(s => s.SchoolId == schoolId).Select(s => s.Name).FirstOrDefaultAsync()
            : null;
        var termName = await _db.Terms.Where(t => t.TermId == termId).Select(t => t.Name).FirstOrDefaultAsync();

        return new AcademicAnalyticsDto
        {
            SchoolId = schoolId, SchoolName = schoolName, TermId = termId, TermName = termName,
            OverallGPA = totalGpa,
            PassedStudents = passed, FailedStudents = failed,
            PassRate = totalCount > 0 ? Math.Round((decimal)passed / totalCount * 100, 1) : 0,
            SubjectAverages = subjectAvg, GradeAverages = gradeAvg,
            TopStudents = top, WeakStudents = weak, ScoreDistribution = dist
        };
    }

    public async Task<FinancialAnalyticsDto> GetFinancialAnalyticsAsync(int? schoolId, int? academicYearId, DateTime fromDate, DateTime toDate)
    {
        var invQ = _db.StudentInvoices.AsNoTracking()
            .Where(i => i.CreatedAt >= fromDate && i.CreatedAt <= toDate);
        if (schoolId.HasValue) invQ = invQ.Where(i => i.SchoolId == schoolId);
        if (academicYearId.HasValue) invQ = invQ.Where(i => i.AcademicYearId == academicYearId);

        var totalInvoiced = await invQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var invoiceCount = await invQ.CountAsync();

        var payQ = _db.Payments.AsNoTracking().Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate);
        if (schoolId.HasValue) payQ = payQ.Where(p => p.Invoice.SchoolId == schoolId);

        var totalPaid = await payQ.SumAsync(p => (decimal?)p.Amount) ?? 0;
        var outstanding = totalInvoiced - totalPaid;
        var collectionRate = totalInvoiced > 0 ? Math.Round(totalPaid / totalInvoiced * 100, 1) : 0;

        var today = DateTime.Today;
        var overdueQ = invQ.Where(i => i.DueDate != null && i.DueDate < today && i.Status != "پرداخت‌شده");
        var overdueCount = await overdueQ.CountAsync();
        var overdueAmount = await overdueQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        // Monthly Collection
        var monthly = await payQ
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Collected = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        var monthlyInv = await invQ
            .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Invoiced = g.Sum(x => x.NetAmount) })
            .ToListAsync();

        var monthlyPoints = new List<MonthlyFinancePoint>();
        for (var d = new DateTime(fromDate.Year, fromDate.Month, 1); d <= toDate; d = d.AddMonths(1))
        {
            var inv = monthlyInv.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Invoiced ?? 0;
            var col = monthly.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Collected ?? 0;
            monthlyPoints.Add(new MonthlyFinancePoint
            {
                MonthLabel = $"{d.Year}/{d.Month:00}",
                Invoiced = inv, Collected = col
            });
        }

        var byFeeType = await invQ
            .GroupBy(i => new { i.FeeTypeId, i.FeeType.Name })
            .Select(g => new FeeTypeBreakdown
            {
                FeeTypeName = g.Key.Name,
                Amount = g.Sum(x => x.NetAmount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();

        var byMethod = await payQ
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new PaymentMethodBreakdown
            {
                Method = g.Key,
                Amount = g.Sum(x => x.Amount),
                Count = g.Count()
            })
            .ToListAsync();

        // بدهکاران
        var debtors = await invQ
            .Where(i => i.Status != "پرداخت‌شده")
            .GroupBy(i => i.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                TotalInv = g.Sum(x => x.NetAmount),
                Overdue = g.Count(x => x.DueDate != null && x.DueDate < today)
            })
            .OrderByDescending(x => x.TotalInv)
            .Take(10)
            .Join(_db.Students.Include(s => s.Person).Include(s => s.Enrollments).ThenInclude(e => e.Classroom),
                x => x.StudentId, s => s.StudentId,
                (x, s) => new DebtorRow
                {
                    StudentId = s.StudentId,
                    StudentName = s.Person.FirstName + " " + s.Person.LastName,
                    ClassroomName = s.Enrollments.Where(e => e.Status == "فعال")
                        .Select(e => e.Classroom.Name).FirstOrDefault() ?? "—",
                    TotalDebt = x.TotalInv - _db.Payments.Where(p => _db.StudentInvoices
                            .Where(i => i.StudentId == s.StudentId && i.Status != "پرداخت‌شده")
                            .Select(i => i.InvoiceId).Contains(p.InvoiceId))
                        .Sum(p => (decimal?)p.Amount) ?? 0,
                    OverdueInvoiceCount = x.Overdue
                }).ToListAsync();

        var schoolName = schoolId.HasValue
            ? await _db.Schools.Where(s => s.SchoolId == schoolId).Select(s => s.Name).FirstOrDefaultAsync()
            : null;

        return new FinancialAnalyticsDto
        {
            SchoolId = schoolId, SchoolName = schoolName,
            AcademicYearId = academicYearId,
            FromDate = fromDate, ToDate = toDate,
            TotalInvoiced = totalInvoiced, TotalPaid = totalPaid,
            TotalOutstanding = outstanding, CollectionRate = collectionRate,
            InvoiceCount = invoiceCount,
            OverdueInvoiceCount = overdueCount, OverdueAmount = overdueAmount,
            MonthlyCollection = monthlyPoints,
            ByFeeType = byFeeType, ByPaymentMethod = byMethod,
            TopDebtors = debtors
        };
    }

    public async Task<DisciplineAnalyticsDto> GetDisciplineAnalyticsAsync(int? schoolId, DateTime fromDate, DateTime toDate)
    {
        var q = _db.DisciplinaryRecords.AsNoTracking()
            .Where(d => d.RecordDate >= fromDate && d.RecordDate <= toDate);
        if (schoolId.HasValue) q = q.Where(d => d.Classroom.SchoolId == schoolId);

        var rewards = await q.CountAsync(d => d.Type.Category == "R");
        var punishments = await q.CountAsync(d => d.Type.Category == "P");

        var byType = await q.GroupBy(d => new { d.Type.Name, d.Type.Category })
            .Select(g => new DisciplineTypeBreakdown
            {
                TypeName = g.Key.Name,
                Category = g.Key.Category,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var monthlyRaw = await q.GroupBy(d => new { d.RecordDate.Year, d.RecordDate.Month, d.Type.Category })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Category, Count = g.Count() })
            .ToListAsync();
        var monthly = new List<MonthlyDisciplinePoint>();
        for (var d = new DateTime(fromDate.Year, fromDate.Month, 1); d <= toDate; d = d.AddMonths(1))
        {
            monthly.Add(new MonthlyDisciplinePoint
            {
                MonthLabel = $"{d.Year}/{d.Month:00}",
                Rewards = monthlyRaw.Where(x => x.Year == d.Year && x.Month == d.Month && x.Category == "R").Sum(x => x.Count),
                Punishments = monthlyRaw.Where(x => x.Year == d.Year && x.Month == d.Month && x.Category == "P").Sum(x => x.Count)
            });
        }

        var topRewarded = await q.Where(d => d.Type.Category == "R")
            .GroupBy(d => d.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .Join(_db.Students.Include(s => s.Person).Include(s => s.Enrollments).ThenInclude(e => e.Classroom),
                x => x.StudentId, s => s.StudentId,
                (x, s) => new StudentDisciplineRow
                {
                    StudentId = s.StudentId,
                    FullName = s.Person.FirstName + " " + s.Person.LastName,
                    ClassroomName = s.Enrollments.Where(e => e.Status == "فعال").Select(e => e.Classroom.Name).FirstOrDefault() ?? "—",
                    RecordCount = x.Count
                }).ToListAsync();

        var topPunished = await q.Where(d => d.Type.Category == "P")
            .GroupBy(d => d.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .Join(_db.Students.Include(s => s.Person).Include(s => s.Enrollments).ThenInclude(e => e.Classroom),
                x => x.StudentId, s => s.StudentId,
                (x, s) => new StudentDisciplineRow
                {
                    StudentId = s.StudentId,
                    FullName = s.Person.FirstName + " " + s.Person.LastName,
                    ClassroomName = s.Enrollments.Where(e => e.Status == "فعال").Select(e => e.Classroom.Name).FirstOrDefault() ?? "—",
                    RecordCount = x.Count
                }).ToListAsync();

        var schoolName = schoolId.HasValue
            ? await _db.Schools.Where(s => s.SchoolId == schoolId).Select(s => s.Name).FirstOrDefaultAsync()
            : null;

        return new DisciplineAnalyticsDto
        {
            SchoolId = schoolId, SchoolName = schoolName,
            FromDate = fromDate, ToDate = toDate,
            TotalRewards = rewards, TotalPunishments = punishments,
            ByType = byType, Monthly = monthly,
            TopRewarded = topRewarded, TopPunished = topPunished
        };
    }
    // ====== گزارش جامع یک کلاس ======
    public async Task<Result<ClassroomAnalyticsDto>> GetClassroomAnalyticsAsync(int classroomId, int? termId = null)
    {
        var cls = await _db.Classrooms.AsNoTracking()
            .Include(c => c.School).Include(c => c.Grade)
            .FirstOrDefaultAsync(c => c.ClassroomId == classroomId);
        if (cls is null) return Result<ClassroomAnalyticsDto>.Fail("کلاس یافت نشد");

        var students = await _db.Enrollments.AsNoTracking()
            .Where(e => e.ClassroomId == classroomId && e.Status == "فعال")
            .Select(e => new { e.StudentId, e.Student.Person.Gender })
            .ToListAsync();
        var studentIds = students.Select(s => s.StudentId).ToList();

        var since = DateTime.Today.AddDays(-30);
        var attRaw = await _db.Attendances.AsNoTracking()
            .Where(a => a.ClassroomId == classroomId && a.AttendanceDate >= since)
            .GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new {
                Date = g.Key,
                Present = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy)
            }).ToListAsync();
        var trend = new List<DailyAttendancePoint>();
        for (var d = since; d <= DateTime.Today; d = d.AddDays(1))
        {
            var r = attRaw.FirstOrDefault(x => x.Date == d);
            trend.Add(new DailyAttendancePoint { Date = d, Label = d.ToString("MM/dd"),
                Present = r?.Present ?? 0, Absent = r?.Absent ?? 0, Tardy = r?.Tardy ?? 0 });
        }

        // میانگین هر درس کلاس
        var subjectAvg = await _db.ExamScores.AsNoTracking()
            .Where(s => studentIds.Contains(s.StudentId)
                && s.Exam.ClassSubject.ClassroomId == classroomId
                && s.NumericScore != null
                && (termId == null || s.Exam.TermId == termId))
            .GroupBy(s => new { s.Exam.ClassSubject.GradeSubject.SubjectId, s.Exam.ClassSubject.GradeSubject.Subject.Name })
            .Select(g => new SubjectAveragePoint {
                SubjectId = g.Key.SubjectId, SubjectName = g.Key.Name,
                AverageScore = (decimal)g.Average(x => x.NumericScore)!,
                TotalScores = g.Count(),
                PassedCount = g.Count(x => x.NumericScore >= 10)
            }).OrderByDescending(s => s.AverageScore).ToListAsync();

        // رتبه‌بندی دانش‌آموزان کلاس
        var ranking = new List<StudentRankRow>();
        foreach (var sid in studentIds)
        {
            var avg = await _db.ExamScores.AsNoTracking()
                .Where(s => s.StudentId == sid && s.Exam.ClassSubject.ClassroomId == classroomId
                    && s.NumericScore != null
                    && (termId == null || s.Exam.TermId == termId))
                .AverageAsync(s => (decimal?)s.NumericScore);

            var info = await _db.Students.AsNoTracking()
                .Include(s => s.Person).Where(s => s.StudentId == sid)
                .Select(s => new { s.StudentCode, FullName = s.Person.FirstName + " " + s.Person.LastName })
                .FirstAsync();
            var absCnt = await _db.Attendances.CountAsync(a => a.StudentId == sid && a.ClassroomId == classroomId && a.Status.IsAbsent);
            var tardyCnt = await _db.Attendances.CountAsync(a => a.StudentId == sid && a.ClassroomId == classroomId && a.Status.IsTardy);
            var rewards = await _db.DisciplinaryRecords.CountAsync(d => d.StudentId == sid && d.Type.Category == "R");
            var puns = await _db.DisciplinaryRecords.CountAsync(d => d.StudentId == sid && d.Type.Category == "P");

            ranking.Add(new StudentRankRow {
                StudentId = sid, StudentCode = info.StudentCode, FullName = info.FullName,
                Average = avg, AbsenceCount = absCnt, TardyCount = tardyCnt,
                RewardsCount = rewards, PunishmentsCount = puns
            });
        }
        var ranked = ranking.OrderByDescending(r => r.Average ?? 0).ToList();
        for (int i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;

        var classAvg = ranked.Where(r => r.Average.HasValue).Select(r => r.Average!.Value).DefaultIfEmpty(0).Average();

        return Result<ClassroomAnalyticsDto>.Ok(new ClassroomAnalyticsDto {
            ClassroomId = classroomId,
            ClassroomName = cls.Name, GradeName = cls.Grade.Name, SchoolName = cls.School.Name,
            TotalStudents = students.Count,
            MaleStudents = students.Count(s => s.Gender == "M"),
            FemaleStudents = students.Count(s => s.Gender == "F"),
            ClassAverage = classAvg > 0 ? classAvg : null,
            TotalAbsences = ranked.Sum(r => r.AbsenceCount),
            TotalTardies = ranked.Sum(r => r.TardyCount),
            TotalRewards = ranked.Sum(r => r.RewardsCount),
            TotalPunishments = ranked.Sum(r => r.PunishmentsCount),
            SubjectAverages = subjectAvg,
            StudentRanking = ranked,
            AttendanceTrend = trend
        });
    }

    // ====== گزارش معلم ======
    public async Task<Result<TeacherAnalyticsDto>> GetTeacherAnalyticsAsync(int staffId, int? termId = null)
    {
        var staff = await _db.Staff.AsNoTracking().Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.StaffId == staffId);
        if (staff is null) return Result<TeacherAnalyticsDto>.Fail("معلم یافت نشد");

        var classSubjects = await _db.ClassSubjects.AsNoTracking()
            .Include(cs => cs.Classroom).Include(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Where(cs => cs.StaffId == staffId && cs.IsActive).ToListAsync();

        var classPerf = new List<TeacherClassPerformance>();
        foreach (var cs in classSubjects)
        {
            var sCount = await _db.Enrollments.CountAsync(e => e.ClassroomId == cs.ClassroomId && e.Status == "فعال");
            var exams = await _db.Exams.AsNoTracking()
                .Where(e => e.ClassSubjectId == cs.ClassSubjectId
                    && (termId == null || e.TermId == termId)).ToListAsync();
            var examIds = exams.Select(e => e.ExamId).ToList();
            var avg = examIds.Any()
                ? await _db.ExamScores.Where(s => examIds.Contains(s.ExamId) && s.NumericScore != null).AverageAsync(s => (decimal?)s.NumericScore)
                : null;
            var totalScores = examIds.Any()
                ? await _db.ExamScores.CountAsync(s => examIds.Contains(s.ExamId) && s.NumericScore != null)
                : 0;
            var passedScores = examIds.Any()
                ? await _db.ExamScores.CountAsync(s => examIds.Contains(s.ExamId) && s.NumericScore >= 10)
                : 0;
            classPerf.Add(new TeacherClassPerformance {
                ClassSubjectId = cs.ClassSubjectId,
                ClassroomName = cs.Classroom.Name,
                SubjectName = cs.GradeSubject.Subject.Name,
                StudentCount = sCount, Average = avg,
                ExamCount = exams.Count,
                PassRate = totalScores > 0 ? (int)Math.Round((double)passedScores / totalScores * 100) : 0
            });
        }

        var allClassSubjectIds = classSubjects.Select(cs => cs.ClassSubjectId).ToList();
        var totalExams = await _db.Exams.CountAsync(e => allClassSubjectIds.Contains(e.ClassSubjectId)
            && (termId == null || e.TermId == termId));
        var scoredExams = await _db.Exams.CountAsync(e => allClassSubjectIds.Contains(e.ClassSubjectId)
            && _db.ExamScores.Any(s => s.ExamId == e.ExamId));
        var allClassrooms = classSubjects.Select(cs => cs.ClassroomId).Distinct().ToList();
        var totalStudents = await _db.Enrollments.CountAsync(e => allClassrooms.Contains(e.ClassroomId) && e.Status == "فعال");
        var attRecs = await _db.Attendances.CountAsync(a => a.RecordedByStaffId == staffId);
        var hwAssigned = await _db.Homeworks.CountAsync(h => h.CreatedByStaffId == staffId);
        var hwGraded = await _db.HomeworkSubmissions.CountAsync(s => s.GradedByStaffId == staffId);
        var overallAvg = await _db.ExamScores.Where(s => _db.Exams.Where(e => allClassSubjectIds.Contains(e.ClassSubjectId)).Select(e => e.ExamId).Contains(s.ExamId) && s.NumericScore != null).AverageAsync(s => (decimal?)s.NumericScore);

        return Result<TeacherAnalyticsDto>.Ok(new TeacherAnalyticsDto {
            StaffId = staffId, PersonnelCode = staff.PersonnelCode,
            TeacherName = staff.Person.FirstName + " " + staff.Person.LastName,
            TotalClasses = classSubjects.Count,
            TotalStudents = totalStudents,
            TotalExams = totalExams,
            TotalScoredExams = scoredExams,
            OverallAverage = overallAvg,
            AttendanceRecordsTaken = attRecs,
            HomeworksAssigned = hwAssigned,
            HomeworksGraded = hwGraded,
            Classes = classPerf
        });
    }

    // ====== گزارش دانش‌آموز ======
    public async Task<Result<StudentAnalyticsDto>> GetStudentAnalyticsAsync(int studentId, int? termId = null)
    {
        var s = await _db.Students.AsNoTracking().Include(x => x.Person)
            .FirstOrDefaultAsync(x => x.StudentId == studentId);
        if (s is null) return Result<StudentAnalyticsDto>.Fail("دانش‌آموز یافت نشد");

        var enroll = await _db.Enrollments.AsNoTracking()
            .Include(e => e.Classroom).ThenInclude(c => c.School)
            .Where(e => e.StudentId == studentId && e.Status == "فعال")
            .FirstOrDefaultAsync();

        var avg = await _db.ExamScores.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.NumericScore != null
                && (termId == null || x.Exam.TermId == termId))
            .AverageAsync(x => (decimal?)x.NumericScore);

        var absCnt = await _db.Attendances.CountAsync(a => a.StudentId == studentId && a.Status.IsAbsent);
        var tardyCnt = await _db.Attendances.CountAsync(a => a.StudentId == studentId && a.Status.IsTardy);
        var rewards = await _db.DisciplinaryRecords.CountAsync(d => d.StudentId == studentId && d.Type.Category == "R");
        var puns = await _db.DisciplinaryRecords.CountAsync(d => d.StudentId == studentId && d.Type.Category == "P");

        var totalInv = await _db.StudentInvoices.Where(i => i.StudentId == studentId).SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalPaid = await _db.Payments.Where(p => p.Invoice.StudentId == studentId).SumAsync(p => (decimal?)p.Amount) ?? 0;

        var subjectScores = await _db.ExamScores.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.NumericScore != null
                && (termId == null || x.Exam.TermId == termId))
            .GroupBy(x => new { x.Exam.ClassSubject.GradeSubject.SubjectId, x.Exam.ClassSubject.GradeSubject.Subject.Name })
            .Select(g => new SubjectAveragePoint {
                SubjectId = g.Key.SubjectId, SubjectName = g.Key.Name,
                AverageScore = (decimal)g.Average(x => x.NumericScore)!,
                TotalScores = g.Count(), PassedCount = g.Count(x => x.NumericScore >= 10)
            }).ToListAsync();

        var since = DateTime.Today.AddDays(-30);
        var attRaw = await _db.Attendances.AsNoTracking()
            .Where(a => a.StudentId == studentId && a.AttendanceDate >= since)
            .GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new {
                Date = g.Key,
                Present = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                Absent = g.Count(x => x.Status.IsAbsent),
                Tardy = g.Count(x => x.Status.IsTardy)
            }).ToListAsync();
        var trend = new List<DailyAttendancePoint>();
        for (var d = since; d <= DateTime.Today; d = d.AddDays(1))
        {
            var r = attRaw.FirstOrDefault(x => x.Date == d);
            trend.Add(new DailyAttendancePoint { Date = d, Label = d.ToString("MM/dd"),
                Present = r?.Present ?? 0, Absent = r?.Absent ?? 0, Tardy = r?.Tardy ?? 0 });
        }

        var recentExams = await _db.ExamScores.AsNoTracking()
            .Include(x => x.Exam).ThenInclude(e => e.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(x => x.DescriptiveScaleItem)
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.Exam.ExamDate)
            .Take(15)
            .Select(x => new ExamScoreItem {
                Title = x.Exam.Title,
                SubjectName = x.Exam.ClassSubject.GradeSubject.Subject.Name,
                Date = x.Exam.ExamDate,
                Score = x.NumericScore,
                MaxScore = x.Exam.MaxScore,
                DescriptiveLabel = x.DescriptiveScaleItem != null ? x.DescriptiveScaleItem.Label : null
            }).ToListAsync();

        return Result<StudentAnalyticsDto>.Ok(new StudentAnalyticsDto {
            StudentId = studentId, StudentCode = s.StudentCode,
            FullName = s.Person.FirstName + " " + s.Person.LastName,
            FatherName = s.Person.FatherName,
            ClassroomName = enroll?.Classroom.Name ?? "—",
            SchoolName = enroll?.Classroom.School.Name ?? "—",
            OverallAverage = avg,
            TotalAbsences = absCnt, TotalTardies = tardyCnt,
            RewardsCount = rewards, PunishmentsCount = puns,
            TotalDebt = totalInv - totalPaid, TotalPaid = totalPaid,
            SubjectScores = subjectScores,
            AttendanceLast30 = trend,
            RecentExams = recentExams
        });
    }
}


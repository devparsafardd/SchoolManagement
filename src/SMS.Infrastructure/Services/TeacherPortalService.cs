using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

/// <summary>
/// پیاده‌سازی سرویس پنل معلم
/// تمام Query ها بر اساس StaffId محدود می‌شوند
/// </summary>
public class TeacherPortalService : ITeacherPortalService
{
    private readonly SmsDbContext _db;

    public TeacherPortalService(SmsDbContext db) => _db = db;

    public async Task<int?> GetStaffIdByPersonAsync(int personId)
    {
        return await _db.Staff
            .Where(s => s.PersonId == personId && s.IsActive)
            .Select(s => (int?)s.StaffId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> OwnsClassSubjectAsync(int staffId, int classSubjectId)
    {
        return await _db.ClassSubjects
            .AnyAsync(cs => cs.ClassSubjectId == classSubjectId && cs.StaffId == staffId && cs.IsActive);
    }

    public async Task<bool> OwnsClassroomAsync(int staffId, int classroomId)
    {
        // یا معلم درسی از این کلاس را تدریس می‌کند، یا معلم رهبر کلاس (HeadTeacher) است
        var isHead = await _db.Classrooms.AnyAsync(c => c.ClassroomId == classroomId && c.HeadTeacherStaffId == staffId);
        if (isHead) return true;

        return await _db.ClassSubjects
            .AnyAsync(cs => cs.ClassroomId == classroomId && cs.StaffId == staffId && cs.IsActive);
    }

    public async Task<Result<TeacherDashboardDto>> GetDashboardAsync(int staffId, TeacherDashboardFilter? filter = null)
    {
        var staff = await _db.Staff.Include(s => s.Person)
            .AsNoTracking().FirstOrDefaultAsync(s => s.StaffId == staffId);
        if (staff is null) return Result<TeacherDashboardDto>.Fail("معلم یافت نشد");

        // سال تحصیلی فعال در صورت ندادن
        var academicYearId = filter?.AcademicYearId
            ?? await _db.AcademicYears.Where(a => a.IsActive).Select(a => (int?)a.AcademicYearId).FirstOrDefaultAsync();

        var myClasses = await GetMyClassesAsync(staffId, academicYearId);

        var today = DateTime.Today;
        var todaySchedule = await GetTodayScheduleAsync(staffId);
        var upcomingExams = await GetUpcomingExamsAsync(staffId, 14);

        // کلاس‌های امروز که حضور و غیابشان ثبت نشده
        var pendingAttendance = 0;
        foreach (var t in todaySchedule)
        {
            var exists = await _db.Attendances
                .AnyAsync(a => a.ClassroomId == t.ClassroomId
                    && a.ClassSubjectId == t.ClassSubjectId
                    && a.AttendanceDate.Date == today
                    && a.RecordedByStaffId == staffId);
            t.AttendanceTaken = exists;
            if (!exists) pendingAttendance++;
        }

        var dto = new TeacherDashboardDto
        {
            StaffId = staffId,
            FullName = $"{staff.Person.FirstName} {staff.Person.LastName}",
            PersonnelCode = staff.PersonnelCode,
            PhotoPath = staff.Person.PhotoPath,
            TotalClasses = myClasses.Count,
            TotalStudents = myClasses.Sum(c => c.StudentCount),
            TodayClasses = todaySchedule.Count,
            PendingAttendance = pendingAttendance,
            UpcomingExams = upcomingExams.Count,
            UnreadMessages = 0, // TODO: بعد از پیاده‌سازی Messaging
            PendingHomeworks = 0, // TODO: بعد از پیاده‌سازی Homework
            TotalAnnouncements = await _db.Announcements
                .CountAsync(a => a.IsActive && (a.ExpiryDate == null || a.ExpiryDate > DateTime.Now)
                    && (a.TargetAudience == "All" || a.TargetAudience == "Teachers")),
            MyClasses = myClasses.Take(8).ToList(),
            TodaySchedule = todaySchedule,
            UpcomingExamsList = upcomingExams.Take(5).ToList(),
            RecentActivities = await GetRecentActivitiesAsync(staffId, 8)
        };

        return Result<TeacherDashboardDto>.Ok(dto);
    }

    public async Task<List<TeacherClassBriefDto>> GetMyClassesAsync(int staffId, int? academicYearId = null)
    {
        academicYearId ??= await _db.AcademicYears.Where(a => a.IsActive)
            .Select(a => (int?)a.AcademicYearId).FirstOrDefaultAsync();

        var q = _db.ClassSubjects
            .Include(cs => cs.Classroom).ThenInclude(c => c.School)
            .Include(cs => cs.Classroom).ThenInclude(c => c.Grade)
            .Include(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .AsNoTracking()
            .Where(cs => cs.StaffId == staffId && cs.IsActive);

        if (academicYearId.HasValue)
            q = q.Where(cs => cs.Classroom.AcademicYearId == academicYearId);

        var classes = await q.Select(cs => new TeacherClassBriefDto
        {
            ClassSubjectId = cs.ClassSubjectId,
            ClassroomId = cs.ClassroomId,
            ClassroomName = cs.Classroom.Name,
            SchoolId = cs.Classroom.SchoolId,
            SchoolName = cs.Classroom.School.Name,
            SubjectName = cs.GradeSubject.Subject.Name,
            GradeName = cs.Classroom.Grade.Name,
            StudentCount = _db.Enrollments.Count(e => e.ClassroomId == cs.ClassroomId && e.Status == "فعال"),
            WeeklyHours = cs.GradeSubject.WeeklyHours
        }).ToListAsync();

        return classes;
    }

    public async Task<Result<TeacherClassDetailDto>> GetClassDetailAsync(int staffId, int classSubjectId)
    {
        if (!await OwnsClassSubjectAsync(staffId, classSubjectId))
            return Result<TeacherClassDetailDto>.Fail("دسترسی به این کلاس مجاز نیست");

        var cs = await _db.ClassSubjects
            .Include(x => x.Classroom).ThenInclude(c => c.School)
            .Include(x => x.Classroom).ThenInclude(c => c.Grade)
            .Include(x => x.Classroom).ThenInclude(c => c.AcademicYear)
            .Include(x => x.GradeSubject).ThenInclude(gs => gs.Subject)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassSubjectId == classSubjectId);

        if (cs is null) return Result<TeacherClassDetailDto>.Fail("کلاس یافت نشد");

        var detail = new TeacherClassDetailDto
        {
            ClassSubjectId = cs.ClassSubjectId,
            ClassroomId = cs.ClassroomId,
            ClassroomName = cs.Classroom.Name,
            SchoolName = cs.Classroom.School.Name,
            SubjectName = cs.GradeSubject.Subject.Name,
            GradeName = cs.Classroom.Grade.Name,
            AcademicYearTitle = cs.Classroom.AcademicYear.Title,
            WeeklyHours = cs.GradeSubject.WeeklyHours,
            Students = await GetClassStudentsAsync(staffId, classSubjectId)
        };

        // خلاصه آزمون‌ها
        detail.Exams = await _db.Exams
            .Include(e => e.ExamType)
            .Where(e => e.ClassSubjectId == classSubjectId)
            .OrderByDescending(e => e.ExamDate)
            .Take(20)
            .Select(e => new TeacherClassExamSummary
            {
                ExamId = e.ExamId,
                Title = e.Title,
                ExamDate = e.ExamDate,
                ExamTypeName = e.ExamType.Name,
                IsFinalized = e.IsFinalized,
                TotalStudents = detail.Students.Count,
                ScoredCount = _db.ExamScores.Count(s => s.ExamId == e.ExamId),
                ClassAverage = _db.ExamScores
                    .Where(s => s.ExamId == e.ExamId && s.NumericScore != null)
                    .Average(s => (decimal?)s.NumericScore)
            }).ToListAsync();

        // خلاصه حضور و غیاب ۱۰ روز اخیر
        var since = DateTime.Today.AddDays(-30);
        detail.RecentAttendance = await _db.Attendances
            .Where(a => a.ClassroomId == cs.ClassroomId && a.ClassSubjectId == classSubjectId
                && a.AttendanceDate >= since)
            .GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new TeacherClassAttendanceSummary
            {
                Date = g.Key,
                PresentCount = g.Count(x => !x.Status.IsAbsent && !x.Status.IsTardy),
                AbsentCount = g.Count(x => x.Status.IsAbsent),
                TardyCount = g.Count(x => x.Status.IsTardy)
            })
            .OrderByDescending(x => x.Date)
            .Take(10)
            .ToListAsync();

        return Result<TeacherClassDetailDto>.Ok(detail);
    }

    public async Task<List<TeacherClassStudentRow>> GetClassStudentsAsync(int staffId, int classSubjectId)
    {
        var cs = await _db.ClassSubjects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassSubjectId == classSubjectId && x.StaffId == staffId && x.IsActive);
        if (cs is null) return new List<TeacherClassStudentRow>();

        var since = DateTime.Today.AddMonths(-12);

        var rows = await _db.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Where(e => e.ClassroomId == cs.ClassroomId && e.Status == "فعال")
            .Select(e => new TeacherClassStudentRow
            {
                StudentId = e.StudentId,
                StudentCode = e.Student.StudentCode,
                FullName = e.Student.Person.FirstName + " " + e.Student.Person.LastName,
                Gender = e.Student.Person.Gender,
                Mobile = e.Student.Person.Mobile,
                PhotoPath = e.Student.Person.PhotoPath,
                AverageScore = _db.ExamScores
                    .Where(s => s.StudentId == e.StudentId
                        && s.Exam.ClassSubjectId == classSubjectId
                        && s.NumericScore != null)
                    .Average(s => (decimal?)s.NumericScore),
                AbsenceCount = _db.Attendances.Count(a => a.StudentId == e.StudentId
                    && a.ClassroomId == cs.ClassroomId
                    && a.ClassSubjectId == classSubjectId
                    && a.AttendanceDate >= since
                    && a.Status.IsAbsent),
                TardyCount = _db.Attendances.Count(a => a.StudentId == e.StudentId
                    && a.ClassroomId == cs.ClassroomId
                    && a.ClassSubjectId == classSubjectId
                    && a.AttendanceDate >= since
                    && a.Status.IsTardy),
                DisciplineRewards = _db.DisciplinaryRecords.Count(d => d.StudentId == e.StudentId
                    && d.Type.Category == "R"),
                DisciplinePunishments = _db.DisciplinaryRecords.Count(d => d.StudentId == e.StudentId
                    && d.Type.Category == "P")
            })
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return rows;
    }

    public async Task<List<TeacherTodayClassDto>> GetWeeklyScheduleAsync(int staffId, int? academicYearId = null)
    {
        // اگر جدول ClassSchedules خالی است، چیزی برنگردان (بعداً Schedule پیاده می‌شود)
        var hasSchedule = await _db.Set<Domain.Entities.ClassSchedule>().AnyAsync();
        if (!hasSchedule) return new List<TeacherTodayClassDto>();

        var q = _db.Set<Domain.Entities.ClassSchedule>()
            .Include(s => s.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(s => s.Classroom)
            .Include(s => s.Period)
            .AsNoTracking()
            .Where(s => s.ClassSubject.StaffId == staffId && s.IsActive);

        if (academicYearId.HasValue)
            q = q.Where(s => s.Classroom.AcademicYearId == academicYearId);

        return await q.OrderBy(s => s.DayOfWeek).ThenBy(s => s.Period.StartTime)
        .Select(s => new TeacherTodayClassDto
        {
            ClassSubjectId = s.ClassSubjectId,
            ClassroomId = s.ClassroomId,
            ClassroomName = s.Classroom.Name,
            SubjectName = s.ClassSubject.GradeSubject.Subject.Name,
            StartTime = s.Period.StartTime.ToString(@"hh\:mm"),
            EndTime = s.Period.EndTime.ToString(@"hh\:mm"),
            SessionNo = s.Period.PeriodNo,
            DayOfWeek = s.DayOfWeek
        }).ToListAsync();
    }

    public async Task<List<TeacherTodayClassDto>> GetTodayScheduleAsync(int staffId)
    {
        // روز هفته به سبک ایرانی: شنبه=0, ... جمعه=6
        var dow = (byte)(((int)DateTime.Today.DayOfWeek + 1) % 7); // .NET: Sunday=0 -> فارسی: شنبه=0
        // تبدیل دقیق‌تر:
        // DayOfWeek.Saturday = 6 -> 0 (شنبه)
        // DayOfWeek.Sunday   = 0 -> 1 (یکشنبه)
        // DayOfWeek.Monday   = 1 -> 2
        // ...
        // DayOfWeek.Friday   = 5 -> 6
        dow = (byte)(((int)DateTime.Today.DayOfWeek + 1) % 7);

        var hasSchedule = await _db.Set<Domain.Entities.ClassSchedule>().AnyAsync();
        if (!hasSchedule) return new List<TeacherTodayClassDto>();

        return await _db.Set<Domain.Entities.ClassSchedule>()
            .Include(s => s.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(s => s.Classroom)
            .Include(s => s.Period)
            .AsNoTracking()
            .Where(s => s.ClassSubject.StaffId == staffId && s.IsActive && s.DayOfWeek == dow)
            .OrderBy(s => s.Period.StartTime)
            .Select(s => new TeacherTodayClassDto
            {
                ClassSubjectId = s.ClassSubjectId,
                ClassroomId = s.ClassroomId,
                ClassroomName = s.Classroom.Name,
                SubjectName = s.ClassSubject.GradeSubject.Subject.Name,
                StartTime = s.Period.StartTime.ToString(@"hh\:mm"),
                EndTime = s.Period.EndTime.ToString(@"hh\:mm"),
                SessionNo = s.Period.PeriodNo,
                DayOfWeek = s.DayOfWeek
            }).ToListAsync();
    }

    public async Task<List<TeacherUpcomingExamDto>> GetUpcomingExamsAsync(int staffId, int daysAhead = 14)
    {
        var today = DateTime.Today;
        var end = today.AddDays(daysAhead);

        return await _db.Exams
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.Classroom)
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(e => e.ExamType)
            .AsNoTracking()
            .Where(e => e.ClassSubject.StaffId == staffId && e.ExamDate >= today && e.ExamDate <= end)
            .OrderBy(e => e.ExamDate)
            .Select(e => new TeacherUpcomingExamDto
            {
                ExamId = e.ExamId,
                Title = e.Title,
                ExamDate = e.ExamDate,
                ClassroomName = e.ClassSubject.Classroom.Name,
                SubjectName = e.ClassSubject.GradeSubject.Subject.Name,
                ExamTypeName = e.ExamType.Name,
                ScoresEntered = _db.ExamScores.Any(s => s.ExamId == e.ExamId)
            }).ToListAsync();
    }

    public async Task<List<TeacherRecentActivityDto>> GetRecentActivitiesAsync(int staffId, int limit = 10)
    {
        var list = new List<TeacherRecentActivityDto>();

        // آخرین حضور و غیاب‌های ثبت شده
        var attendance = await _db.Attendances
            .Include(a => a.Classroom)
            .Where(a => a.RecordedByStaffId == staffId)
            .GroupBy(a => new { a.ClassroomId, a.AttendanceDate.Date, a.ClassSubjectId })
            .Select(g => new
            {
                g.Key.ClassroomId,
                ClassName = g.First().Classroom.Name,
                Date = g.Key.Date,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Date)
            .Take(limit)
            .ToListAsync();

        foreach (var a in attendance)
        {
            list.Add(new TeacherRecentActivityDto
            {
                Type = "attendance",
                Description = $"ثبت حضور و غیاب کلاس {a.ClassName} ({a.Count} نفر)",
                At = a.Date,
                Link = $"/Attendance/Take?classroomId={a.ClassroomId}"
            });
        }

        // آخرین آزمون‌های ساخته شده
        var exams = await _db.Exams
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.Classroom)
            .Where(e => e.CreatedByStaffId == staffId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new TeacherRecentActivityDto
            {
                Type = "exam",
                Description = $"ایجاد آزمون «{e.Title}» در کلاس {e.ClassSubject.Classroom.Name}",
                At = e.CreatedAt,
                Link = $"/Exams/Scores/{e.ExamId}"
            }).ToListAsync();

        list.AddRange(exams);

        return list.OrderByDescending(x => x.At).Take(limit).ToList();
    }
}

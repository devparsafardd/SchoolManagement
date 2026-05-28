using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Common;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence;

public class SmsDbContext : DbContext
{
    public SmsDbContext(DbContextOptions<SmsDbContext> options) : base(options) { }

    // افراد و کاربران
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    // ساختار آموزشی
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();

    // کارکنان و دانش‌آموزان و اولیا
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffSchoolAssignment> StaffAssignments => Set<StaffSchoolAssignment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentEnrollment> Enrollments => Set<StudentEnrollment>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

    public DbSet<Classroom> Classrooms => Set<Classroom>();

    // دروس
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<GradeSubject> GradeSubjects => Set<GradeSubject>();
    public DbSet<ClassSubjectTeacher> ClassSubjects => Set<ClassSubjectTeacher>();

    // حضور و غیاب
    public DbSet<AttendanceStatus> AttendanceStatuses => Set<AttendanceStatus>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // آزمون و نمره
    public DbSet<ExamType> ExamTypes => Set<ExamType>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<GradeScaleItem> GradeScaleItems => Set<GradeScaleItem>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamScore> ExamScores => Set<ExamScore>();

    // انضباطی
    public DbSet<DisciplinaryType> DisciplinaryTypes => Set<DisciplinaryType>();
    public DbSet<DisciplinaryRecord> DisciplinaryRecords => Set<DisciplinaryRecord>();

    // مالی
    public DbSet<FeeType> FeeTypes => Set<FeeType>();
    public DbSet<StudentInvoice> StudentInvoices => Set<StudentInvoice>();
    public DbSet<Payment> Payments => Set<Payment>();

    // اطلاع‌رسانی
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<SmsLog> SmsLogs => Set<SmsLog>();

    // گزارش
    public DbSet<TermSubjectGrade> TermSubjectGrades => Set<TermSubjectGrade>();
    public DbSet<StudentTermGPA> StudentTermGPAs => Set<StudentTermGPA>();

    // سیستم
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // برنامه هفتگی و زنگ‌ها
    public DbSet<SchoolPeriod> SchoolPeriods => Set<SchoolPeriod>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();

    // تکالیف و پیام‌ها
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<HomeworkSubmission> HomeworkSubmissions => Set<HomeworkSubmission>();
    public DbSet<Message> Messages => Set<Message>();

    // تقویم و نظرسنجی
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ====== Primary Keys ======
        b.Entity<Person>().HasKey(p => p.PersonId);
        b.Entity<User>().HasKey(u => u.UserId);
        b.Entity<Role>().HasKey(r => r.RoleId);
        b.Entity<UserRole>().HasKey(ur => ur.UserRoleId);
        b.Entity<Province>().HasKey(p => p.ProvinceId);
        b.Entity<City>().HasKey(c => c.CityId);
        b.Entity<EducationLevel>().HasKey(e => e.EducationLevelId);
        b.Entity<Grade>().HasKey(g => g.GradeId);
        b.Entity<School>().HasKey(s => s.SchoolId);
        b.Entity<AcademicYear>().HasKey(a => a.AcademicYearId);
        b.Entity<Term>().HasKey(t => t.TermId);
        b.Entity<Staff>().HasKey(s => s.StaffId);
        b.Entity<StaffSchoolAssignment>().HasKey(a => a.AssignmentId);
        b.Entity<Student>().HasKey(s => s.StudentId);
        b.Entity<StudentEnrollment>().HasKey(e => e.EnrollmentId);
        b.Entity<Classroom>().HasKey(c => c.ClassroomId);
        b.Entity<Subject>().HasKey(s => s.SubjectId);
        b.Entity<GradeSubject>().HasKey(gs => gs.GradeSubjectId);
        b.Entity<ClassSubjectTeacher>().HasKey(cst => cst.ClassSubjectId);
        b.Entity<AttendanceStatus>().HasKey(s => s.StatusId);
        b.Entity<Attendance>().HasKey(a => a.AttendanceId);
        b.Entity<ExamType>().HasKey(et => et.ExamTypeId);
        b.Entity<GradeScale>().HasKey(gs => gs.GradeScaleId);
        b.Entity<GradeScaleItem>().HasKey(gsi => gsi.GradeScaleItemId);
        b.Entity<Exam>().HasKey(e => e.ExamId);
        b.Entity<ExamScore>().HasKey(es => es.ScoreId);
        b.Entity<DisciplinaryType>().HasKey(dt => dt.TypeId);
        b.Entity<DisciplinaryRecord>().HasKey(dr => dr.RecordId);
        b.Entity<Guardian>().HasKey(g => g.GuardianId);
        b.Entity<StudentGuardian>().HasKey(sg => new { sg.StudentId, sg.GuardianId });
        b.Entity<FeeType>().HasKey(f => f.FeeTypeId);
        b.Entity<StudentInvoice>().HasKey(i => i.InvoiceId);
        b.Entity<Payment>().HasKey(p => p.PaymentId);
        b.Entity<Announcement>().HasKey(a => a.AnnouncementId);
        b.Entity<SmsLog>().HasKey(l => l.SmsLogId);
        b.Entity<TermSubjectGrade>().HasKey(t => t.TermSubjectGradeId);
        b.Entity<StudentTermGPA>().HasKey(s => s.StudentTermGPAId);
        b.Entity<SystemSetting>().HasKey(s => s.SettingId);
        b.Entity<AuditLog>().HasKey(a => a.AuditId);
        // برنامه هفتگی و زنگ‌ها
        b.Entity<SchoolPeriod>().HasKey(p => p.PeriodId);
        b.Entity<SchoolPeriod>().HasIndex(p => new { p.SchoolId, p.PeriodNo }).IsUnique();
        b.Entity<SchoolPeriod>().HasOne(p => p.School).WithMany().HasForeignKey(p => p.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ClassSchedule>().HasKey(s => s.ScheduleId);
        b.Entity<ClassSchedule>().HasIndex(s => new { s.ClassroomId, s.DayOfWeek, s.PeriodId }).IsUnique();
        // برای جلوگیری از multiple cascade paths همه را Restrict می‌کنیم
        b.Entity<ClassSchedule>().HasOne(s => s.Classroom).WithMany().HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassSchedule>().HasOne(s => s.ClassSubject).WithMany().HasForeignKey(s => s.ClassSubjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassSchedule>().HasOne(s => s.Period).WithMany().HasForeignKey(s => s.PeriodId).OnDelete(DeleteBehavior.Restrict);

        // تکالیف
        b.Entity<Homework>().HasKey(h => h.HomeworkId);
        b.Entity<Homework>().Property(h => h.MaxScore).HasPrecision(5, 2);
        b.Entity<Homework>().HasOne(h => h.ClassSubject).WithMany().HasForeignKey(h => h.ClassSubjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Homework>().HasOne(h => h.CreatedBy).WithMany().HasForeignKey(h => h.CreatedByStaffId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<HomeworkSubmission>().HasKey(s => s.SubmissionId);
        b.Entity<HomeworkSubmission>().Property(s => s.Score).HasPrecision(5, 2);
        b.Entity<HomeworkSubmission>().HasIndex(s => new { s.HomeworkId, s.StudentId }).IsUnique();
        b.Entity<HomeworkSubmission>().HasOne(s => s.Homework).WithMany(h => h.Submissions).HasForeignKey(s => s.HomeworkId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<HomeworkSubmission>().HasOne(s => s.Student).WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);

        // پیام‌های داخلی
        b.Entity<Message>().HasKey(m => m.MessageId);
        b.Entity<Message>().HasIndex(m => new { m.ToUserId, m.ReadAt });
        b.Entity<Message>().HasOne(m => m.FromUser).WithMany().HasForeignKey(m => m.FromUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Message>().HasOne(m => m.ToUser).WithMany().HasForeignKey(m => m.ToUserId).OnDelete(DeleteBehavior.Restrict);

        // تقویم
        b.Entity<CalendarEvent>().HasKey(e => e.EventId);
        b.Entity<CalendarEvent>().HasOne(e => e.School).WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CalendarEvent>().HasOne(e => e.Classroom).WithMany().HasForeignKey(e => e.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CalendarEvent>().HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        // نظرسنجی
        b.Entity<Survey>().HasKey(s => s.SurveyId);
        b.Entity<Survey>().HasOne(s => s.CreatedBy).WithMany().HasForeignKey(s => s.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SurveyQuestion>().HasKey(q => q.QuestionId);
        b.Entity<SurveyQuestion>().HasOne(q => q.Survey).WithMany(s => s.Questions).HasForeignKey(q => q.SurveyId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SurveyAnswer>().HasKey(a => a.AnswerId);
        b.Entity<SurveyAnswer>().HasOne(a => a.Survey).WithMany().HasForeignKey(a => a.SurveyId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SurveyAnswer>().HasOne(a => a.Question).WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SurveyAnswer>().HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        // ====== Value Generation ======
        b.Entity<AttendanceStatus>().Property(s => s.StatusId).ValueGeneratedNever();

        // ====== Decimal precision ======
        b.Entity<GradeSubject>().Property(p => p.Credits).HasPrecision(4, 2);
        b.Entity<GradeSubject>().Property(p => p.Coefficient).HasPrecision(4, 2);
        b.Entity<GradeSubject>().Property(p => p.WeeklyHours).HasPrecision(4, 2);
        b.Entity<GradeSubject>().Property(p => p.MaxScore).HasPrecision(5, 2);
        b.Entity<GradeSubject>().Property(p => p.PassingScore).HasPrecision(5, 2);
        b.Entity<ExamType>().Property(p => p.DefaultWeight).HasPrecision(5, 2);
        b.Entity<GradeScaleItem>().Property(p => p.NumericEquivalent).HasPrecision(5, 2);
        b.Entity<Exam>().Property(p => p.MaxScore).HasPrecision(5, 2);
        b.Entity<Exam>().Property(p => p.Weight).HasPrecision(5, 2);
        b.Entity<ExamScore>().Property(p => p.NumericScore).HasPrecision(5, 2);
        b.Entity<DisciplinaryType>().Property(p => p.DefaultScoreImpact).HasPrecision(4, 2);
        b.Entity<DisciplinaryRecord>().Property(p => p.ScoreImpact).HasPrecision(4, 2);
        b.Entity<StaffSchoolAssignment>().Property(p => p.WeeklyHours).HasPrecision(5, 2);
        b.Entity<FeeType>().Property(p => p.DefaultAmount).HasPrecision(15, 0);
        b.Entity<StudentInvoice>().Property(p => p.Amount).HasPrecision(15, 0);
        b.Entity<StudentInvoice>().Property(p => p.Discount).HasPrecision(15, 0);
        b.Entity<StudentInvoice>().Property(p => p.NetAmount).HasPrecision(15, 0);
        b.Entity<Payment>().Property(p => p.Amount).HasPrecision(15, 0);
        b.Entity<TermSubjectGrade>().Property(p => p.FinalNumericScore).HasPrecision(5, 2);
        b.Entity<StudentTermGPA>().Property(p => p.GPA).HasPrecision(5, 3);
        b.Entity<StudentTermGPA>().Property(p => p.DisciplineScore).HasPrecision(5, 2);

        // ====== UNIQUE Indexes ======
        b.Entity<Person>().HasIndex(p => p.NationalCode).IsUnique().HasFilter("[NationalCode] IS NOT NULL");
        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        b.Entity<User>().HasIndex(u => u.PersonId).IsUnique();
        b.Entity<Student>().HasIndex(s => s.StudentCode).IsUnique();
        b.Entity<Student>().HasIndex(s => s.PersonId).IsUnique();
        b.Entity<Staff>().HasIndex(s => s.PersonId).IsUnique();
        b.Entity<Staff>().HasIndex(s => s.PersonnelCode).IsUnique().HasFilter("[PersonnelCode] IS NOT NULL");
        b.Entity<School>().HasIndex(s => s.Code).IsUnique();
        b.Entity<AcademicYear>().HasIndex(a => a.Title).IsUnique();
        b.Entity<EducationLevel>().HasIndex(e => e.Code).IsUnique();
        b.Entity<Subject>().HasIndex(s => s.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
        b.Entity<AttendanceStatus>().HasIndex(s => s.Code).IsUnique();
        b.Entity<AttendanceStatus>().HasIndex(s => s.Name).IsUnique();
        b.Entity<ExamType>().HasIndex(t => t.Name).IsUnique();
        b.Entity<DisciplinaryType>().HasIndex(t => t.Name).IsUnique();
        b.Entity<Guardian>().HasIndex(g => g.PersonId).IsUnique();
        b.Entity<StudentInvoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
        b.Entity<SystemSetting>().HasIndex(s => s.Key).IsUnique();

        // ====== Composite UNIQUE ======
        b.Entity<Classroom>().HasIndex(c => new { c.SchoolId, c.AcademicYearId, c.GradeId, c.Name }).IsUnique();
        b.Entity<StudentEnrollment>().HasIndex(e => new { e.StudentId, e.AcademicYearId }).IsUnique();
        b.Entity<StaffSchoolAssignment>().HasIndex(a => new { a.StaffId, a.SchoolId, a.AcademicYearId, a.Position }).IsUnique();
        b.Entity<UserRole>().HasIndex(ur => new { ur.UserId, ur.RoleId, ur.SchoolId, ur.AcademicYearId }).IsUnique();
        b.Entity<GradeSubject>().HasIndex(gs => new { gs.GradeId, gs.SubjectId }).IsUnique();
        b.Entity<ClassSubjectTeacher>().HasIndex(cst => new { cst.ClassroomId, cst.GradeSubjectId }).IsUnique();
        b.Entity<Attendance>().HasIndex(a => new { a.StudentId, a.AttendanceDate, a.ClassSubjectId }).IsUnique();
        b.Entity<ExamScore>().HasIndex(es => new { es.ExamId, es.StudentId }).IsUnique();
        b.Entity<TermSubjectGrade>().HasIndex(t => new { t.StudentId, t.TermId, t.ClassSubjectId }).IsUnique();
        b.Entity<StudentTermGPA>().HasIndex(s => new { s.StudentId, s.TermId }).IsUnique();

        // ====== Useful Indexes ======
        b.Entity<AuditLog>().HasIndex(a => a.CreatedAt);
        b.Entity<AuditLog>().HasIndex(a => new { a.EntityName, a.EntityId });
        b.Entity<AuditLog>().HasIndex(a => a.UserId);
        b.Entity<Attendance>().HasIndex(a => new { a.ClassroomId, a.AttendanceDate });
        b.Entity<Attendance>().HasIndex(a => new { a.StudentId, a.AttendanceDate });
        b.Entity<DisciplinaryRecord>().HasIndex(d => new { d.StudentId, d.AcademicYearId });
        b.Entity<SmsLog>().HasIndex(l => l.CreatedAt);

        // ====== Relationships ======
        b.Entity<User>().HasOne(u => u.Person).WithOne(p => p.User).HasForeignKey<User>(u => u.PersonId);
        b.Entity<Student>().HasOne(s => s.Person).WithOne(p => p.Student).HasForeignKey<Student>(s => s.PersonId);
        b.Entity<Staff>().HasOne(s => s.Person).WithOne(p => p.Staff).HasForeignKey<Staff>(s => s.PersonId);
        b.Entity<Guardian>().HasOne(g => g.Person).WithMany().HasForeignKey(g => g.PersonId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<StudentEnrollment>().HasOne(e => e.AcademicYear).WithMany().HasForeignKey(e => e.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StaffSchoolAssignment>().HasOne(a => a.School).WithMany().HasForeignKey(a => a.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StaffSchoolAssignment>().HasOne(a => a.AcademicYear).WithMany().HasForeignKey(a => a.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Classroom>().HasOne(c => c.AcademicYear).WithMany(a => a.Classrooms).HasForeignKey(c => c.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Classroom>().HasOne(c => c.Grade).WithMany().HasForeignKey(c => c.GradeId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Term>().HasOne(t => t.AcademicYear).WithMany().HasForeignKey(t => t.AcademicYearId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<ClassSubjectTeacher>().HasOne(cst => cst.Classroom).WithMany().HasForeignKey(cst => cst.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassSubjectTeacher>().HasOne(cst => cst.GradeSubject).WithMany().HasForeignKey(cst => cst.GradeSubjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassSubjectTeacher>().HasOne(cst => cst.Staff).WithMany().HasForeignKey(cst => cst.StaffId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<GradeSubject>().HasOne(gs => gs.Grade).WithMany().HasForeignKey(gs => gs.GradeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<GradeSubject>().HasOne(gs => gs.Subject).WithMany().HasForeignKey(gs => gs.SubjectId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Attendance>().HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Attendance>().HasOne(a => a.Classroom).WithMany().HasForeignKey(a => a.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Attendance>().HasOne(a => a.Status).WithMany().HasForeignKey(a => a.StatusId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Exam>().HasOne(e => e.ClassSubject).WithMany().HasForeignKey(e => e.ClassSubjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Exam>().HasOne(e => e.ExamType).WithMany().HasForeignKey(e => e.ExamTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Exam>().HasOne(e => e.Term).WithMany().HasForeignKey(e => e.TermId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Exam>().HasOne(e => e.GradeScale).WithMany().HasForeignKey(e => e.GradeScaleId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<ExamScore>().HasOne(es => es.Exam).WithMany(e => e.Scores).HasForeignKey(es => es.ExamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ExamScore>().HasOne(es => es.Student).WithMany().HasForeignKey(es => es.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExamScore>().HasOne(es => es.DescriptiveScaleItem).WithMany().HasForeignKey(es => es.DescriptiveScaleItemId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<GradeScaleItem>().HasOne(gsi => gsi.GradeScale).WithMany(gs => gs.Items).HasForeignKey(gsi => gsi.GradeScaleId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<DisciplinaryRecord>().HasOne(dr => dr.Student).WithMany().HasForeignKey(dr => dr.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DisciplinaryRecord>().HasOne(dr => dr.Classroom).WithMany().HasForeignKey(dr => dr.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DisciplinaryRecord>().HasOne(dr => dr.Type).WithMany().HasForeignKey(dr => dr.TypeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DisciplinaryRecord>().HasOne(dr => dr.RecordedBy).WithMany().HasForeignKey(dr => dr.RecordedByStaffId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<StudentGuardian>().HasOne(sg => sg.Student).WithMany().HasForeignKey(sg => sg.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StudentGuardian>().HasOne(sg => sg.Guardian).WithMany(g => g.Students).HasForeignKey(sg => sg.GuardianId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<StudentInvoice>().HasOne(i => i.Student).WithMany().HasForeignKey(i => i.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StudentInvoice>().HasOne(i => i.School).WithMany().HasForeignKey(i => i.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StudentInvoice>().HasOne(i => i.AcademicYear).WithMany().HasForeignKey(i => i.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StudentInvoice>().HasOne(i => i.FeeType).WithMany().HasForeignKey(i => i.FeeTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Payment>().HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<TermSubjectGrade>().HasOne(t => t.Student).WithMany().HasForeignKey(t => t.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TermSubjectGrade>().HasOne(t => t.Term).WithMany().HasForeignKey(t => t.TermId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TermSubjectGrade>().HasOne(t => t.ClassSubject).WithMany().HasForeignKey(t => t.ClassSubjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TermSubjectGrade>().HasOne(t => t.FinalDescriptiveItem).WithMany().HasForeignKey(t => t.FinalDescriptiveItemId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<StudentTermGPA>().HasOne(s => s.Student).WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StudentTermGPA>().HasOne(s => s.Term).WithMany().HasForeignKey(s => s.TermId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StudentTermGPA>().HasOne(s => s.Classroom).WithMany().HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Restrict);

        // Soft Delete Filter
        ApplySoftDeleteFilter(b);
    }

    private static void ApplySoftDeleteFilter(ModelBuilder b)
    {
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var param = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
                var condition = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, param);
                b.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(SmsDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Name = "SuperAdmin", DisplayName = "مدیر ارشد سیستم" },
                new Role { Name = "SchoolAdmin", DisplayName = "مدیر سیستم مدرسه" },
                new Role { Name = "Principal", DisplayName = "مدیر مدرسه" },
                new Role { Name = "VicePrincipal", DisplayName = "معاون" },
                new Role { Name = "Teacher", DisplayName = "معلم" },
                new Role { Name = "Counselor", DisplayName = "مشاور" },
                new Role { Name = "Accountant", DisplayName = "حسابدار" },
                new Role { Name = "Student", DisplayName = "دانش‌آموز" },
                new Role { Name = "Parent", DisplayName = "ولی دانش‌آموز" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.EducationLevels.AnyAsync())
        {
            db.EducationLevels.AddRange(
                new EducationLevel { Name = "ابتدایی", Code = "ELEM", IsDescriptive = true, MinGrade = 1, MaxGrade = 6 },
                new EducationLevel { Name = "متوسطه اول", Code = "MID", IsDescriptive = false, MinGrade = 7, MaxGrade = 9 },
                new EducationLevel { Name = "متوسطه دوم", Code = "HIGH", IsDescriptive = false, MinGrade = 10, MaxGrade = 12 },
                new EducationLevel { Name = "فنی و حرفه‌ای", Code = "TECH", IsDescriptive = false, MinGrade = 10, MaxGrade = 12 },
                new EducationLevel { Name = "کاردانش", Code = "CARD", IsDescriptive = false, MinGrade = 10, MaxGrade = 12 }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Grades.AnyAsync())
        {
            var levels = await db.EducationLevels.ToListAsync();
            var elem = levels.First(l => l.Code == "ELEM");
            var mid = levels.First(l => l.Code == "MID");
            var high = levels.First(l => l.Code == "HIGH");

            string[] elemNames = { "اول دبستان", "دوم دبستان", "سوم دبستان", "چهارم دبستان", "پنجم دبستان", "ششم دبستان" };
            for (byte i = 0; i < elemNames.Length; i++)
                db.Grades.Add(new Grade { Name = elemNames[i], OrderNo = (byte)(i + 1), EducationLevelId = elem.EducationLevelId });

            string[] midNames = { "هفتم", "هشتم", "نهم" };
            for (byte i = 0; i < midNames.Length; i++)
                db.Grades.Add(new Grade { Name = midNames[i], OrderNo = (byte)(i + 7), EducationLevelId = mid.EducationLevelId });

            string[] highNames = { "دهم", "یازدهم", "دوازدهم" };
            for (byte i = 0; i < highNames.Length; i++)
                db.Grades.Add(new Grade { Name = highNames[i], OrderNo = (byte)(i + 10), EducationLevelId = high.EducationLevelId });

            await db.SaveChangesAsync();
        }

        if (!await db.AcademicYears.AnyAsync())
        {
            db.AcademicYears.Add(new AcademicYear
            {
                Title = "1404-1405",
                StartDate = new DateTime(2025, 9, 23),
                EndDate = new DateTime(2026, 6, 22),
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Terms.AnyAsync())
        {
            var year = await db.AcademicYears.FirstAsync(a => a.IsActive);
            db.Terms.AddRange(
                new Term { AcademicYearId = year.AcademicYearId, Name = "نوبت اول", OrderNo = 1, StartDate = new DateTime(2025, 9, 23), EndDate = new DateTime(2026, 1, 20) },
                new Term { AcademicYearId = year.AcademicYearId, Name = "نوبت دوم", OrderNo = 2, StartDate = new DateTime(2026, 1, 21), EndDate = new DateTime(2026, 6, 22) }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Provinces.AnyAsync())
        {
            var tehran = new Province { Name = "تهران" };
            var esfahan = new Province { Name = "اصفهان" };
            var fars = new Province { Name = "فارس" };
            var khorasan = new Province { Name = "خراسان رضوی" };
            var azar = new Province { Name = "آذربایجان شرقی" };
            db.Provinces.AddRange(tehran, esfahan, fars, khorasan, azar);
            await db.SaveChangesAsync();

            db.Cities.AddRange(
                new City { Name = "تهران", ProvinceId = tehran.ProvinceId },
                new City { Name = "ری", ProvinceId = tehran.ProvinceId },
                new City { Name = "شهریار", ProvinceId = tehran.ProvinceId },
                new City { Name = "اصفهان", ProvinceId = esfahan.ProvinceId },
                new City { Name = "کاشان", ProvinceId = esfahan.ProvinceId },
                new City { Name = "شیراز", ProvinceId = fars.ProvinceId },
                new City { Name = "مرودشت", ProvinceId = fars.ProvinceId },
                new City { Name = "مشهد", ProvinceId = khorasan.ProvinceId },
                new City { Name = "نیشابور", ProvinceId = khorasan.ProvinceId },
                new City { Name = "تبریز", ProvinceId = azar.ProvinceId }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Subjects.AnyAsync())
        {
            db.Subjects.AddRange(
                new Subject { Name = "ریاضی", Code = "MATH" },
                new Subject { Name = "علوم تجربی", Code = "SCI" },
                new Subject { Name = "فارسی", Code = "FA" },
                new Subject { Name = "نگارش فارسی", Code = "FA_W" },
                new Subject { Name = "املا", Code = "FA_DICT" },
                new Subject { Name = "مطالعات اجتماعی", Code = "SOC" },
                new Subject { Name = "هدیه‌های آسمان", Code = "REL_E" },
                new Subject { Name = "قرآن", Code = "QURAN" },
                new Subject { Name = "زبان انگلیسی", Code = "ENG" },
                new Subject { Name = "عربی", Code = "AR" },
                new Subject { Name = "دینی", Code = "REL" },
                new Subject { Name = "فیزیک", Code = "PHY" },
                new Subject { Name = "شیمی", Code = "CHEM" },
                new Subject { Name = "زیست‌شناسی", Code = "BIO" },
                new Subject { Name = "هندسه", Code = "GEO" },
                new Subject { Name = "حسابان", Code = "CALC" },
                new Subject { Name = "آمار و احتمال", Code = "STAT" },
                new Subject { Name = "ادبیات فارسی", Code = "FA_LIT" },
                new Subject { Name = "تاریخ", Code = "HIST" },
                new Subject { Name = "جغرافیا", Code = "GEOG" },
                new Subject { Name = "فلسفه", Code = "PHIL" },
                new Subject { Name = "روان‌شناسی", Code = "PSY" },
                new Subject { Name = "اقتصاد", Code = "ECON" },
                new Subject { Name = "ورزش", Code = "PE" },
                new Subject { Name = "هنر", Code = "ART" },
                new Subject { Name = "کار و فناوری", Code = "TECH" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.AttendanceStatuses.AnyAsync())
        {
            db.AttendanceStatuses.AddRange(
                new AttendanceStatus { StatusId = 1, Name = "حاضر", Code = "PRESENT", IsAbsent = false, IsTardy = false, Color = "#06d6a0" },
                new AttendanceStatus { StatusId = 2, Name = "غایب غیرموجه", Code = "ABSENT", IsAbsent = true, IsTardy = false, Color = "#ef476f" },
                new AttendanceStatus { StatusId = 3, Name = "غایب موجه", Code = "EXCUSED", IsAbsent = true, IsTardy = false, Color = "#ffd166" },
                new AttendanceStatus { StatusId = 4, Name = "تاخیر", Code = "TARDY", IsAbsent = false, IsTardy = true, Color = "#f59e0b" },
                new AttendanceStatus { StatusId = 5, Name = "مرخصی", Code = "LEAVE", IsAbsent = true, IsTardy = false, Color = "#118ab2" },
                new AttendanceStatus { StatusId = 6, Name = "فرار از مدرسه", Code = "TRUANT", IsAbsent = true, IsTardy = false, Color = "#9c1c4b" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.ExamTypes.AnyAsync())
        {
            db.ExamTypes.AddRange(
                new ExamType { Name = "آزمون کلاسی", Code = "QUIZ", DefaultWeight = 0.5m, IsFinal = false },
                new ExamType { Name = "مستمر", Code = "ONGOING", DefaultWeight = 1.0m, IsFinal = false },
                new ExamType { Name = "پرسش شفاهی", Code = "ORAL", DefaultWeight = 0.5m, IsFinal = false },
                new ExamType { Name = "میان‌ترم", Code = "MID", DefaultWeight = 1.5m, IsFinal = false },
                new ExamType { Name = "نوبت اول", Code = "TERM1", DefaultWeight = 2.0m, IsFinal = true },
                new ExamType { Name = "نوبت دوم", Code = "TERM2", DefaultWeight = 2.0m, IsFinal = true },
                new ExamType { Name = "پایانی", Code = "FINAL", DefaultWeight = 3.0m, IsFinal = true },
                new ExamType { Name = "پودمانی", Code = "MODULE", DefaultWeight = 1.0m, IsFinal = false },
                new ExamType { Name = "پروژه", Code = "PROJ", DefaultWeight = 1.0m, IsFinal = false }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.GradeScales.AnyAsync())
        {
            var scale4 = new GradeScale { Name = "توصیفی ۴ سطحی ابتدایی", IsDescriptive = true };
            db.GradeScales.Add(scale4);
            await db.SaveChangesAsync();

            db.GradeScaleItems.AddRange(
                new GradeScaleItem { GradeScaleId = scale4.GradeScaleId, Symbol = "خ", Label = "خیلی خوب", NumericEquivalent = 19, OrderNo = 1 },
                new GradeScaleItem { GradeScaleId = scale4.GradeScaleId, Symbol = "خو", Label = "خوب", NumericEquivalent = 16, OrderNo = 2 },
                new GradeScaleItem { GradeScaleId = scale4.GradeScaleId, Symbol = "ق", Label = "قابل قبول", NumericEquivalent = 13, OrderNo = 3 },
                new GradeScaleItem { GradeScaleId = scale4.GradeScaleId, Symbol = "ن", Label = "نیازمند تلاش بیشتر", NumericEquivalent = 9, OrderNo = 4 }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.DisciplinaryTypes.AnyAsync())
        {
            db.DisciplinaryTypes.AddRange(
                new DisciplinaryType { Name = "تشویق کلاسی", Category = "R", Severity = 1, DefaultScoreImpact = 0.25m },
                new DisciplinaryType { Name = "تشویق مدرسه", Category = "R", Severity = 2, DefaultScoreImpact = 0.50m },
                new DisciplinaryType { Name = "تقدیر کتبی", Category = "R", Severity = 3, DefaultScoreImpact = 1.00m },
                new DisciplinaryType { Name = "لوح تقدیر", Category = "R", Severity = 4, DefaultScoreImpact = 1.50m },
                new DisciplinaryType { Name = "تذکر شفاهی", Category = "P", Severity = 1, DefaultScoreImpact = -0.25m },
                new DisciplinaryType { Name = "تذکر کتبی", Category = "P", Severity = 2, DefaultScoreImpact = -0.50m },
                new DisciplinaryType { Name = "احضار اولیا", Category = "P", Severity = 3, DefaultScoreImpact = -1.00m },
                new DisciplinaryType { Name = "تعهد کتبی", Category = "P", Severity = 3, DefaultScoreImpact = -1.00m },
                new DisciplinaryType { Name = "تعلیق از تحصیل", Category = "P", Severity = 5, DefaultScoreImpact = -2.00m }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.FeeTypes.AnyAsync())
        {
            db.FeeTypes.AddRange(
                new FeeType { Name = "شهریه ثابت", IsRecurring = false, DefaultAmount = 50_000_000 },
                new FeeType { Name = "شهریه ماهانه", IsRecurring = true, DefaultAmount = 5_000_000 },
                new FeeType { Name = "سرویس مدرسه", IsRecurring = true, DefaultAmount = 2_500_000 },
                new FeeType { Name = "کتب درسی", IsRecurring = false, DefaultAmount = 3_000_000 },
                new FeeType { Name = "کلاس فوق‌برنامه", IsRecurring = false },
                new FeeType { Name = "اردو و گردش علمی", IsRecurring = false },
                new FeeType { Name = "ثبت‌نام اولیه", IsRecurring = false }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.SystemSettings.AnyAsync())
        {
            db.SystemSettings.AddRange(
                new SystemSetting { Key = "AppName", Value = "سیستم مدیریت مدارس", Category = "General" },
                new SystemSetting { Key = "AppVersion", Value = "2.0.0", Category = "General" },
                new SystemSetting { Key = "DefaultPasswordLength", Value = "6", Category = "Security" },
                new SystemSetting { Key = "MaxFailedLoginAttempts", Value = "5", Category = "Security" },
                new SystemSetting { Key = "SmsProvider", Value = "Fake", Category = "Sms" },
                new SystemSetting { Key = "SmsApiKey", Value = "", Category = "Sms" },
                new SystemSetting { Key = "SmsSenderNumber", Value = "10001234", Category = "Sms" },
                new SystemSetting { Key = "AutoNotifyAbsence", Value = "false", Category = "Sms" },
                new SystemSetting { Key = "ReportCardSchoolName", Value = "", Category = "Report" },
                new SystemSetting { Key = "ReportCardLogo", Value = "", Category = "Report" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var person = new Person { FirstName = "مدیر", LastName = "سیستم", Gender = "M", Mobile = "09120000000" };
            db.Persons.Add(person);
            await db.SaveChangesAsync();

            var user = new User
            {
                PersonId = person.PersonId,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var superAdminRole = await db.Roles.FirstAsync(r => r.Name == "SuperAdmin");
            db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = superAdminRole.RoleId });
            await db.SaveChangesAsync();
        }
    }
}

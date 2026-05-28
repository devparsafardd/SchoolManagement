using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence;

/// <summary>
/// داده‌های نمونه (Demo) برای تست همه بخش‌های سیستم.
/// فقط زمانی اجرا می‌شود که هیچ مدرسه‌ای در دیتابیس وجود نداشته باشد.
/// در محیط Production هرگز اجرا نمی‌شود.
/// </summary>
public static class DemoDataSeeder
{
    private static readonly Random _rnd = new(12345); // ثابت برای تکرارپذیری

    public static async Task SeedDemoAsync(SmsDbContext db, ILogger? logger = null)
    {
        // اگه قبلاً داده دمو وجود داشت، خروج
        if (await db.Schools.AnyAsync())
        {
            logger?.LogInformation("Demo data already exists, skipping.");
            return;
        }

        logger?.LogInformation("🌱 Seeding demo data ...");

        var academicYearId = await db.AcademicYears.Where(a => a.IsActive).Select(a => a.AcademicYearId).FirstAsync();
        var termIds = await db.Terms.Where(t => t.AcademicYearId == academicYearId).OrderBy(t => t.OrderNo).Select(t => t.TermId).ToListAsync();
        var subjects = await db.Subjects.ToListAsync();
        var statuses = await db.AttendanceStatuses.ToListAsync();
        var examTypes = await db.ExamTypes.ToListAsync();
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.RoleId);
        var disciplineTypes = await db.DisciplinaryTypes.ToListAsync();
        var feeTypes = await db.FeeTypes.ToListAsync();

        // ====== ۱) مدارس ======
        var tehranCityId = await db.Cities.Where(c => c.Name == "تهران").Select(c => c.CityId).FirstAsync();
        var shirazCityId = await db.Cities.Where(c => c.Name == "شیراز").Select(c => c.CityId).FirstAsync();

        var levels = await db.EducationLevels.ToDictionaryAsync(l => l.Code, l => l.EducationLevelId);

        var school1 = new School
        {
            Name = "دبستان شهید بهشتی",
            Code = "TEH-001",
            CityId = tehranCityId,
            Address = "تهران، خیابان آزادی، کوچه گلستان، پلاک ۱۲",
            Phone = "02144556677",
            Gender = "B",
            SchoolType = "دولتی",
            EducationLevelId = levels["ELEM"],
            IsActive = true
        };
        var school2 = new School
        {
            Name = "دبیرستان دخترانه فردوسی",
            Code = "SHZ-002",
            CityId = shirazCityId,
            Address = "شیراز، بلوار حافظ، خیابان سعدی، پلاک ۴۵",
            Phone = "07132345678",
            Gender = "F",
            SchoolType = "غیرانتفاعی",
            EducationLevelId = levels["HIGH"],
            IsActive = true
        };
        db.Schools.AddRange(school1, school2);
        await db.SaveChangesAsync();

        // ====== ۲) زنگ‌های مدرسه ======
        var periods = new List<SchoolPeriod>();
        foreach (var school in new[] { school1, school2 })
        {
            periods.AddRange(new[]
            {
                new SchoolPeriod { SchoolId = school.SchoolId, PeriodNo = 1, Name = "زنگ اول", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(8, 45, 0) },
                new SchoolPeriod { SchoolId = school.SchoolId, PeriodNo = 2, Name = "زنگ دوم", StartTime = new TimeSpan(8, 55, 0), EndTime = new TimeSpan(9, 40, 0) },
                new SchoolPeriod { SchoolId = school.SchoolId, PeriodNo = 3, Name = "تفریح اول", StartTime = new TimeSpan(9, 40, 0), EndTime = new TimeSpan(10, 0, 0), IsBreak = true },
                new SchoolPeriod { SchoolId = school.SchoolId, PeriodNo = 4, Name = "زنگ سوم", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(10, 45, 0) },
                new SchoolPeriod { SchoolId = school.SchoolId, PeriodNo = 5, Name = "زنگ چهارم", StartTime = new TimeSpan(10, 55, 0), EndTime = new TimeSpan(11, 40, 0) },
                new SchoolPeriod { SchoolId = school.SchoolId, PeriodNo = 6, Name = "زنگ پنجم", StartTime = new TimeSpan(11, 50, 0), EndTime = new TimeSpan(12, 35, 0) },
            });
        }
        db.SchoolPeriods.AddRange(periods);
        await db.SaveChangesAsync();

        // ====== ۳) کارکنان (Staff) ======
        // مدیر مدرسه ۱
        var principal1 = await CreatePersonWithStaffAndUserAsync(db, "احمد", "محمدی", "M", "principal1", "Pass@123",
            personnelCode: "P-001", mobile: "09121111001", degree: "کارشناسی ارشد");
        await AssignToSchoolAsync(db, principal1.Staff!.StaffId, school1.SchoolId, academicYearId, "Principal");
        await AssignUserRoleAsync(db, principal1.User!.UserId, roles["Principal"], school1.SchoolId);

        // معاون مدرسه ۱
        var vp1 = await CreatePersonWithStaffAndUserAsync(db, "زهرا", "کریمی", "F", "vp1", "Pass@123",
            personnelCode: "P-002", mobile: "09121111002");
        await AssignToSchoolAsync(db, vp1.Staff!.StaffId, school1.SchoolId, academicYearId, "VicePrincipal");
        await AssignUserRoleAsync(db, vp1.User!.UserId, roles["VicePrincipal"], school1.SchoolId);

        // مدیر مدرسه ۲
        var principal2 = await CreatePersonWithStaffAndUserAsync(db, "مریم", "حسینی", "F", "principal2", "Pass@123",
            personnelCode: "P-003", mobile: "09121111003", degree: "دکتری");
        await AssignToSchoolAsync(db, principal2.Staff!.StaffId, school2.SchoolId, academicYearId, "Principal");
        await AssignUserRoleAsync(db, principal2.User!.UserId, roles["Principal"], school2.SchoolId);

        // معلم‌های مدرسه ۱ (ابتدایی)
        var t1 = await CreatePersonWithStaffAndUserAsync(db, "علی", "رضایی", "M", "teacher1", "Pass@123",
            personnelCode: "T-101", mobile: "09121112001", fieldOfStudy: "آموزش ابتدایی");
        var t2 = await CreatePersonWithStaffAndUserAsync(db, "فاطمه", "نوری", "F", "teacher2", "Pass@123",
            personnelCode: "T-102", mobile: "09121112002", fieldOfStudy: "آموزش ابتدایی");
        var t3 = await CreatePersonWithStaffAndUserAsync(db, "حسین", "احمدی", "M", "teacher3", "Pass@123",
            personnelCode: "T-103", mobile: "09121112003", fieldOfStudy: "ریاضی");
        foreach (var t in new[] { t1, t2, t3 })
        {
            await AssignToSchoolAsync(db, t.Staff!.StaffId, school1.SchoolId, academicYearId, "Teacher", 18);
            await AssignUserRoleAsync(db, t.User!.UserId, roles["Teacher"], school1.SchoolId);
        }

        // معلم‌های مدرسه ۲ (متوسطه دوم)
        var t4 = await CreatePersonWithStaffAndUserAsync(db, "نرگس", "صالحی", "F", "teacher4", "Pass@123",
            personnelCode: "T-201", mobile: "09121112004", fieldOfStudy: "ریاضی");
        var t5 = await CreatePersonWithStaffAndUserAsync(db, "سارا", "موسوی", "F", "teacher5", "Pass@123",
            personnelCode: "T-202", mobile: "09121112005", fieldOfStudy: "فیزیک");
        var t6 = await CreatePersonWithStaffAndUserAsync(db, "آرزو", "اکبری", "F", "teacher6", "Pass@123",
            personnelCode: "T-203", mobile: "09121112006", fieldOfStudy: "ادبیات");
        foreach (var t in new[] { t4, t5, t6 })
        {
            await AssignToSchoolAsync(db, t.Staff!.StaffId, school2.SchoolId, academicYearId, "Teacher", 20);
            await AssignUserRoleAsync(db, t.User!.UserId, roles["Teacher"], school2.SchoolId);
        }

        // حسابدار سراسری
        var accountant = await CreatePersonWithStaffAndUserAsync(db, "محمد", "جعفری", "M", "accountant", "Pass@123",
            personnelCode: "A-001", mobile: "09121113001");
        await AssignToSchoolAsync(db, accountant.Staff!.StaffId, school1.SchoolId, academicYearId, "Admin");
        await AssignUserRoleAsync(db, accountant.User!.UserId, roles["Accountant"], school1.SchoolId);

        await db.SaveChangesAsync();

        // ====== ۴) کلاس‌ها ======
        var grade5Id = await db.Grades.Where(g => g.Name == "پنجم دبستان").Select(g => g.GradeId).FirstAsync();
        var grade6Id = await db.Grades.Where(g => g.Name == "ششم دبستان").Select(g => g.GradeId).FirstAsync();
        var grade10Id = await db.Grades.Where(g => g.Name == "دهم").Select(g => g.GradeId).FirstAsync();
        var grade11Id = await db.Grades.Where(g => g.Name == "یازدهم").Select(g => g.GradeId).FirstAsync();

        var cls5A = new Classroom { SchoolId = school1.SchoolId, AcademicYearId = academicYearId, GradeId = grade5Id, Name = "پنجم الف", Capacity = 25, HeadTeacherStaffId = t1.Staff.StaffId, RoomNumber = "101" };
        var cls5B = new Classroom { SchoolId = school1.SchoolId, AcademicYearId = academicYearId, GradeId = grade5Id, Name = "پنجم ب", Capacity = 25, HeadTeacherStaffId = t2.Staff.StaffId, RoomNumber = "102" };
        var cls6A = new Classroom { SchoolId = school1.SchoolId, AcademicYearId = academicYearId, GradeId = grade6Id, Name = "ششم الف", Capacity = 30, HeadTeacherStaffId = t3.Staff.StaffId, RoomNumber = "201" };
        var cls10A = new Classroom { SchoolId = school2.SchoolId, AcademicYearId = academicYearId, GradeId = grade10Id, Name = "دهم ریاضی", Capacity = 28, HeadTeacherStaffId = t4.Staff.StaffId, RoomNumber = "A-1" };
        var cls11A = new Classroom { SchoolId = school2.SchoolId, AcademicYearId = academicYearId, GradeId = grade11Id, Name = "یازدهم تجربی", Capacity = 28, HeadTeacherStaffId = t5.Staff.StaffId, RoomNumber = "B-1" };

        db.Classrooms.AddRange(cls5A, cls5B, cls6A, cls10A, cls11A);
        await db.SaveChangesAsync();

        // ====== ۵) GradeSubjects (تخصیص درس‌ها به پایه) ======
        var subjMap = subjects.ToDictionary(s => s.Code!, s => s);

        async Task AddGradeSubjectsAsync(int gradeId, params (string code, decimal coef, decimal weekly, bool desc)[] items)
        {
            foreach (var i in items)
            {
                if (!subjMap.ContainsKey(i.code)) continue;
                db.GradeSubjects.Add(new GradeSubject
                {
                    GradeId = gradeId, SubjectId = subjMap[i.code].SubjectId,
                    Coefficient = i.coef, WeeklyHours = i.weekly, IsDescriptive = i.desc,
                    MaxScore = 20, PassingScore = 10
                });
            }
        }

        await AddGradeSubjectsAsync(grade5Id,
            ("MATH", 2, 4, true), ("SCI", 2, 3, true), ("FA", 2, 5, true),
            ("SOC", 1, 2, true), ("ENG", 1, 2, true), ("PE", 1, 2, true), ("ART", 1, 2, true));
        await AddGradeSubjectsAsync(grade6Id,
            ("MATH", 2, 4, true), ("SCI", 2, 3, true), ("FA", 2, 5, true),
            ("SOC", 1, 2, true), ("ENG", 1, 2, true), ("PE", 1, 2, true));
        await AddGradeSubjectsAsync(grade10Id,
            ("MATH", 3, 4, false), ("PHY", 3, 4, false), ("CHEM", 2, 3, false),
            ("FA_LIT", 2, 2, false), ("ENG", 2, 2, false), ("AR", 1, 2, false), ("REL", 1, 2, false));
        await AddGradeSubjectsAsync(grade11Id,
            ("MATH", 3, 4, false), ("PHY", 3, 4, false), ("CHEM", 2, 3, false), ("BIO", 3, 3, false),
            ("FA_LIT", 2, 2, false), ("ENG", 2, 2, false));
        await db.SaveChangesAsync();

        // ====== ۶) ClassSubjectTeachers ======
        // مدرسه ۱
        await AssignSubjectsAsync(db, cls5A, new[] { ("MATH", t1), ("SCI", t1), ("FA", t1), ("SOC", t2), ("ENG", t2), ("PE", t3), ("ART", t2) }, subjMap);
        await AssignSubjectsAsync(db, cls5B, new[] { ("MATH", t2), ("SCI", t2), ("FA", t2), ("SOC", t1), ("ENG", t1), ("PE", t3), ("ART", t1) }, subjMap);
        await AssignSubjectsAsync(db, cls6A, new[] { ("MATH", t3), ("SCI", t3), ("FA", t1), ("SOC", t2), ("ENG", t2), ("PE", t3) }, subjMap);
        // مدرسه ۲
        await AssignSubjectsAsync(db, cls10A, new[] { ("MATH", t4), ("PHY", t5), ("CHEM", t5), ("FA_LIT", t6), ("ENG", t6), ("AR", t6), ("REL", t6) }, subjMap);
        await AssignSubjectsAsync(db, cls11A, new[] { ("MATH", t4), ("PHY", t5), ("CHEM", t5), ("BIO", t5), ("FA_LIT", t6), ("ENG", t6) }, subjMap);
        await db.SaveChangesAsync();

        // ====== ۷) برنامه هفتگی ======
        await BuildScheduleAsync(db, cls5A, school1.SchoolId);
        await BuildScheduleAsync(db, cls6A, school1.SchoolId);
        await BuildScheduleAsync(db, cls10A, school2.SchoolId);
        await db.SaveChangesAsync();

        // ====== ۸) دانش‌آموزان + اولیا + ثبت‌نام ======
        var firstNamesM = new[] { "محمد", "علی", "حسین", "امیر", "رضا", "مهدی", "سعید", "حسن", "ارشیا", "پارسا", "آرمین", "آرتین" };
        var firstNamesF = new[] { "فاطمه", "زهرا", "نگار", "سارا", "نازنین", "هستی", "نیلوفر", "مریم", "هدیه", "ملیکا", "آیلین", "هانیه" };
        var lastNames = new[] { "محمدی", "حسینی", "رضایی", "احمدی", "کاظمی", "موسوی", "حیدری", "نظری", "قاسمی", "اسدی", "سلطانی", "بهرامی", "علوی", "صادقی" };

        await SeedStudentsForClassAsync(db, cls5A, 18, firstNamesM, firstNamesF, lastNames, mixGender: true, roles, academicYearId, useMale: true);
        await SeedStudentsForClassAsync(db, cls5B, 16, firstNamesM, firstNamesF, lastNames, mixGender: true, roles, academicYearId, useMale: false);
        await SeedStudentsForClassAsync(db, cls6A, 22, firstNamesM, firstNamesF, lastNames, mixGender: true, roles, academicYearId, useMale: true);
        await SeedStudentsForClassAsync(db, cls10A, 20, firstNamesM, firstNamesF, lastNames, mixGender: false, roles, academicYearId, useMale: false);
        await SeedStudentsForClassAsync(db, cls11A, 19, firstNamesM, firstNamesF, lastNames, mixGender: false, roles, academicYearId, useMale: false);
        await db.SaveChangesAsync();

        // ====== ۹) حضور و غیاب ۱۵ روز اخیر ======
        await SeedAttendanceAsync(db, statuses);

        // ====== ۱۰) آزمون‌ها و نمرات ======
        await SeedExamsAndScoresAsync(db, examTypes, termIds.First());

        // ====== ۱۱) سوابق انضباطی ======
        await SeedDisciplineAsync(db, disciplineTypes, academicYearId);

        // ====== ۱۲) مالی (فاکتور و پرداخت) ======
        await SeedFinanceAsync(db, feeTypes, academicYearId);

        // ====== ۱۳) اعلان‌ها ======
        await SeedAnnouncementsAsync(db, school1, school2, principal1.User!.UserId);

        // ====== ۱۴) تکالیف ======
        await SeedHomeworkAsync(db);

        // ====== ۱۵) پیام‌های نمونه ======
        await SeedMessagesAsync(db, principal1.User.UserId, t1.User!.UserId);

        logger?.LogInformation("✅ Demo data seeded successfully.");
    }

    // ===== Helpers =====
    private record PersonStaffUser(Person Person, Staff? Staff, User? User);

    private static async Task<PersonStaffUser> CreatePersonWithStaffAndUserAsync(SmsDbContext db,
        string firstName, string lastName, string gender, string username, string password,
        string? personnelCode = null, string? mobile = null, string? degree = null, string? fieldOfStudy = null)
    {
        var p = new Person
        {
            FirstName = firstName, LastName = lastName, Gender = gender,
            Mobile = mobile, IsActive = true,
            BirthDate = new DateTime(1980 + _rnd.Next(20), _rnd.Next(1, 12), _rnd.Next(1, 28))
        };
        db.Persons.Add(p);
        await db.SaveChangesAsync();

        var s = new Staff
        {
            PersonId = p.PersonId, PersonnelCode = personnelCode,
            EmploymentType = "رسمی", Degree = degree ?? "کارشناسی",
            FieldOfStudy = fieldOfStudy, HireDate = DateTime.Today.AddYears(-_rnd.Next(1, 15))
        };
        db.Staff.Add(s);

        var u = new User
        {
            PersonId = p.PersonId, Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
        db.Users.Add(u);
        await db.SaveChangesAsync();

        return new PersonStaffUser(p, s, u);
    }

    private static async Task AssignToSchoolAsync(SmsDbContext db, int staffId, int schoolId, int academicYearId,
        string position, decimal? weeklyHours = null)
    {
        db.StaffAssignments.Add(new StaffSchoolAssignment
        {
            StaffId = staffId, SchoolId = schoolId, AcademicYearId = academicYearId,
            Position = position, WeeklyHours = weeklyHours,
            StartDate = DateTime.Today.AddMonths(-6), IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task AssignUserRoleAsync(SmsDbContext db, int userId, int roleId, int? schoolId = null)
    {
        db.UserRoles.Add(new UserRole
        {
            UserId = userId, RoleId = roleId, SchoolId = schoolId,
            StartDate = DateTime.UtcNow, IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task AssignSubjectsAsync(SmsDbContext db, Classroom cls,
        (string code, PersonStaffUser teacher)[] mapping, Dictionary<string, Subject> subjMap)
    {
        foreach (var (code, teacher) in mapping)
        {
            if (!subjMap.ContainsKey(code)) continue;
            var gs = await db.GradeSubjects
                .FirstOrDefaultAsync(g => g.GradeId == cls.GradeId && g.SubjectId == subjMap[code].SubjectId);
            if (gs is null) continue;
            db.ClassSubjects.Add(new ClassSubjectTeacher
            {
                ClassroomId = cls.ClassroomId,
                GradeSubjectId = gs.GradeSubjectId,
                StaffId = teacher.Staff!.StaffId,
                StartDate = DateTime.Today.AddMonths(-6),
                IsActive = true
            });
        }
    }

    private static async Task BuildScheduleAsync(SmsDbContext db, Classroom cls, int schoolId)
    {
        var periods = await db.SchoolPeriods.Where(p => p.SchoolId == schoolId && !p.IsBreak).OrderBy(p => p.PeriodNo).ToListAsync();
        var classSubjects = await db.ClassSubjects.Where(cs => cs.ClassroomId == cls.ClassroomId && cs.IsActive).ToListAsync();
        if (!classSubjects.Any() || !periods.Any()) return;

        // ۵ روز (شنبه تا چهارشنبه) × زنگ‌های دروس
        for (byte day = 0; day < 5; day++)
        {
            for (int i = 0; i < periods.Count && i < classSubjects.Count; i++)
            {
                var cs = classSubjects[(day + i) % classSubjects.Count];
                db.ClassSchedules.Add(new ClassSchedule
                {
                    ClassroomId = cls.ClassroomId,
                    ClassSubjectId = cs.ClassSubjectId,
                    PeriodId = periods[i].PeriodId,
                    DayOfWeek = day,
                    RoomNumber = cls.RoomNumber,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedStudentsForClassAsync(SmsDbContext db, Classroom cls, int count,
        string[] firstM, string[] firstF, string[] lasts, bool mixGender,
        Dictionary<string, int> roles, int academicYearId, bool useMale)
    {
        for (int i = 0; i < count; i++)
        {
            var gender = mixGender ? (i % 2 == 0 ? "M" : "F") : (useMale ? "M" : "F");
            var first = gender == "M" ? firstM[_rnd.Next(firstM.Length)] : firstF[_rnd.Next(firstF.Length)];
            var last = lasts[_rnd.Next(lasts.Length)];

            // والد
            var parentFirstName = gender == "M" ? "محمد" : "علی";  // پدر
            var parent = new Person
            {
                FirstName = parentFirstName + _rnd.Next(100), LastName = last, Gender = "M",
                Mobile = "0912" + _rnd.Next(1000000, 9999999).ToString(),
                IsActive = true
            };
            db.Persons.Add(parent);
            await db.SaveChangesAsync();

            var guardian = new Guardian { PersonId = parent.PersonId, Occupation = "آزاد" };
            db.Guardians.Add(guardian);
            var parentUser = new User
            {
                PersonId = parent.PersonId,
                Username = $"parent_{cls.ClassroomId}_{i + 1}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123")
            };
            db.Users.Add(parentUser);
            await db.SaveChangesAsync();
            db.UserRoles.Add(new UserRole { UserId = parentUser.UserId, RoleId = roles["Parent"], IsActive = true });

            // دانش‌آموز
            var studentPerson = new Person
            {
                FirstName = first, LastName = last, FatherName = parent.FirstName,
                Gender = gender, BirthDate = DateTime.Today.AddYears(-(10 + _rnd.Next(0, 8))),
                Mobile = i < 3 ? "0913" + _rnd.Next(1000000, 9999999).ToString() : null,
                IsActive = true
            };
            db.Persons.Add(studentPerson);
            await db.SaveChangesAsync();

            var student = new Student
            {
                PersonId = studentPerson.PersonId,
                StudentCode = $"{cls.AcademicYearId}{cls.ClassroomId:D3}{(i + 1):D2}",
                EnrollmentDate = DateTime.Today.AddMonths(-6),
                IsActive = true
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            // اتصال ولی به دانش‌آموز
            db.StudentGuardians.Add(new StudentGuardian
            {
                StudentId = student.StudentId, GuardianId = guardian.GuardianId,
                Relationship = "پدر", IsPrimary = true, HasCustody = true, CanPickup = true
            });

            // اکانت دانش‌آموز (فقط برای 5 نفر اول هر کلاس)
            if (i < 5)
            {
                var stdUser = new User
                {
                    PersonId = studentPerson.PersonId,
                    Username = $"student_{cls.ClassroomId}_{i + 1}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123")
                };
                db.Users.Add(stdUser);
                await db.SaveChangesAsync();
                db.UserRoles.Add(new UserRole { UserId = stdUser.UserId, RoleId = roles["Student"], IsActive = true });
            }

            // ثبت‌نام در کلاس
            db.Enrollments.Add(new StudentEnrollment
            {
                StudentId = student.StudentId,
                ClassroomId = cls.ClassroomId,
                AcademicYearId = academicYearId,
                EnrollmentDate = DateTime.Today.AddMonths(-6),
                Status = "فعال"
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedAttendanceAsync(SmsDbContext db, List<AttendanceStatus> statuses)
    {
        var present = statuses.First(s => s.Code == "PRESENT");
        var absent = statuses.First(s => s.Code == "ABSENT");
        var excused = statuses.First(s => s.Code == "EXCUSED");
        var tardy = statuses.First(s => s.Code == "TARDY");

        var classrooms = await db.Classrooms.ToListAsync();
        foreach (var cls in classrooms)
        {
            var students = await db.Enrollments.Where(e => e.ClassroomId == cls.ClassroomId && e.Status == "فعال")
                .Select(e => e.StudentId).ToListAsync();
            if (!students.Any()) continue;

            var classSubject = await db.ClassSubjects.Where(c => c.ClassroomId == cls.ClassroomId).Select(c => c.ClassSubjectId).FirstOrDefaultAsync();
            var teacherStaffId = await db.ClassSubjects.Where(c => c.ClassroomId == cls.ClassroomId).Select(c => c.StaffId).FirstOrDefaultAsync();
            if (teacherStaffId == 0) continue;

            // ۱۵ روز کاری اخیر
            for (int d = 1; d <= 15; d++)
            {
                var date = DateTime.Today.AddDays(-d);
                if (date.DayOfWeek == DayOfWeek.Friday) continue;

                foreach (var sid in students)
                {
                    var rand = _rnd.Next(100);
                    byte statusId = rand switch
                    {
                        < 80 => present.StatusId,    // ۸۰٪ حاضر
                        < 88 => tardy.StatusId,      // ۸٪ تاخیر
                        < 95 => absent.StatusId,     // ۷٪ غایب غیرموجه
                        _ => excused.StatusId         // ۵٪ غایب موجه
                    };
                    db.Attendances.Add(new Attendance
                    {
                        StudentId = sid, ClassroomId = cls.ClassroomId,
                        ClassSubjectId = classSubject == 0 ? null : classSubject,
                        AttendanceDate = date, StatusId = statusId,
                        TardyMinutes = statusId == tardy.StatusId ? (short)_rnd.Next(5, 30) : null,
                        RecordedByStaffId = teacherStaffId
                    });
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedExamsAndScoresAsync(SmsDbContext db, List<ExamType> examTypes, int termId)
    {
        var quizType = examTypes.First(e => e.Code == "QUIZ");
        var midType = examTypes.First(e => e.Code == "MID");

        var allClassSubjects = await db.ClassSubjects.Include(cs => cs.GradeSubject)
            .Where(cs => cs.IsActive).ToListAsync();

        foreach (var cs in allClassSubjects)
        {
            var students = await db.Enrollments.Where(e => e.ClassroomId == cs.ClassroomId && e.Status == "فعال")
                .Select(e => e.StudentId).ToListAsync();
            if (!students.Any()) continue;

            // ۲ آزمون برای هر درس کلاسی
            var isDescriptive = cs.GradeSubject.IsDescriptive;
            int? scaleId = null;
            if (isDescriptive)
                scaleId = await db.GradeScales.Where(g => g.IsDescriptive).Select(g => (int?)g.GradeScaleId).FirstOrDefaultAsync();
            var scaleItems = scaleId.HasValue
                ? await db.GradeScaleItems.Where(i => i.GradeScaleId == scaleId).OrderBy(i => i.OrderNo).ToListAsync()
                : new List<GradeScaleItem>();

            var exams = new[]
            {
                new Exam { Title = "آزمون مستمر", ClassSubjectId = cs.ClassSubjectId, ExamTypeId = quizType.ExamTypeId, TermId = termId,
                    ExamDate = DateTime.Today.AddDays(-20), MaxScore = 20, Weight = quizType.DefaultWeight, IsDescriptive = isDescriptive,
                    GradeScaleId = scaleId, IsFinalized = true, CreatedByStaffId = cs.StaffId },
                new Exam { Title = "آزمون میان‌ترم", ClassSubjectId = cs.ClassSubjectId, ExamTypeId = midType.ExamTypeId, TermId = termId,
                    ExamDate = DateTime.Today.AddDays(-5), MaxScore = 20, Weight = midType.DefaultWeight, IsDescriptive = isDescriptive,
                    GradeScaleId = scaleId, IsFinalized = false, CreatedByStaffId = cs.StaffId }
            };
            db.Exams.AddRange(exams);
            await db.SaveChangesAsync();

            foreach (var ex in exams)
            {
                foreach (var sid in students)
                {
                    if (_rnd.Next(100) < 5) continue; // ۵٪ غایب

                    decimal? numeric = null;
                    int? descId = null;
                    if (isDescriptive && scaleItems.Any())
                        descId = scaleItems[_rnd.Next(scaleItems.Count)].GradeScaleItemId;
                    else
                        numeric = Math.Round((decimal)(8 + _rnd.NextDouble() * 12), 2);

                    db.ExamScores.Add(new ExamScore
                    {
                        ExamId = ex.ExamId, StudentId = sid,
                        NumericScore = numeric, DescriptiveScaleItemId = descId,
                        EnteredByStaffId = cs.StaffId, EnteredAt = DateTime.UtcNow
                    });
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedDisciplineAsync(SmsDbContext db, List<DisciplinaryType> types, int academicYearId)
    {
        var rewards = types.Where(t => t.Category == "R").ToList();
        var punishments = types.Where(t => t.Category == "P").ToList();
        var classrooms = await db.Classrooms.ToListAsync();

        foreach (var cls in classrooms)
        {
            var students = await db.Enrollments.Where(e => e.ClassroomId == cls.ClassroomId).Select(e => e.StudentId).ToListAsync();
            var teacherStaffId = await db.ClassSubjects.Where(c => c.ClassroomId == cls.ClassroomId).Select(c => c.StaffId).FirstOrDefaultAsync();
            if (teacherStaffId == 0 || !students.Any()) continue;

            for (int i = 0; i < Math.Min(students.Count / 3, 8); i++)
            {
                var sid = students[_rnd.Next(students.Count)];
                var isReward = _rnd.Next(100) < 60;
                var type = isReward ? rewards[_rnd.Next(rewards.Count)] : punishments[_rnd.Next(punishments.Count)];
                db.DisciplinaryRecords.Add(new DisciplinaryRecord
                {
                    StudentId = sid, ClassroomId = cls.ClassroomId,
                    AcademicYearId = academicYearId, TypeId = type.TypeId,
                    RecordDate = DateTime.Today.AddDays(-_rnd.Next(1, 60)),
                    Description = isReward ? "عملکرد مطلوب در کلاس" : "بی‌نظمی در کلاس",
                    ScoreImpact = type.DefaultScoreImpact,
                    RecordedByStaffId = teacherStaffId
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedFinanceAsync(SmsDbContext db, List<FeeType> feeTypes, int academicYearId)
    {
        var ftFixed = feeTypes.First(f => f.Name == "شهریه ثابت");
        var ftMonthly = feeTypes.First(f => f.Name == "شهریه ماهانه");
        var enrollments = await db.Enrollments
            .Include(e => e.Classroom)
            .Where(e => e.AcademicYearId == academicYearId && e.Status == "فعال").ToListAsync();

        int n = 1;
        foreach (var e in enrollments)
        {
            // شهریه ثابت
            var inv1 = new StudentInvoice
            {
                StudentId = e.StudentId, AcademicYearId = academicYearId, SchoolId = e.Classroom.SchoolId,
                FeeTypeId = ftFixed.FeeTypeId,
                InvoiceNumber = $"INV-{academicYearId}-{n:D5}",
                Amount = 50_000_000, Discount = 0, NetAmount = 50_000_000,
                DueDate = DateTime.Today.AddDays(-15), Status = "صادرشده",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
            n++;
            db.StudentInvoices.Add(inv1);
            await db.SaveChangesAsync();

            // ۷۰٪ پرداخت کرده‌اند
            if (_rnd.Next(100) < 70)
            {
                db.Payments.Add(new Payment
                {
                    InvoiceId = inv1.InvoiceId, PaymentDate = DateTime.Today.AddDays(-_rnd.Next(1, 25)),
                    Amount = inv1.NetAmount, PaymentMethod = _rnd.Next(2) == 0 ? "کارت" : "آنلاین",
                    ReferenceNumber = "REF" + _rnd.Next(100000, 999999)
                });
                inv1.Status = "پرداخت‌شده";
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedAnnouncementsAsync(SmsDbContext db, School s1, School s2, int adminUserId)
    {
        db.Announcements.AddRange(
            new Announcement
            {
                SchoolId = null, Title = "آغاز سال تحصیلی جدید",
                Body = "به اطلاع کلیه عزیزان می‌رسانیم سال تحصیلی جدید آغاز شد. حضور همه دانش‌آموزان الزامی است.",
                TargetAudience = "All", PublishDate = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddDays(30), CreatedByUserId = adminUserId, IsActive = true
            },
            new Announcement
            {
                SchoolId = s1.SchoolId, Title = "جلسه اولیا و مربیان",
                Body = "جلسه اولیا و مربیان روز پنج‌شنبه ساعت ۱۶ در سالن اجتماعات برگزار می‌شود.",
                TargetAudience = "Parents", PublishDate = DateTime.UtcNow.AddDays(-3),
                ExpiryDate = DateTime.UtcNow.AddDays(7), CreatedByUserId = adminUserId, IsActive = true
            },
            new Announcement
            {
                SchoolId = s2.SchoolId, Title = "اعلام برنامه امتحانات نوبت اول",
                Body = "برنامه امتحانات نوبت اول از تاریخ ۱۵ دی ماه آغاز خواهد شد. لطفاً به برنامه نصب شده در تابلوی اعلانات مراجعه کنید.",
                TargetAudience = "Students", PublishDate = DateTime.UtcNow.AddDays(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(20), CreatedByUserId = adminUserId, IsActive = true
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedHomeworkAsync(SmsDbContext db)
    {
        var classSubjects = await db.ClassSubjects.Where(cs => cs.IsActive).Take(5).ToListAsync();
        foreach (var cs in classSubjects)
        {
            var hw = new Homework
            {
                ClassSubjectId = cs.ClassSubjectId, CreatedByStaffId = cs.StaffId,
                Title = "تکلیف هفتگی - تمرینات صفحه ۲۵",
                Description = "لطفاً تمرینات ۱ تا ۱۰ صفحه ۲۵ کتاب درسی را در دفتر تمرین حل کرده و در جلسه بعد ارائه دهید.",
                AssignedDate = DateTime.Today.AddDays(-5),
                DueDate = DateTime.Today.AddDays(3),
                MaxScore = 10, AllowFileSubmission = true, IsActive = true
            };
            db.Homeworks.Add(hw);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedMessagesAsync(SmsDbContext db, int principalUserId, int teacherUserId)
    {
        db.Messages.AddRange(
            new Message
            {
                FromUserId = principalUserId, ToUserId = teacherUserId,
                Subject = "خوش آمدید", Body = "به خانواده آموزشی ما خوش آمدید. موفق باشید.",
                SentAt = DateTime.UtcNow.AddDays(-7), Category = "Notice"
            },
            new Message
            {
                FromUserId = teacherUserId, ToUserId = principalUserId,
                Subject = "درخواست ملاقات", Body = "سلام. لطفاً برای بحث در مورد برنامه کلاسی، زمانی برای ملاقات تعیین فرمایید.",
                SentAt = DateTime.UtcNow.AddDays(-5), Category = "Request"
            }
        );
        await db.SaveChangesAsync();
    }
}

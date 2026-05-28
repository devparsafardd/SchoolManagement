using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Identity;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class BulkImportService : IBulkImportService
{
    private readonly SmsDbContext _db;
    private readonly IPasswordHasher _hasher;

    public BulkImportService(SmsDbContext db, IPasswordHasher hasher)
    {
        _db = db; _hasher = hasher;
    }

    // ===== قالب دانش‌آموزان =====
    public byte[] GenerateStudentsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("دانش‌آموزان");
        ws.RightToLeft = true;

        var headers = new[]
        {
            "نام*", "نام خانوادگی*", "نام پدر", "کد ملی", "جنسیت* (M/F)",
            "تاریخ تولد (1390/05/15)", "موبایل", "آدرس",
            "نام ولی*", "نام خانوادگی ولی*", "موبایل ولی*", "نسبت (پدر/مادر)",
            "نام کاربری دانش‌آموز", "رمز عبور دانش‌آموز",
            "نام کاربری ولی", "رمز عبور ولی"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
            cell.Style.Font.FontColor = XLColor.White;
        }
        ws.Row(1).Height = 30;

        // یک ردیف نمونه
        ws.Cell(2, 1).Value = "علی";
        ws.Cell(2, 2).Value = "محمدی";
        ws.Cell(2, 3).Value = "حسن";
        ws.Cell(2, 4).Value = "0011223344";
        ws.Cell(2, 5).Value = "M";
        ws.Cell(2, 6).Value = "1390/05/15";
        ws.Cell(2, 7).Value = "09121234567";
        ws.Cell(2, 8).Value = "تهران، خیابان ...";
        ws.Cell(2, 9).Value = "حسن";
        ws.Cell(2, 10).Value = "محمدی";
        ws.Cell(2, 11).Value = "09127654321";
        ws.Cell(2, 12).Value = "پدر";
        ws.Cell(2, 13).Value = "ali.mohammadi";
        ws.Cell(2, 14).Value = "Pass@123";
        ws.Cell(2, 15).Value = "h.mohammadi";
        ws.Cell(2, 16).Value = "Pass@123";

        // راهنما در شیت دوم
        var help = wb.Worksheets.Add("راهنما");
        help.RightToLeft = true;
        help.Cell(1, 1).Value = "نکات مهم برای پر کردن:";
        help.Cell(1, 1).Style.Font.Bold = true;
        help.Cell(2, 1).Value = "۱) فیلدهای ستاره‌دار (*) اجباری هستند";
        help.Cell(3, 1).Value = "۲) جنسیت: فقط M (پسر) یا F (دختر)";
        help.Cell(4, 1).Value = "۳) تاریخ تولد به فرمت شمسی: 1390/05/15";
        help.Cell(5, 1).Value = "۴) موبایل ۱۱ رقمی (مثلاً 09121234567)";
        help.Cell(6, 1).Value = "۵) اگر نام کاربری/رمز خالی بود، اکانت ساخته نمی‌شود";
        help.Cell(7, 1).Value = "۶) نسبت ولی: پدر / مادر / سرپرست";

        ws.Columns().AdjustToContents();
        help.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ===== ایمپورت دانش‌آموزان =====
    public async Task<BulkImportResult> ImportStudentsAsync(Stream excelStream, int classroomId)
    {
        var result = new BulkImportResult();

        var classroom = await _db.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.ClassroomId == classroomId);
        if (classroom is null)
        {
            result.Errors.Add(new BulkImportError { Message = "کلاس یافت نشد" });
            return result;
        }

        var studentRoleId = await _db.Roles.Where(r => r.Name == "Student").Select(r => r.RoleId).FirstAsync();
        var parentRoleId = await _db.Roles.Where(r => r.Name == "Parent").Select(r => r.RoleId).FirstAsync();

        using var wb = new XLWorkbook(excelStream);
        var ws = wb.Worksheets.First();

        int row = 2;
        while (!ws.Row(row).Cell(1).IsEmpty())
        {
            result.TotalRows++;
            try
            {
                string firstName = ws.Cell(row, 1).GetString().Trim();
                string lastName = ws.Cell(row, 2).GetString().Trim();
                string? fatherName = ws.Cell(row, 3).GetString().Trim();
                string? nationalCode = ws.Cell(row, 4).GetString().Trim();
                string gender = ws.Cell(row, 5).GetString().Trim().ToUpperInvariant();
                string? birthDateStr = ws.Cell(row, 6).GetString().Trim();
                string? mobile = ws.Cell(row, 7).GetString().Trim();
                string? address = ws.Cell(row, 8).GetString().Trim();
                string parentFirst = ws.Cell(row, 9).GetString().Trim();
                string parentLast = ws.Cell(row, 10).GetString().Trim();
                string parentMobile = ws.Cell(row, 11).GetString().Trim();
                string? rel = ws.Cell(row, 12).GetString().Trim();
                string? studentUsername = ws.Cell(row, 13).GetString().Trim();
                string? studentPassword = ws.Cell(row, 14).GetString().Trim();
                string? parentUsername = ws.Cell(row, 15).GetString().Trim();
                string? parentPassword = ws.Cell(row, 16).GetString().Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    result.Errors.Add(new BulkImportError { RowNumber = row, Field = "نام", Message = "نام و نام خانوادگی اجباری است" });
                    result.FailedCount++; row++; continue;
                }
                if (gender != "M" && gender != "F")
                {
                    result.Errors.Add(new BulkImportError { RowNumber = row, Field = "جنسیت", Message = "جنسیت باید M یا F باشد" });
                    result.FailedCount++; row++; continue;
                }
                if (string.IsNullOrWhiteSpace(parentFirst) || string.IsNullOrWhiteSpace(parentLast) || string.IsNullOrWhiteSpace(parentMobile))
                {
                    result.Errors.Add(new BulkImportError { RowNumber = row, Field = "ولی", Message = "اطلاعات ولی اجباری است" });
                    result.FailedCount++; row++; continue;
                }

                DateTime? birthDate = null;
                if (!string.IsNullOrWhiteSpace(birthDateStr))
                    birthDate = SMS.Shared.Helpers.PersianDate.FromPersian(birthDateStr);

                // ساخت ولی
                var parentPerson = new Person
                {
                    FirstName = parentFirst, LastName = parentLast, Gender = "M",
                    Mobile = parentMobile, IsActive = true
                };
                _db.Persons.Add(parentPerson);
                await _db.SaveChangesAsync();

                var guardian = new Guardian { PersonId = parentPerson.PersonId, Occupation = "آزاد" };
                _db.Guardians.Add(guardian);
                await _db.SaveChangesAsync();

                // اکانت ولی (اگر نام کاربری داده شده)
                if (!string.IsNullOrWhiteSpace(parentUsername) && !string.IsNullOrWhiteSpace(parentPassword))
                {
                    var exists = await _db.Users.AnyAsync(u => u.Username == parentUsername);
                    if (!exists)
                    {
                        var pu = new User
                        {
                            PersonId = parentPerson.PersonId,
                            Username = parentUsername,
                            PasswordHash = _hasher.Hash(parentPassword)
                        };
                        _db.Users.Add(pu);
                        await _db.SaveChangesAsync();
                        _db.UserRoles.Add(new UserRole { UserId = pu.UserId, RoleId = parentRoleId, IsActive = true });
                    }
                }

                // ساخت دانش‌آموز
                var studentPerson = new Person
                {
                    FirstName = firstName, LastName = lastName, FatherName = fatherName ?? parentFirst,
                    NationalCode = string.IsNullOrEmpty(nationalCode) ? null : nationalCode,
                    Gender = gender, BirthDate = birthDate, Mobile = mobile, Address = address,
                    IsActive = true
                };
                _db.Persons.Add(studentPerson);
                await _db.SaveChangesAsync();

                var studentCode = $"{classroom.AcademicYearId}{classroom.ClassroomId:D3}{(row - 1):D2}";
                var student = new Student
                {
                    PersonId = studentPerson.PersonId,
                    StudentCode = studentCode,
                    EnrollmentDate = DateTime.Today,
                    IsActive = true
                };
                _db.Students.Add(student);
                await _db.SaveChangesAsync();

                // اتصال ولی به دانش‌آموز
                _db.StudentGuardians.Add(new StudentGuardian
                {
                    StudentId = student.StudentId, GuardianId = guardian.GuardianId,
                    Relationship = string.IsNullOrWhiteSpace(rel) ? "پدر" : rel,
                    IsPrimary = true, HasCustody = true, CanPickup = true
                });

                // اکانت دانش‌آموز
                if (!string.IsNullOrWhiteSpace(studentUsername) && !string.IsNullOrWhiteSpace(studentPassword))
                {
                    var exists = await _db.Users.AnyAsync(u => u.Username == studentUsername);
                    if (!exists)
                    {
                        var su = new User
                        {
                            PersonId = studentPerson.PersonId,
                            Username = studentUsername,
                            PasswordHash = _hasher.Hash(studentPassword)
                        };
                        _db.Users.Add(su);
                        await _db.SaveChangesAsync();
                        _db.UserRoles.Add(new UserRole { UserId = su.UserId, RoleId = studentRoleId, IsActive = true });
                    }
                }

                // ثبت‌نام در کلاس
                _db.Enrollments.Add(new StudentEnrollment
                {
                    StudentId = student.StudentId,
                    ClassroomId = classroomId,
                    AcademicYearId = classroom.AcademicYearId,
                    EnrollmentDate = DateTime.Today,
                    Status = "فعال"
                });
                await _db.SaveChangesAsync();

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkImportError { RowNumber = row, Message = ex.Message });
                result.FailedCount++;
            }
            row++;
        }
        return result;
    }

    // ===== قالب معلمان =====
    public byte[] GenerateStaffTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("معلمان");
        ws.RightToLeft = true;

        var headers = new[]
        {
            "نام*", "نام خانوادگی*", "نام پدر", "کد ملی", "جنسیت* (M/F)",
            "تاریخ تولد", "موبایل", "ایمیل", "کد پرسنلی",
            "مدرک تحصیلی", "رشته تحصیلی", "تاریخ استخدام", "شماره شبا",
            "سمت* (Teacher/VicePrincipal/Counselor)", "ساعت هفتگی",
            "نام کاربری*", "رمز عبور*"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
            cell.Style.Font.FontColor = XLColor.White;
        }
        // نمونه
        ws.Cell(2, 1).Value = "علی";
        ws.Cell(2, 2).Value = "رضایی";
        ws.Cell(2, 5).Value = "M";
        ws.Cell(2, 7).Value = "09121111111";
        ws.Cell(2, 9).Value = "T-001";
        ws.Cell(2, 10).Value = "کارشناسی";
        ws.Cell(2, 11).Value = "ریاضی";
        ws.Cell(2, 14).Value = "Teacher";
        ws.Cell(2, 15).Value = "18";
        ws.Cell(2, 16).Value = "ali.rezaei";
        ws.Cell(2, 17).Value = "Pass@123";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ===== ایمپورت معلمان =====
    public async Task<BulkImportResult> ImportStaffAsync(Stream excelStream, int schoolId)
    {
        var result = new BulkImportResult();
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear is null)
        {
            result.Errors.Add(new BulkImportError { Message = "سال تحصیلی فعال یافت نشد" });
            return result;
        }

        var teacherRoleId = await _db.Roles.Where(r => r.Name == "Teacher").Select(r => r.RoleId).FirstAsync();

        using var wb = new XLWorkbook(excelStream);
        var ws = wb.Worksheets.First();

        int row = 2;
        while (!ws.Row(row).Cell(1).IsEmpty())
        {
            result.TotalRows++;
            try
            {
                string firstName = ws.Cell(row, 1).GetString().Trim();
                string lastName = ws.Cell(row, 2).GetString().Trim();
                string gender = ws.Cell(row, 5).GetString().Trim().ToUpperInvariant();
                string? mobile = ws.Cell(row, 7).GetString().Trim();
                string? personnelCode = ws.Cell(row, 9).GetString().Trim();
                string? degree = ws.Cell(row, 10).GetString().Trim();
                string? field = ws.Cell(row, 11).GetString().Trim();
                string position = ws.Cell(row, 14).GetString().Trim();
                if (string.IsNullOrWhiteSpace(position)) position = "Teacher";
                string? weeklyHours = ws.Cell(row, 15).GetString().Trim();
                string username = ws.Cell(row, 16).GetString().Trim();
                string password = ws.Cell(row, 17).GetString().Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    result.Errors.Add(new BulkImportError { RowNumber = row, Message = "نام و نام خانوادگی اجباری است" });
                    result.FailedCount++; row++; continue;
                }
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    result.Errors.Add(new BulkImportError { RowNumber = row, Message = "نام کاربری و رمز اجباری است" });
                    result.FailedCount++; row++; continue;
                }
                if (gender != "M" && gender != "F") gender = "M";

                if (await _db.Users.AnyAsync(u => u.Username == username))
                {
                    result.Errors.Add(new BulkImportError { RowNumber = row, Message = $"نام کاربری {username} تکراری است" });
                    result.FailedCount++; row++; continue;
                }

                var person = new Person { FirstName = firstName, LastName = lastName, Gender = gender, Mobile = mobile, IsActive = true };
                _db.Persons.Add(person); await _db.SaveChangesAsync();

                var staff = new Staff
                {
                    PersonId = person.PersonId, PersonnelCode = personnelCode,
                    Degree = degree, FieldOfStudy = field,
                    EmploymentType = "رسمی", IsActive = true, HireDate = DateTime.Today
                };
                _db.Staff.Add(staff); await _db.SaveChangesAsync();

                decimal? hours = null;
                if (decimal.TryParse(weeklyHours, out var h)) hours = h;
                _db.StaffAssignments.Add(new StaffSchoolAssignment
                {
                    StaffId = staff.StaffId, SchoolId = schoolId,
                    AcademicYearId = activeYear.AcademicYearId,
                    Position = position, WeeklyHours = hours,
                    StartDate = DateTime.Today, IsActive = true
                });

                var user = new User { PersonId = person.PersonId, Username = username, PasswordHash = _hasher.Hash(password) };
                _db.Users.Add(user); await _db.SaveChangesAsync();
                _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = teacherRoleId, SchoolId = schoolId, IsActive = true });
                await _db.SaveChangesAsync();

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkImportError { RowNumber = row, Message = ex.Message });
                result.FailedCount++;
            }
            row++;
        }
        return result;
    }
}

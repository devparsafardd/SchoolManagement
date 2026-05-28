using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly SmsDbContext _db;
    public StudentService(SmsDbContext db) => _db = db;

    public async Task<PagedResult<StudentDto>> GetPagedAsync(int? schoolId, int? classroomId, string? search, int page = 1, int pageSize = 20)
    {
        var q = _db.Students
            .Include(s => s.Person)
            .Include(s => s.Enrollments).ThenInclude(e => e.Classroom).ThenInclude(c => c.School)
            .AsNoTracking().AsQueryable();

        if (classroomId.HasValue)
            q = q.Where(s => s.Enrollments.Any(e => e.ClassroomId == classroomId && e.Status == "فعال"));
        else if (schoolId.HasValue)
            q = q.Where(s => s.Enrollments.Any(e => e.Classroom.SchoolId == schoolId && e.Status == "فعال"));

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s =>
                s.Person.FirstName.Contains(search) ||
                s.Person.LastName.Contains(search) ||
                s.StudentCode.Contains(search) ||
                (s.Person.NationalCode != null && s.Person.NationalCode.Contains(search)));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(s => s.Person.LastName).ThenBy(s => s.Person.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new StudentDto
            {
                StudentId = s.StudentId, PersonId = s.PersonId, StudentCode = s.StudentCode,
                FirstName = s.Person.FirstName, LastName = s.Person.LastName,
                FatherName = s.Person.FatherName, NationalCode = s.Person.NationalCode,
                Gender = s.Person.Gender, BirthDate = s.Person.BirthDate,
                Mobile = s.Person.Mobile, Address = s.Person.Address,
                BloodType = s.BloodType, IsActive = s.IsActive,
                CurrentClassName = s.Enrollments.Where(e => e.Status == "فعال")
                    .OrderByDescending(e => e.EnrollmentDate).Select(e => e.Classroom.Name).FirstOrDefault(),
                CurrentSchoolName = s.Enrollments.Where(e => e.Status == "فعال")
                    .OrderByDescending(e => e.EnrollmentDate).Select(e => e.Classroom.School.Name).FirstOrDefault(),
                UserId = s.Person.User != null ? s.Person.User.UserId : null,
                Username = s.Person.User != null ? s.Person.User.Username : null,
                IsUserLocked = s.Person.User != null ? s.Person.User.IsLocked : false
            }).ToListAsync();

        return new PagedResult<StudentDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result<StudentDto>> GetByIdAsync(int id)
    {
        var s = await _db.Students
            .Include(x => x.Person).ThenInclude(p => p.User)
            .Include(x => x.Enrollments).ThenInclude(e => e.Classroom).ThenInclude(c => c.School)
            .AsNoTracking().FirstOrDefaultAsync(x => x.StudentId == id);

        if (s is null) return Result<StudentDto>.Fail("دانش‌آموز یافت نشد");

        return Result<StudentDto>.Ok(new StudentDto
        {
            StudentId = s.StudentId, PersonId = s.PersonId, StudentCode = s.StudentCode,
            FirstName = s.Person.FirstName, LastName = s.Person.LastName,
            FatherName = s.Person.FatherName, NationalCode = s.Person.NationalCode,
            Gender = s.Person.Gender, BirthDate = s.Person.BirthDate,
            Mobile = s.Person.Mobile, Address = s.Person.Address,
            BloodType = s.BloodType, IsActive = s.IsActive,
            CurrentClassName = s.Enrollments.Where(e => e.Status == "فعال")
                .OrderByDescending(e => e.EnrollmentDate).Select(e => e.Classroom.Name).FirstOrDefault(),
            CurrentSchoolName = s.Enrollments.Where(e => e.Status == "فعال")
                .OrderByDescending(e => e.EnrollmentDate).Select(e => e.Classroom.School.Name).FirstOrDefault(),
            UserId = s.Person.User?.UserId,
            Username = s.Person.User?.Username,
            IsUserLocked = s.Person.User?.IsLocked ?? false
        });
    }

    public async Task<Result<int>> CreateAsync(StudentCreateDto dto)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (await _db.Students.AnyAsync(s => s.StudentCode == dto.StudentCode))
                return Result<int>.Fail("شماره دانش‌آموزی تکراری است");

            if (!string.IsNullOrEmpty(dto.NationalCode) &&
                await _db.Persons.AnyAsync(p => p.NationalCode == dto.NationalCode))
                return Result<int>.Fail("کد ملی قبلاً ثبت شده است");

            var person = new Person
            {
                FirstName = dto.FirstName, LastName = dto.LastName, FatherName = dto.FatherName,
                NationalCode = dto.NationalCode, Gender = dto.Gender, BirthDate = dto.BirthDate,
                Mobile = dto.Mobile, Address = dto.Address
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var student = new Student
            {
                PersonId = person.PersonId, StudentCode = dto.StudentCode,
                BloodType = dto.BloodType, SpecialNeeds = dto.SpecialNeeds,
                EnrollmentDate = DateTime.UtcNow
            };
            _db.Students.Add(student);
            await _db.SaveChangesAsync();

            if (dto.ClassroomId.HasValue)
            {
                var classroom = await _db.Classrooms.FindAsync(dto.ClassroomId.Value);
                if (classroom is not null)
                {
                    _db.Enrollments.Add(new StudentEnrollment
                    {
                        StudentId = student.StudentId, ClassroomId = classroom.ClassroomId,
                        AcademicYearId = classroom.AcademicYearId,
                        EnrollmentDate = DateTime.UtcNow, Status = "فعال"
                    });
                    await _db.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return Result<int>.Ok(student.StudentId, "دانش‌آموز با موفقیت ثبت شد");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result<int>.Fail($"خطا در ثبت: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(StudentUpdateDto dto)
    {
        var student = await _db.Students.Include(s => s.Person).FirstOrDefaultAsync(s => s.StudentId == dto.StudentId);
        if (student is null) return Result.Fail("دانش‌آموز یافت نشد");

        student.Person.FirstName = dto.FirstName;
        student.Person.LastName = dto.LastName;
        student.Person.FatherName = dto.FatherName;
        student.Person.NationalCode = dto.NationalCode;
        student.Person.Gender = dto.Gender;
        student.Person.BirthDate = dto.BirthDate;
        student.Person.Mobile = dto.Mobile;
        student.Person.Address = dto.Address;
        student.Person.ModifiedAt = DateTime.UtcNow;

        student.StudentCode = dto.StudentCode;
        student.BloodType = dto.BloodType;
        student.SpecialNeeds = dto.SpecialNeeds;
        student.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Result.Ok("اطلاعات دانش‌آموز ویرایش شد");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return Result.Fail("دانش‌آموز یافت نشد");
        student.IsActive = false;
        await _db.SaveChangesAsync();
        return Result.Ok("دانش‌آموز غیرفعال شد");
    }

    public async Task<Result> EnrollAsync(int studentId, int classroomId)
    {
        var classroom = await _db.Classrooms.FindAsync(classroomId);
        if (classroom is null) return Result.Fail("کلاس یافت نشد");

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.AcademicYearId == classroom.AcademicYearId);

        if (existing is not null && existing.Status == "فعال")
            return Result.Fail("دانش‌آموز در این سال تحصیلی قبلاً ثبت‌نام شده است");

        if (classroom.Capacity.HasValue)
        {
            var current = await _db.Enrollments.CountAsync(e => e.ClassroomId == classroomId && e.Status == "فعال");
            if (current >= classroom.Capacity.Value)
                return Result.Fail("ظرفیت این کلاس تکمیل است");
        }

        _db.Enrollments.Add(new StudentEnrollment
        {
            StudentId = studentId, ClassroomId = classroomId,
            AcademicYearId = classroom.AcademicYearId,
            EnrollmentDate = DateTime.UtcNow, Status = "فعال"
        });
        await _db.SaveChangesAsync();
        return Result.Ok("دانش‌آموز در کلاس ثبت‌نام شد");
    }
}

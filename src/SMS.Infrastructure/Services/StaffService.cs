using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Identity;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly SmsDbContext _db;
    private readonly IPasswordHasher _hasher;

    public StaffService(SmsDbContext db, IPasswordHasher hasher)
    {
        _db = db; _hasher = hasher;
    }

    public async Task<PagedResult<StaffDto>> GetPagedAsync(int? schoolId, string? position, string? search, int page = 1, int pageSize = 20)
    {
        var q = _db.Staff
            .Include(s => s.Person)
            .Include(s => s.Assignments).ThenInclude(a => a.School)
            .AsNoTracking()
            .AsQueryable();

        if (schoolId.HasValue)
            q = q.Where(s => s.Assignments.Any(a => a.SchoolId == schoolId && a.IsActive));

        if (!string.IsNullOrWhiteSpace(position))
            q = q.Where(s => s.Assignments.Any(a => a.Position == position && a.IsActive));

        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(s =>
                s.Person.FirstName.Contains(search) ||
                s.Person.LastName.Contains(search) ||
                (s.PersonnelCode != null && s.PersonnelCode.Contains(search)) ||
                (s.Person.NationalCode != null && s.Person.NationalCode.Contains(search)) ||
                (s.Person.Mobile != null && s.Person.Mobile.Contains(search)));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(s => s.Person.LastName).ThenBy(s => s.Person.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new StaffDto
            {
                StaffId = s.StaffId,
                PersonId = s.PersonId,
                FirstName = s.Person.FirstName,
                LastName = s.Person.LastName,
                PersonnelCode = s.PersonnelCode,
                NationalCode = s.Person.NationalCode,
                Gender = s.Person.Gender,
                Mobile = s.Person.Mobile,
                Email = s.Person.Email,
                EmploymentType = s.EmploymentType,
                Degree = s.Degree,
                FieldOfStudy = s.FieldOfStudy,
                HireDate = s.HireDate,
                IBAN = s.IBAN,
                IsActive = s.IsActive,
                AssignmentCount = s.Assignments.Count(a => a.IsActive),
                Positions = s.Assignments.Where(a => a.IsActive).Select(a => a.Position).Distinct().ToList()
            }).ToListAsync();

        return new PagedResult<StaffDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result<StaffDto>> GetByIdAsync(int id)
    {
        var s = await _db.Staff.Include(x => x.Person).Include(x => x.Assignments)
            .AsNoTracking().FirstOrDefaultAsync(x => x.StaffId == id);
        if (s is null) return Result<StaffDto>.Fail("کارمند یافت نشد");

        return Result<StaffDto>.Ok(new StaffDto
        {
            StaffId = s.StaffId, PersonId = s.PersonId,
            FirstName = s.Person.FirstName, LastName = s.Person.LastName,
            PersonnelCode = s.PersonnelCode, NationalCode = s.Person.NationalCode,
            Gender = s.Person.Gender, Mobile = s.Person.Mobile, Email = s.Person.Email,
            EmploymentType = s.EmploymentType, Degree = s.Degree, FieldOfStudy = s.FieldOfStudy,
            HireDate = s.HireDate, IBAN = s.IBAN, IsActive = s.IsActive,
            AssignmentCount = s.Assignments.Count(a => a.IsActive),
            Positions = s.Assignments.Where(a => a.IsActive).Select(a => a.Position).Distinct().ToList()
        });
    }

    public async Task<Result<int>> CreateAsync(StaffCreateDto dto)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (!string.IsNullOrEmpty(dto.PersonnelCode) &&
                await _db.Staff.AnyAsync(s => s.PersonnelCode == dto.PersonnelCode))
                return Result<int>.Fail("کد پرسنلی تکراری است");

            if (!string.IsNullOrEmpty(dto.NationalCode) &&
                await _db.Persons.AnyAsync(p => p.NationalCode == dto.NationalCode))
                return Result<int>.Fail("کد ملی قبلاً ثبت شده");

            if (dto.CreateUserAccount && !string.IsNullOrEmpty(dto.Username) &&
                await _db.Users.AnyAsync(u => u.Username == dto.Username))
                return Result<int>.Fail("نام کاربری تکراری است");

            var person = new Person
            {
                FirstName = dto.FirstName, LastName = dto.LastName, FatherName = dto.FatherName,
                NationalCode = dto.NationalCode, Gender = dto.Gender, BirthDate = dto.BirthDate,
                Mobile = dto.Mobile, Email = dto.Email, Address = dto.Address
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var staff = new Staff
            {
                PersonId = person.PersonId, PersonnelCode = dto.PersonnelCode,
                EmploymentType = dto.EmploymentType, Degree = dto.Degree,
                FieldOfStudy = dto.FieldOfStudy, HireDate = dto.HireDate, IBAN = dto.IBAN
            };
            _db.Staff.Add(staff);
            await _db.SaveChangesAsync();

            if (dto.CreateUserAccount && !string.IsNullOrEmpty(dto.Username) && !string.IsNullOrEmpty(dto.Password))
            {
                var user = new User
                {
                    PersonId = person.PersonId,
                    Username = dto.Username,
                    PasswordHash = _hasher.Hash(dto.Password)
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Result<int>.Ok(staff.StaffId, "کارمند با موفقیت ثبت شد");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result<int>.Fail($"خطا: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(StaffUpdateDto dto)
    {
        var staff = await _db.Staff.Include(s => s.Person).FirstOrDefaultAsync(s => s.StaffId == dto.StaffId);
        if (staff is null) return Result.Fail("کارمند یافت نشد");

        staff.Person.FirstName = dto.FirstName;
        staff.Person.LastName = dto.LastName;
        staff.Person.FatherName = dto.FatherName;
        staff.Person.NationalCode = dto.NationalCode;
        staff.Person.Gender = dto.Gender;
        staff.Person.BirthDate = dto.BirthDate;
        staff.Person.Mobile = dto.Mobile;
        staff.Person.Email = dto.Email;
        staff.Person.Address = dto.Address;
        staff.Person.ModifiedAt = DateTime.UtcNow;

        staff.PersonnelCode = dto.PersonnelCode;
        staff.EmploymentType = dto.EmploymentType;
        staff.Degree = dto.Degree;
        staff.FieldOfStudy = dto.FieldOfStudy;
        staff.HireDate = dto.HireDate;
        staff.IBAN = dto.IBAN;
        staff.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Result.Ok("کارمند ویرایش شد");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var staff = await _db.Staff.Include(s => s.Assignments).FirstOrDefaultAsync(s => s.StaffId == id);
        if (staff is null) return Result.Fail("کارمند یافت نشد");
        if (staff.Assignments.Any(a => a.IsActive))
            return Result.Fail("این کارمند تخصیص فعال دارد. ابتدا تخصیص‌ها را غیرفعال کنید");

        staff.IsActive = false;
        await _db.SaveChangesAsync();
        return Result.Ok("کارمند غیرفعال شد");
    }

    public async Task<List<StaffAssignmentDto>> GetAssignmentsAsync(int staffId)
    {
        return await _db.StaffAssignments
            .Include(a => a.School).Include(a => a.AcademicYear).Include(a => a.Staff).ThenInclude(s => s.Person)
            .Where(a => a.StaffId == staffId)
            .AsNoTracking()
            .OrderByDescending(a => a.AcademicYearId).ThenBy(a => a.School.Name)
            .Select(a => new StaffAssignmentDto
            {
                AssignmentId = a.AssignmentId, StaffId = a.StaffId,
                StaffName = a.Staff.Person.FirstName + " " + a.Staff.Person.LastName,
                SchoolId = a.SchoolId, SchoolName = a.School.Name,
                AcademicYearId = a.AcademicYearId, AcademicYearTitle = a.AcademicYear.Title,
                Position = a.Position, WeeklyHours = a.WeeklyHours,
                StartDate = a.StartDate, EndDate = a.EndDate, IsActive = a.IsActive
            }).ToListAsync();
    }

    public async Task<Result> AddAssignmentAsync(StaffAssignmentCreateDto dto)
    {
        if (await _db.StaffAssignments.AnyAsync(a =>
            a.StaffId == dto.StaffId && a.SchoolId == dto.SchoolId &&
            a.AcademicYearId == dto.AcademicYearId && a.Position == dto.Position))
            return Result.Fail("این تخصیص قبلاً ثبت شده است");

        _db.StaffAssignments.Add(new StaffSchoolAssignment
        {
            StaffId = dto.StaffId, SchoolId = dto.SchoolId,
            AcademicYearId = dto.AcademicYearId, Position = dto.Position,
            WeeklyHours = dto.WeeklyHours, StartDate = dto.StartDate
        });
        await _db.SaveChangesAsync();
        return Result.Ok("تخصیص جدید اضافه شد");
    }

    public async Task<Result> RemoveAssignmentAsync(int assignmentId)
    {
        var a = await _db.StaffAssignments.FindAsync(assignmentId);
        if (a is null) return Result.Fail("تخصیص یافت نشد");
        _db.StaffAssignments.Remove(a);
        await _db.SaveChangesAsync();
        return Result.Ok("تخصیص حذف شد");
    }

    public async Task<List<StaffDto>> GetTeachersBySchoolAsync(int schoolId, int? academicYearId = null)
    {
        academicYearId ??= (await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive))?.AcademicYearId;

        return await _db.Staff
            .Include(s => s.Person)
            .Where(s => s.IsActive && s.Assignments.Any(a =>
                a.IsActive && a.SchoolId == schoolId &&
                (!academicYearId.HasValue || a.AcademicYearId == academicYearId)))
            .AsNoTracking()
            .Select(s => new StaffDto
            {
                StaffId = s.StaffId, PersonId = s.PersonId,
                FirstName = s.Person.FirstName, LastName = s.Person.LastName,
                PersonnelCode = s.PersonnelCode, Gender = s.Person.Gender,
                Mobile = s.Person.Mobile
            })
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class GuardianService : IGuardianService
{
    private readonly SmsDbContext _db;
    public GuardianService(SmsDbContext db) => _db = db;

    public async Task<PagedResult<GuardianDto>> GetPagedAsync(string? search, int page = 1, int pageSize = 20)
    {
        var q = _db.Guardians.Include(g => g.Person).ThenInclude(p => p.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(g => g.Person.FirstName.Contains(search) ||
                             g.Person.LastName.Contains(search) ||
                             (g.Person.Mobile != null && g.Person.Mobile.Contains(search)) ||
                             (g.Person.NationalCode != null && g.Person.NationalCode.Contains(search)));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(g => g.Person.LastName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => new GuardianDto
            {
                GuardianId = g.GuardianId, PersonId = g.PersonId,
                FirstName = g.Person.FirstName, LastName = g.Person.LastName,
                NationalCode = g.Person.NationalCode, Mobile = g.Person.Mobile,
                Occupation = g.Occupation, WorkplacePhone = g.WorkplacePhone,
                EducationLevel = g.EducationLevel,
                StudentCount = _db.StudentGuardians.Count(sg => sg.GuardianId == g.GuardianId),
                UserId = g.Person.User != null ? g.Person.User.UserId : null,
                Username = g.Person.User != null ? g.Person.User.Username : null,
                IsUserLocked = g.Person.User != null ? g.Person.User.IsLocked : false
            }).ToListAsync();

        return new PagedResult<GuardianDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result<GuardianDto>> GetByIdAsync(int id)
    {
        var g = await _db.Guardians.Include(x => x.Person).ThenInclude(p => p.User)
            .AsNoTracking().FirstOrDefaultAsync(x => x.GuardianId == id);
        if (g is null) return Result<GuardianDto>.Fail("ولی یافت نشد");

        return Result<GuardianDto>.Ok(new GuardianDto
        {
            GuardianId = g.GuardianId, PersonId = g.PersonId,
            FirstName = g.Person.FirstName, LastName = g.Person.LastName,
            NationalCode = g.Person.NationalCode, Mobile = g.Person.Mobile,
            Occupation = g.Occupation, WorkplacePhone = g.WorkplacePhone,
            EducationLevel = g.EducationLevel,
            StudentCount = await _db.StudentGuardians.CountAsync(sg => sg.GuardianId == g.GuardianId),
            UserId = g.Person.User?.UserId,
            Username = g.Person.User?.Username,
            IsUserLocked = g.Person.User?.IsLocked ?? false
        });
    }

    public async Task<Result<int>> CreateAsync(GuardianCreateDto dto)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (!string.IsNullOrEmpty(dto.NationalCode) &&
                await _db.Persons.AnyAsync(p => p.NationalCode == dto.NationalCode))
                return Result<int>.Fail("کد ملی قبلاً ثبت شده");

            var person = new Person
            {
                FirstName = dto.FirstName, LastName = dto.LastName,
                NationalCode = dto.NationalCode, Gender = dto.Gender,
                Mobile = dto.Mobile, Email = dto.Email, Address = dto.Address
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var guardian = new Guardian
            {
                PersonId = person.PersonId, Occupation = dto.Occupation,
                WorkplacePhone = dto.WorkplacePhone, EducationLevel = dto.EducationLevel
            };
            _db.Guardians.Add(guardian);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return Result<int>.Ok(guardian.GuardianId, "ولی ثبت شد");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result<int>.Fail($"خطا: {ex.Message}");
        }
    }

    public async Task<List<StudentGuardianDto>> GetStudentGuardiansAsync(int studentId)
    {
        return await _db.StudentGuardians
            .Include(sg => sg.Guardian).ThenInclude(g => g.Person)
            .Where(sg => sg.StudentId == studentId)
            .AsNoTracking()
            .Select(sg => new StudentGuardianDto
            {
                StudentId = sg.StudentId, GuardianId = sg.GuardianId,
                GuardianName = sg.Guardian.Person.FirstName + " " + sg.Guardian.Person.LastName,
                Mobile = sg.Guardian.Person.Mobile,
                Relationship = sg.Relationship, IsPrimary = sg.IsPrimary,
                HasCustody = sg.HasCustody, CanPickup = sg.CanPickup
            }).ToListAsync();
    }

    public async Task<Result> AssignToStudentAsync(AssignGuardianDto dto)
    {
        if (await _db.StudentGuardians.AnyAsync(sg => sg.StudentId == dto.StudentId && sg.GuardianId == dto.GuardianId))
            return Result.Fail("این ولی قبلاً به این دانش‌آموز متصل شده");

        if (dto.IsPrimary)
        {
            var others = await _db.StudentGuardians.Where(sg => sg.StudentId == dto.StudentId).ToListAsync();
            foreach (var o in others) o.IsPrimary = false;
        }

        _db.StudentGuardians.Add(new StudentGuardian
        {
            StudentId = dto.StudentId, GuardianId = dto.GuardianId,
            Relationship = dto.Relationship, IsPrimary = dto.IsPrimary,
            HasCustody = dto.HasCustody, CanPickup = dto.CanPickup
        });
        await _db.SaveChangesAsync();
        return Result.Ok("ولی به دانش‌آموز متصل شد");
    }

    public async Task<Result> RemoveFromStudentAsync(int studentId, int guardianId)
    {
        var sg = await _db.StudentGuardians.FindAsync(studentId, guardianId);
        if (sg is null) return Result.Fail("یافت نشد");
        _db.StudentGuardians.Remove(sg);
        await _db.SaveChangesAsync();
        return Result.Ok("ارتباط حذف شد");
    }
}

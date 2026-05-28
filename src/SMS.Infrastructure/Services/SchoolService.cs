using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class SchoolService : ISchoolService
{
    private readonly SmsDbContext _db;
    public SchoolService(SmsDbContext db) => _db = db;

    public async Task<PagedResult<SchoolDto>> GetPagedAsync(string? search, int page = 1, int pageSize = 20)
    {
        var q = _db.Schools
            .Include(s => s.City).ThenInclude(c => c.Province)
            .Include(s => s.EducationLevel)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.Name.Contains(search) || s.Code.Contains(search));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SchoolDto
            {
                SchoolId = s.SchoolId,
                Name = s.Name,
                Code = s.Code,
                CityId = s.CityId,
                CityName = s.City.Name,
                ProvinceName = s.City.Province.Name,
                Gender = s.Gender,
                SchoolType = s.SchoolType,
                EducationLevelId = s.EducationLevelId,
                EducationLevelName = s.EducationLevel.Name,
                Address = s.Address,
                Phone = s.Phone,
                IsActive = s.IsActive,
                ClassroomCount = s.Classrooms.Count,
                StudentCount = s.Classrooms.SelectMany(c => c.Enrollments).Count(e => e.Status == "فعال")
            })
            .ToListAsync();

        return new PagedResult<SchoolDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result<SchoolDto>> GetByIdAsync(int id)
    {
        var s = await _db.Schools
            .Include(x => x.City).ThenInclude(c => c.Province)
            .Include(x => x.EducationLevel)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == id);

        if (s is null) return Result<SchoolDto>.Fail("مدرسه یافت نشد");

        return Result<SchoolDto>.Ok(new SchoolDto
        {
            SchoolId = s.SchoolId,
            Name = s.Name,
            Code = s.Code,
            CityId = s.CityId,
            CityName = s.City.Name,
            ProvinceName = s.City.Province.Name,
            Gender = s.Gender,
            SchoolType = s.SchoolType,
            EducationLevelId = s.EducationLevelId,
            EducationLevelName = s.EducationLevel.Name,
            Address = s.Address,
            Phone = s.Phone,
            IsActive = s.IsActive
        });
    }

    public async Task<Result<int>> CreateAsync(SchoolCreateDto dto)
    {
        if (await _db.Schools.AnyAsync(s => s.Code == dto.Code))
            return Result<int>.Fail("کد مدرسه تکراری است");

        var school = new School
        {
            Name = dto.Name,
            Code = dto.Code,
            CityId = dto.CityId,
            Gender = dto.Gender,
            SchoolType = dto.SchoolType,
            EducationLevelId = dto.EducationLevelId,
            Address = dto.Address,
            Phone = dto.Phone
        };
        _db.Schools.Add(school);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(school.SchoolId, "مدرسه با موفقیت ایجاد شد");
    }

    public async Task<Result> UpdateAsync(SchoolUpdateDto dto)
    {
        var school = await _db.Schools.FindAsync(dto.SchoolId);
        if (school is null) return Result.Fail("مدرسه یافت نشد");

        if (school.Code != dto.Code && await _db.Schools.AnyAsync(s => s.Code == dto.Code))
            return Result.Fail("کد مدرسه تکراری است");

        school.Name = dto.Name;
        school.Code = dto.Code;
        school.CityId = dto.CityId;
        school.Gender = dto.Gender;
        school.SchoolType = dto.SchoolType;
        school.EducationLevelId = dto.EducationLevelId;
        school.Address = dto.Address;
        school.Phone = dto.Phone;
        school.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Result.Ok("مدرسه با موفقیت ویرایش شد");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var school = await _db.Schools.Include(s => s.Classrooms).FirstOrDefaultAsync(s => s.SchoolId == id);
        if (school is null) return Result.Fail("مدرسه یافت نشد");
        if (school.Classrooms.Any())
            return Result.Fail("این مدرسه دارای کلاس فعال است و قابل حذف نیست");

        _db.Schools.Remove(school);
        await _db.SaveChangesAsync();
        return Result.Ok("مدرسه حذف شد");
    }

    public async Task<Result> ToggleActiveAsync(int id)
    {
        var school = await _db.Schools.FindAsync(id);
        if (school is null) return Result.Fail("مدرسه یافت نشد");
        school.IsActive = !school.IsActive;
        await _db.SaveChangesAsync();
        return Result.Ok(school.IsActive ? "مدرسه فعال شد" : "مدرسه غیرفعال شد");
    }
}

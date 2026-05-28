using Microsoft.EntityFrameworkCore;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class LookupService : ILookupService
{
    private readonly SmsDbContext _db;
    public LookupService(SmsDbContext db) => _db = db;

    public async Task<List<LookupDto>> GetProvincesAsync() =>
        await _db.Provinces.AsNoTracking().OrderBy(p => p.Name)
            .Select(p => new LookupDto { Id = p.ProvinceId, Name = p.Name }).ToListAsync();

    public async Task<List<CityLookupDto>> GetCitiesAsync(int? provinceId = null) =>
        await _db.Cities.Include(c => c.Province)
            .AsNoTracking()
            .Where(c => !provinceId.HasValue || c.ProvinceId == provinceId)
            .OrderBy(c => c.Name)
            .Select(c => new CityLookupDto
            {
                Id = c.CityId, Name = c.Name,
                ProvinceId = c.ProvinceId, ProvinceName = c.Province.Name
            }).ToListAsync();

    public async Task<List<LookupDto>> GetEducationLevelsAsync() =>
        await _db.EducationLevels.AsNoTracking().OrderBy(e => e.MinGrade)
            .Select(e => new LookupDto { Id = e.EducationLevelId, Name = e.Name }).ToListAsync();

    public async Task<List<GradeLookupDto>> GetGradesAsync(int? educationLevelId = null) =>
        await _db.Grades.Include(g => g.EducationLevel)
            .AsNoTracking()
            .Where(g => !educationLevelId.HasValue || g.EducationLevelId == educationLevelId)
            .OrderBy(g => g.OrderNo)
            .Select(g => new GradeLookupDto
            {
                Id = g.GradeId, Name = g.Name,
                EducationLevelId = g.EducationLevelId,
                IsDescriptive = g.EducationLevel.IsDescriptive
            }).ToListAsync();

    public async Task<List<LookupDto>> GetAcademicYearsAsync() =>
        await _db.AcademicYears.AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .Select(a => new LookupDto { Id = a.AcademicYearId, Name = a.Title }).ToListAsync();

    public async Task<LookupDto?> GetActiveAcademicYearAsync() =>
        await _db.AcademicYears.AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new LookupDto { Id = a.AcademicYearId, Name = a.Title })
            .FirstOrDefaultAsync();

    public async Task<List<LookupDto>> GetTermsAsync(int? academicYearId = null)
    {
        academicYearId ??= (await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive))?.AcademicYearId;
        return await _db.Terms
            .Where(t => !academicYearId.HasValue || t.AcademicYearId == academicYearId)
            .AsNoTracking().OrderBy(t => t.OrderNo)
            .Select(t => new LookupDto { Id = t.TermId, Name = t.Name }).ToListAsync();
    }
}

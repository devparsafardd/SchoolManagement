using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface ILookupService
{
    Task<List<LookupDto>> GetProvincesAsync();
    Task<List<CityLookupDto>> GetCitiesAsync(int? provinceId = null);
    Task<List<LookupDto>> GetEducationLevelsAsync();
    Task<List<GradeLookupDto>> GetGradesAsync(int? educationLevelId = null);
    Task<List<LookupDto>> GetAcademicYearsAsync();
    Task<LookupDto?> GetActiveAcademicYearAsync();
    Task<List<LookupDto>> GetTermsAsync(int? academicYearId = null);
}

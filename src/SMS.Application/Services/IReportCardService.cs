using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

/// <summary>سرویس کارنامه و گزارش تحصیلی</summary>
public interface IReportCardService
{
    Task<Result<StudentReportCardDto>> GenerateAsync(int studentId, int termId);
    Task<List<StudentReportCardDto>> GenerateClassReportAsync(int classroomId, int termId);
    Task<Result> CalculateClassroomGradesAsync(int classroomId, int termId);
    Task<byte[]> ExportToPdfAsync(StudentReportCardDto dto);
}

using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IHomeworkService
{
    // معلم
    Task<List<HomeworkDto>> GetByClassSubjectAsync(int classSubjectId);
    Task<List<HomeworkDto>> GetByTeacherAsync(int staffId, bool onlyActive = true);
    Task<Result<HomeworkDto>> GetByIdAsync(long id);
    Task<Result<long>> CreateAsync(HomeworkCreateDto dto, int staffId);
    Task<Result> UpdateAsync(long id, HomeworkCreateDto dto, int staffId);
    Task<Result> DeleteAsync(long id, int staffId);

    Task<List<HomeworkSubmissionDto>> GetSubmissionsAsync(long homeworkId);
    Task<Result> GradeSubmissionAsync(HomeworkGradeDto dto, int staffId);

    // دانش‌آموز
    Task<List<HomeworkDto>> GetForStudentAsync(int studentId, bool onlyActive = true);
    Task<Result<HomeworkSubmissionDto?>> GetSubmissionAsync(long homeworkId, int studentId);
    Task<Result<long>> SubmitAsync(HomeworkSubmitDto dto);
}

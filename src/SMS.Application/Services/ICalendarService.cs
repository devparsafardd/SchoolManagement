using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface ICalendarService
{
    Task<List<CalendarEventDto>> GetEventsAsync(DateTime fromDate, DateTime toDate, int? schoolId = null, int? classroomId = null);
    Task<List<CalendarEventDto>> GetUpcomingAsync(int days = 30, int? schoolId = null);
    Task<Result<int>> CreateAsync(CalendarEventCreateDto dto, int createdByUserId);
    Task<Result> UpdateAsync(int eventId, CalendarEventCreateDto dto);
    Task<Result> DeleteAsync(int eventId);
    Task<Result<CalendarEventDto>> GetByIdAsync(int eventId);
}

public interface ISurveyService
{
    Task<List<SurveyDto>> GetAllAsync(bool onlyActive = false);
    Task<List<SurveyDto>> GetForUserAsync(int userId);
    Task<Result<SurveyDto>> GetByIdAsync(int surveyId);
    Task<List<SurveyQuestionDto>> GetQuestionsAsync(int surveyId);
    Task<Result<int>> CreateAsync(SurveyCreateDto dto, int createdByUserId);
    Task<Result> DeleteAsync(int surveyId);
    Task<bool> HasUserRespondedAsync(int surveyId, int userId);
    Task<Result> SubmitAsync(SurveySubmitDto dto, int userId);
    Task<Result<SurveyResultDto>> GetResultsAsync(int surveyId);
}

using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class SurveyService : ISurveyService
{
    private readonly SmsDbContext _db;
    public SurveyService(SmsDbContext db) => _db = db;

    public async Task<List<SurveyDto>> GetAllAsync(bool onlyActive = false)
    {
        var q = _db.Surveys.AsNoTracking().Include(s => s.CreatedBy).ThenInclude(u => u.Person).AsQueryable();
        if (onlyActive) q = q.Where(s => s.IsActive && s.EndDate >= DateTime.Today);
        return await q.OrderByDescending(s => s.CreatedAt)
            .Select(s => new SurveyDto
            {
                SurveyId = s.SurveyId, Title = s.Title, Description = s.Description,
                TargetAudience = s.TargetAudience,
                SchoolId = s.SchoolId, ClassroomId = s.ClassroomId,
                StartDate = s.StartDate, EndDate = s.EndDate,
                IsAnonymous = s.IsAnonymous, IsActive = s.IsActive,
                CreatedByName = s.CreatedBy.Person.FirstName + " " + s.CreatedBy.Person.LastName,
                QuestionCount = s.Questions.Count,
                ResponseCount = _db.SurveyAnswers.Where(a => a.SurveyId == s.SurveyId).Select(a => a.UserId).Distinct().Count()
            }).ToListAsync();
    }

    public async Task<List<SurveyDto>> GetForUserAsync(int userId)
    {
        // نقش‌های کاربر
        var userRoles = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Select(ur => ur.Role.Name).ToListAsync();
        var audiences = new List<string> { "All" };
        if (userRoles.Contains("Student")) audiences.Add("Students");
        if (userRoles.Contains("Parent")) audiences.Add("Parents");
        if (userRoles.Contains("Teacher") || userRoles.Contains("Counselor")) audiences.Add("Teachers");

        var all = await GetAllAsync(onlyActive: true);
        return all.Where(s => audiences.Contains(s.TargetAudience)).ToList();
    }

    public async Task<Result<SurveyDto>> GetByIdAsync(int surveyId)
    {
        var list = await GetAllAsync();
        var s = list.FirstOrDefault(x => x.SurveyId == surveyId);
        if (s is null) return Result<SurveyDto>.Fail("نظرسنجی یافت نشد");
        return Result<SurveyDto>.Ok(s);
    }

    public async Task<List<SurveyQuestionDto>> GetQuestionsAsync(int surveyId)
    {
        return await _db.SurveyQuestions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.OrderNo)
            .Select(q => new SurveyQuestionDto
            {
                QuestionId = q.QuestionId, SurveyId = q.SurveyId,
                Text = q.Text, QuestionType = q.QuestionType,
                Options = q.Options, IsRequired = q.IsRequired,
                OrderNo = q.OrderNo
            }).ToListAsync();
    }

    public async Task<Result<int>> CreateAsync(SurveyCreateDto dto, int createdByUserId)
    {
        var survey = new Survey
        {
            Title = dto.Title, Description = dto.Description,
            TargetAudience = dto.TargetAudience,
            SchoolId = dto.SchoolId, ClassroomId = dto.ClassroomId,
            StartDate = dto.StartDate.Date, EndDate = dto.EndDate.Date,
            IsAnonymous = dto.IsAnonymous, IsActive = true,
            CreatedByUserId = createdByUserId
        };
        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync();

        int order = 1;
        foreach (var q in dto.Questions)
        {
            _db.SurveyQuestions.Add(new SurveyQuestion
            {
                SurveyId = survey.SurveyId,
                Text = q.Text, QuestionType = q.QuestionType,
                Options = q.Options, IsRequired = q.IsRequired,
                OrderNo = order++
            });
        }
        await _db.SaveChangesAsync();
        return Result<int>.Ok(survey.SurveyId, "نظرسنجی ایجاد شد");
    }

    public async Task<Result> DeleteAsync(int surveyId)
    {
        var s = await _db.Surveys.FirstOrDefaultAsync(x => x.SurveyId == surveyId);
        if (s is null) return Result.Fail("یافت نشد");
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return Result.Ok("نظرسنجی غیرفعال شد");
    }

    public async Task<bool> HasUserRespondedAsync(int surveyId, int userId)
        => await _db.SurveyAnswers.AnyAsync(a => a.SurveyId == surveyId && a.UserId == userId);

    public async Task<Result> SubmitAsync(SurveySubmitDto dto, int userId)
    {
        if (await HasUserRespondedAsync(dto.SurveyId, userId))
            return Result.Fail("شما قبلاً به این نظرسنجی پاسخ داده‌اید");

        foreach (var a in dto.Answers)
        {
            if (string.IsNullOrWhiteSpace(a.AnswerText)) continue;
            _db.SurveyAnswers.Add(new SurveyAnswer
            {
                SurveyId = dto.SurveyId,
                QuestionId = a.QuestionId,
                UserId = userId,
                AnswerText = a.AnswerText
            });
        }
        await _db.SaveChangesAsync();
        return Result.Ok("با تشکر، پاسخ شما ثبت شد");
    }

    public async Task<Result<SurveyResultDto>> GetResultsAsync(int surveyId)
    {
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.SurveyId == surveyId);
        if (survey is null) return Result<SurveyResultDto>.Fail("یافت نشد");

        var questions = await GetQuestionsAsync(surveyId);
        var totalResp = await _db.SurveyAnswers.Where(a => a.SurveyId == surveyId).Select(a => a.UserId).Distinct().CountAsync();

        var qResults = new List<SurveyQuestionResultDto>();
        foreach (var q in questions)
        {
            var answers = await _db.SurveyAnswers.AsNoTracking()
                .Where(a => a.QuestionId == q.QuestionId)
                .Select(a => a.AnswerText).ToListAsync();
            var nonEmpty = answers.Where(a => !string.IsNullOrEmpty(a)).ToList();

            var qr = new SurveyQuestionResultDto
            {
                QuestionId = q.QuestionId, Text = q.Text, QuestionType = q.QuestionType
            };

            if (q.QuestionType == "SingleChoice" || q.QuestionType == "YesNo" || q.QuestionType == "Rating")
            {
                var grouped = nonEmpty.GroupBy(a => a!).Select(g => new { Opt = g.Key, Count = g.Count() }).ToList();
                var total = grouped.Sum(x => x.Count);
                qr.Summary = grouped.Select(g => new SurveyAnswerSummary
                {
                    Option = g.Opt,
                    Count = g.Count,
                    Percent = total > 0 ? Math.Round((decimal)g.Count / total * 100, 1) : 0
                }).OrderByDescending(s => s.Count).ToList();
            }
            else if (q.QuestionType == "MultipleChoice")
            {
                // فرض: گزینه‌ها با کاما جدا شدن
                var flat = nonEmpty.SelectMany(a => a!.Split(',').Select(x => x.Trim())).Where(x => !string.IsNullOrEmpty(x)).ToList();
                var grouped = flat.GroupBy(x => x).Select(g => new { Opt = g.Key, Count = g.Count() }).ToList();
                var total = grouped.Sum(x => x.Count);
                qr.Summary = grouped.Select(g => new SurveyAnswerSummary
                {
                    Option = g.Opt, Count = g.Count,
                    Percent = total > 0 ? Math.Round((decimal)g.Count / total * 100, 1) : 0
                }).OrderByDescending(s => s.Count).ToList();
            }
            else // Text
            {
                qr.TextAnswers = nonEmpty.Select(a => a!).ToList();
            }
            qResults.Add(qr);
        }

        return Result<SurveyResultDto>.Ok(new SurveyResultDto
        {
            SurveyId = surveyId, Title = survey.Title,
            TotalResponses = totalResp, Questions = qResults
        });
    }
}

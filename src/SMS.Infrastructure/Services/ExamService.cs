using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class ExamService : IExamService
{
    private readonly SmsDbContext _db;
    private readonly INotificationService? _notification;
    public ExamService(SmsDbContext db, INotificationService? notification = null)
    {
        _db = db; _notification = notification;
    }

    private IQueryable<ExamDto> BaseQuery() => _db.Exams
        .Include(e => e.ClassSubject).ThenInclude(cs => cs.Classroom)
        .Include(e => e.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
        .Include(e => e.ClassSubject).ThenInclude(cs => cs.Staff).ThenInclude(s => s.Person)
        .Include(e => e.ExamType).Include(e => e.Term)
        .AsNoTracking()
        .Select(e => new ExamDto
        {
            ExamId = e.ExamId, Title = e.Title,
            ClassSubjectId = e.ClassSubjectId,
            SubjectName = e.ClassSubject.GradeSubject.Subject.Name,
            ClassroomName = e.ClassSubject.Classroom.Name,
            TeacherName = e.ClassSubject.Staff.Person.FirstName + " " + e.ClassSubject.Staff.Person.LastName,
            ExamTypeId = e.ExamTypeId, ExamTypeName = e.ExamType.Name,
            TermId = e.TermId, TermName = e.Term.Name,
            ExamDate = e.ExamDate, MaxScore = e.MaxScore, Weight = e.Weight,
            IsDescriptive = e.IsDescriptive, GradeScaleId = e.GradeScaleId,
            IsFinalized = e.IsFinalized,
            ScoresEntered = _db.ExamScores.Count(s => s.ExamId == e.ExamId),
            TotalStudents = _db.Enrollments.Count(en =>
                en.ClassroomId == e.ClassSubject.ClassroomId && en.Status == "فعال")
        });

    public async Task<List<ExamDto>> GetByClassSubjectAsync(int classSubjectId)
        => await BaseQuery().Where(e => e.ClassSubjectId == classSubjectId)
            .OrderByDescending(e => e.ExamDate).ToListAsync();

    public async Task<List<ExamDto>> GetByTeacherAsync(int staffId, int? termId = null)
    {
        var q = _db.Exams
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.Classroom)
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(e => e.ClassSubject).ThenInclude(cs => cs.Staff).ThenInclude(s => s.Person)
            .Include(e => e.ExamType).Include(e => e.Term)
            .Where(e => e.ClassSubject.StaffId == staffId);
        if (termId.HasValue) q = q.Where(e => e.TermId == termId);

        return await q.AsNoTracking()
            .OrderByDescending(e => e.ExamDate)
            .Select(e => new ExamDto
            {
                ExamId = e.ExamId, Title = e.Title,
                ClassSubjectId = e.ClassSubjectId,
                SubjectName = e.ClassSubject.GradeSubject.Subject.Name,
                ClassroomName = e.ClassSubject.Classroom.Name,
                TeacherName = e.ClassSubject.Staff.Person.FirstName + " " + e.ClassSubject.Staff.Person.LastName,
                ExamTypeId = e.ExamTypeId, ExamTypeName = e.ExamType.Name,
                TermId = e.TermId, TermName = e.Term.Name,
                ExamDate = e.ExamDate, MaxScore = e.MaxScore, Weight = e.Weight,
                IsDescriptive = e.IsDescriptive, GradeScaleId = e.GradeScaleId,
                IsFinalized = e.IsFinalized,
                ScoresEntered = _db.ExamScores.Count(s => s.ExamId == e.ExamId),
                TotalStudents = _db.Enrollments.Count(en =>
                    en.ClassroomId == e.ClassSubject.ClassroomId && en.Status == "فعال")
            }).ToListAsync();
    }

    public async Task<Result<ExamDto>> GetByIdAsync(long id)
    {
        var e = await BaseQuery().FirstOrDefaultAsync(x => x.ExamId == id);
        return e is null ? Result<ExamDto>.Fail("آزمون یافت نشد") : Result<ExamDto>.Ok(e);
    }

    public async Task<Result<long>> CreateAsync(ExamCreateDto dto, int createdByStaffId)
    {
        var cs = await _db.ClassSubjects.FindAsync(dto.ClassSubjectId);
        if (cs is null) return Result<long>.Fail("درس کلاس یافت نشد");

        var exam = new Exam
        {
            Title = dto.Title,
            ClassSubjectId = dto.ClassSubjectId,
            ExamTypeId = dto.ExamTypeId,
            TermId = dto.TermId,
            ExamDate = dto.ExamDate.Date,
            DurationMinutes = dto.DurationMinutes,
            MaxScore = dto.MaxScore,
            Weight = dto.Weight,
            IsDescriptive = dto.IsDescriptive,
            GradeScaleId = dto.GradeScaleId,
            Description = dto.Description,
            CreatedByStaffId = createdByStaffId
        };
        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();
        return Result<long>.Ok(exam.ExamId, "آزمون با موفقیت ایجاد شد");
    }

    public async Task<Result> UpdateAsync(long id, ExamCreateDto dto)
    {
        var exam = await _db.Exams.FindAsync(id);
        if (exam is null) return Result.Fail("آزمون یافت نشد");
        if (exam.IsFinalized) return Result.Fail("آزمون نهایی شده، قابل ویرایش نیست");

        exam.Title = dto.Title;
        exam.ExamTypeId = dto.ExamTypeId;
        exam.TermId = dto.TermId;
        exam.ExamDate = dto.ExamDate.Date;
        exam.DurationMinutes = dto.DurationMinutes;
        exam.MaxScore = dto.MaxScore;
        exam.Weight = dto.Weight;
        exam.IsDescriptive = dto.IsDescriptive;
        exam.GradeScaleId = dto.GradeScaleId;
        exam.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Result.Ok("آزمون ویرایش شد");
    }

    public async Task<Result> DeleteAsync(long id)
    {
        var exam = await _db.Exams.Include(e => e.Scores).FirstOrDefaultAsync(e => e.ExamId == id);
        if (exam is null) return Result.Fail("آزمون یافت نشد");
        if (exam.IsFinalized) return Result.Fail("آزمون نهایی شده، قابل حذف نیست");

        _db.ExamScores.RemoveRange(exam.Scores);
        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync();
        return Result.Ok("آزمون حذف شد");
    }

    public async Task<Result> FinalizeAsync(long id)
    {
        var exam = await _db.Exams.FindAsync(id);
        if (exam is null) return Result.Fail("آزمون یافت نشد");
        exam.IsFinalized = true;
        await _db.SaveChangesAsync();
        if (_notification != null) { try { await _notification.NotifyExamFinalizedAsync(id); } catch { } }
        return Result.Ok("آزمون نهایی شد و دیگر قابل تغییر نیست");
    }

    public async Task<Result> UnfinalizeAsync(long id)
    {
        var exam = await _db.Exams.FindAsync(id);
        if (exam is null) return Result.Fail("آزمون یافت نشد");
        exam.IsFinalized = false;
        await _db.SaveChangesAsync();
        return Result.Ok("آزمون به حالت قابل ویرایش بازگشت");
    }

    public async Task<List<ExamScoreRow>> GetScoresAsync(long examId)
    {
        var exam = await _db.Exams
            .Include(e => e.ClassSubject)
            .AsNoTracking().FirstOrDefaultAsync(e => e.ExamId == examId);
        if (exam is null) return new List<ExamScoreRow>();

        var students = await _db.Enrollments
            .Include(en => en.Student).ThenInclude(s => s.Person)
            .Where(en => en.ClassroomId == exam.ClassSubject.ClassroomId && en.Status == "فعال")
            .AsNoTracking()
            .OrderBy(en => en.Student.Person.LastName)
            .Select(en => new
            {
                en.StudentId, en.Student.StudentCode,
                FullName = en.Student.Person.FirstName + " " + en.Student.Person.LastName
            }).ToListAsync();

        var scores = await _db.ExamScores.AsNoTracking()
            .Where(s => s.ExamId == examId).ToListAsync();

        return students.Select(st =>
        {
            var sc = scores.FirstOrDefault(s => s.StudentId == st.StudentId);
            return new ExamScoreRow
            {
                StudentId = st.StudentId,
                StudentCode = st.StudentCode,
                FullName = st.FullName,
                ScoreId = sc?.ScoreId,
                NumericScore = sc?.NumericScore,
                DescriptiveScaleItemId = sc?.DescriptiveScaleItemId,
                IsAbsent = sc?.IsAbsent ?? false,
                IsExempt = sc?.IsExempt ?? false,
                Comment = sc?.Comment
            };
        }).ToList();
    }

    public async Task<Result> SaveScoresAsync(EnterScoresDto dto, int enteredByStaffId)
    {
        var exam = await _db.Exams.FindAsync(dto.ExamId);
        if (exam is null) return Result.Fail("آزمون یافت نشد");
        if (exam.IsFinalized) return Result.Fail("آزمون نهایی شده، نمی‌توان نمره ثبت کرد");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var row in dto.Scores)
            {
                var existing = await _db.ExamScores
                    .FirstOrDefaultAsync(s => s.ExamId == dto.ExamId && s.StudentId == row.StudentId);

                if (existing is null)
                {
                    // اگر هیچ مقداری نداره، اصلاً ثبت نکن
                    if (row.NumericScore is null && row.DescriptiveScaleItemId is null
                        && !row.IsAbsent && !row.IsExempt) continue;

                    _db.ExamScores.Add(new ExamScore
                    {
                        ExamId = dto.ExamId,
                        StudentId = row.StudentId,
                        NumericScore = exam.IsDescriptive ? null : row.NumericScore,
                        DescriptiveScaleItemId = exam.IsDescriptive ? row.DescriptiveScaleItemId : null,
                        IsAbsent = row.IsAbsent,
                        IsExempt = row.IsExempt,
                        Comment = row.Comment,
                        EnteredByStaffId = enteredByStaffId
                    });
                }
                else
                {
                    existing.NumericScore = exam.IsDescriptive ? null : row.NumericScore;
                    existing.DescriptiveScaleItemId = exam.IsDescriptive ? row.DescriptiveScaleItemId : null;
                    existing.IsAbsent = row.IsAbsent;
                    existing.IsExempt = row.IsExempt;
                    existing.Comment = row.Comment;
                    existing.ModifiedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Result.Ok("نمرات با موفقیت ثبت شد");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result.Fail($"خطا: {ex.Message}");
        }
    }

    public async Task<List<GradeScaleDto>> GetGradeScalesAsync()
    {
        return await _db.GradeScales.Include(gs => gs.Items)
            .Where(gs => gs.IsActive)
            .AsNoTracking()
            .Select(gs => new GradeScaleDto
            {
                GradeScaleId = gs.GradeScaleId,
                Name = gs.Name, IsDescriptive = gs.IsDescriptive,
                Items = gs.Items.OrderBy(i => i.OrderNo).Select(i => new GradeScaleItemDto
                {
                    GradeScaleItemId = i.GradeScaleItemId,
                    Symbol = i.Symbol, Label = i.Label,
                    NumericEquivalent = i.NumericEquivalent, OrderNo = i.OrderNo
                }).ToList()
            }).ToListAsync();
    }

    public async Task<List<ExamTypeDto>> GetExamTypesAsync()
    {
        return await _db.ExamTypes.AsNoTracking()
            .OrderBy(t => t.ExamTypeId)
            .Select(t => new ExamTypeDto
            {
                ExamTypeId = t.ExamTypeId,
                Name = t.Name,
                Code = t.Code,
                DefaultWeight = t.DefaultWeight,
                IsFinal = t.IsFinal
            }).ToListAsync();
    }
}

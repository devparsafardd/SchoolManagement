using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class HomeworkService : IHomeworkService
{
    private readonly SmsDbContext _db;
    public HomeworkService(SmsDbContext db) => _db = db;

    public async Task<List<HomeworkDto>> GetByClassSubjectAsync(int classSubjectId)
    {
        return await _db.Homeworks.AsNoTracking()
            .Include(h => h.ClassSubject).ThenInclude(cs => cs.Classroom)
            .Include(h => h.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(h => h.CreatedBy).ThenInclude(s => s.Person)
            .Where(h => h.ClassSubjectId == classSubjectId)
            .OrderByDescending(h => h.AssignedDate)
            .Select(h => new HomeworkDto
            {
                HomeworkId = h.HomeworkId, ClassSubjectId = h.ClassSubjectId,
                Title = h.Title, Description = h.Description,
                AssignedDate = h.AssignedDate, DueDate = h.DueDate,
                MaxScore = h.MaxScore, AttachmentPath = h.AttachmentPath,
                AllowFileSubmission = h.AllowFileSubmission, IsActive = h.IsActive,
                SubjectName = h.ClassSubject.GradeSubject.Subject.Name,
                ClassroomName = h.ClassSubject.Classroom.Name,
                CreatedByName = h.CreatedBy.Person.FirstName + " " + h.CreatedBy.Person.LastName,
                SubmissionsCount = h.Submissions.Count,
                GradedCount = h.Submissions.Count(s => s.Score.HasValue),
                TotalStudents = _db.Enrollments.Count(e => e.ClassroomId == h.ClassSubject.ClassroomId && e.Status == "فعال")
            }).ToListAsync();
    }

    public async Task<List<HomeworkDto>> GetByTeacherAsync(int staffId, bool onlyActive = true)
    {
        var q = _db.Homeworks.AsNoTracking()
            .Where(h => h.ClassSubject.StaffId == staffId);
        if (onlyActive) q = q.Where(h => h.IsActive);
        return await q
            .Include(h => h.ClassSubject).ThenInclude(cs => cs.Classroom)
            .Include(h => h.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .OrderByDescending(h => h.AssignedDate)
            .Select(h => new HomeworkDto
            {
                HomeworkId = h.HomeworkId, ClassSubjectId = h.ClassSubjectId,
                Title = h.Title, Description = h.Description,
                AssignedDate = h.AssignedDate, DueDate = h.DueDate,
                MaxScore = h.MaxScore, IsActive = h.IsActive,
                SubjectName = h.ClassSubject.GradeSubject.Subject.Name,
                ClassroomName = h.ClassSubject.Classroom.Name,
                SubmissionsCount = h.Submissions.Count,
                GradedCount = h.Submissions.Count(s => s.Score.HasValue),
                TotalStudents = _db.Enrollments.Count(e => e.ClassroomId == h.ClassSubject.ClassroomId && e.Status == "فعال")
            }).ToListAsync();
    }

    public async Task<Result<HomeworkDto>> GetByIdAsync(long id)
    {
        var h = await _db.Homeworks.AsNoTracking()
            .Include(x => x.ClassSubject).ThenInclude(cs => cs.Classroom)
            .Include(x => x.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(x => x.CreatedBy).ThenInclude(s => s.Person)
            .FirstOrDefaultAsync(x => x.HomeworkId == id);
        if (h is null) return Result<HomeworkDto>.Fail("تکلیف یافت نشد");

        return Result<HomeworkDto>.Ok(new HomeworkDto
        {
            HomeworkId = h.HomeworkId, ClassSubjectId = h.ClassSubjectId,
            Title = h.Title, Description = h.Description,
            AssignedDate = h.AssignedDate, DueDate = h.DueDate,
            MaxScore = h.MaxScore, AttachmentPath = h.AttachmentPath,
            AllowFileSubmission = h.AllowFileSubmission, IsActive = h.IsActive,
            SubjectName = h.ClassSubject.GradeSubject.Subject.Name,
            ClassroomName = h.ClassSubject.Classroom.Name,
            CreatedByName = h.CreatedBy.Person.FirstName + " " + h.CreatedBy.Person.LastName,
            SubmissionsCount = await _db.HomeworkSubmissions.CountAsync(s => s.HomeworkId == id),
            GradedCount = await _db.HomeworkSubmissions.CountAsync(s => s.HomeworkId == id && s.Score != null),
            TotalStudents = await _db.Enrollments.CountAsync(e => e.ClassroomId == h.ClassSubject.ClassroomId && e.Status == "فعال")
        });
    }

    public async Task<Result<long>> CreateAsync(HomeworkCreateDto dto, int staffId)
    {
        // چک کنیم این Staff معلم این ClassSubject هست
        var owns = await _db.ClassSubjects.AnyAsync(cs => cs.ClassSubjectId == dto.ClassSubjectId && cs.StaffId == staffId);
        if (!owns) return Result<long>.Fail("این درس کلاسی به شما تعلق ندارد");

        var h = new Homework
        {
            ClassSubjectId = dto.ClassSubjectId,
            CreatedByStaffId = staffId,
            Title = dto.Title,
            Description = dto.Description,
            AssignedDate = DateTime.Today,
            DueDate = dto.DueDate,
            MaxScore = dto.MaxScore,
            AttachmentPath = dto.AttachmentPath,
            AllowFileSubmission = dto.AllowFileSubmission
        };
        _db.Homeworks.Add(h);
        await _db.SaveChangesAsync();
        return Result<long>.Ok(h.HomeworkId, "تکلیف ایجاد شد");
    }

    public async Task<Result> UpdateAsync(long id, HomeworkCreateDto dto, int staffId)
    {
        var h = await _db.Homeworks.FirstOrDefaultAsync(x => x.HomeworkId == id);
        if (h is null) return Result.Fail("تکلیف یافت نشد");
        if (h.CreatedByStaffId != staffId) return Result.Fail("دسترسی ندارید");

        h.Title = dto.Title;
        h.Description = dto.Description;
        h.DueDate = dto.DueDate;
        h.MaxScore = dto.MaxScore;
        h.AttachmentPath = dto.AttachmentPath ?? h.AttachmentPath;
        h.AllowFileSubmission = dto.AllowFileSubmission;
        await _db.SaveChangesAsync();
        return Result.Ok("تکلیف به‌روزرسانی شد");
    }

    public async Task<Result> DeleteAsync(long id, int staffId)
    {
        var h = await _db.Homeworks.FirstOrDefaultAsync(x => x.HomeworkId == id);
        if (h is null) return Result.Fail("تکلیف یافت نشد");
        if (h.CreatedByStaffId != staffId) return Result.Fail("دسترسی ندارید");
        h.IsActive = false; // Soft delete
        await _db.SaveChangesAsync();
        return Result.Ok("تکلیف غیرفعال شد");
    }

    public async Task<List<HomeworkSubmissionDto>> GetSubmissionsAsync(long homeworkId)
    {
        // فقط تحویل دانش‌آموزانی که کلاس فعالی دارن
        var h = await _db.Homeworks.AsNoTracking()
            .Include(x => x.ClassSubject).FirstOrDefaultAsync(x => x.HomeworkId == homeworkId);
        if (h is null) return new();

        var students = await _db.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.Person)
            .Where(e => e.ClassroomId == h.ClassSubject.ClassroomId && e.Status == "فعال")
            .Select(e => new { e.StudentId, e.Student.StudentCode, e.Student.Person.FirstName, e.Student.Person.LastName })
            .ToListAsync();

        var subs = await _db.HomeworkSubmissions.AsNoTracking()
            .Where(s => s.HomeworkId == homeworkId).ToListAsync();

        return students.Select(st =>
        {
            var sub = subs.FirstOrDefault(s => s.StudentId == st.StudentId);
            return new HomeworkSubmissionDto
            {
                SubmissionId = sub?.SubmissionId ?? 0,
                HomeworkId = homeworkId,
                StudentId = st.StudentId,
                StudentCode = st.StudentCode,
                FullName = st.FirstName + " " + st.LastName,
                SubmittedAt = sub?.SubmittedAt ?? DateTime.MinValue,
                IsLate = sub?.IsLate ?? false,
                TextAnswer = sub?.TextAnswer,
                AttachmentPath = sub?.AttachmentPath,
                Score = sub?.Score,
                TeacherFeedback = sub?.TeacherFeedback
            };
        }).OrderBy(x => x.FullName).ToList();
    }

    public async Task<Result> GradeSubmissionAsync(HomeworkGradeDto dto, int staffId)
    {
        var sub = await _db.HomeworkSubmissions.Include(s => s.Homework)
            .FirstOrDefaultAsync(s => s.SubmissionId == dto.SubmissionId);
        if (sub is null) return Result.Fail("تحویل تکلیف یافت نشد");
        if (sub.Homework.CreatedByStaffId != staffId) return Result.Fail("دسترسی ندارید");

        sub.Score = dto.Score;
        sub.TeacherFeedback = dto.TeacherFeedback;
        sub.GradedByStaffId = staffId;
        sub.GradedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Result.Ok("نمره ثبت شد");
    }

    public async Task<List<HomeworkDto>> GetForStudentAsync(int studentId, bool onlyActive = true)
    {
        // کلاس‌های فعال این دانش‌آموز
        var classrooms = await _db.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == "فعال")
            .Select(e => e.ClassroomId).ToListAsync();

        var q = _db.Homeworks.AsNoTracking()
            .Where(h => classrooms.Contains(h.ClassSubject.ClassroomId));
        if (onlyActive) q = q.Where(h => h.IsActive);

        return await q
            .Include(h => h.ClassSubject).ThenInclude(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(h => h.CreatedBy).ThenInclude(s => s.Person)
            .OrderByDescending(h => h.AssignedDate)
            .Select(h => new HomeworkDto
            {
                HomeworkId = h.HomeworkId, ClassSubjectId = h.ClassSubjectId,
                Title = h.Title, Description = h.Description,
                AssignedDate = h.AssignedDate, DueDate = h.DueDate,
                MaxScore = h.MaxScore, AttachmentPath = h.AttachmentPath,
                AllowFileSubmission = h.AllowFileSubmission,
                SubjectName = h.ClassSubject.GradeSubject.Subject.Name,
                CreatedByName = h.CreatedBy.Person.FirstName + " " + h.CreatedBy.Person.LastName,
                SubmissionsCount = h.Submissions.Count(s => s.StudentId == studentId)
            }).ToListAsync();
    }

    public async Task<Result<HomeworkSubmissionDto?>> GetSubmissionAsync(long homeworkId, int studentId)
    {
        var sub = await _db.HomeworkSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.HomeworkId == homeworkId && s.StudentId == studentId);
        if (sub is null) return Result<HomeworkSubmissionDto?>.Ok(null);
        return Result<HomeworkSubmissionDto?>.Ok(new HomeworkSubmissionDto
        {
            SubmissionId = sub.SubmissionId,
            HomeworkId = sub.HomeworkId,
            StudentId = sub.StudentId,
            SubmittedAt = sub.SubmittedAt,
            IsLate = sub.IsLate,
            TextAnswer = sub.TextAnswer,
            AttachmentPath = sub.AttachmentPath,
            Score = sub.Score,
            TeacherFeedback = sub.TeacherFeedback,
            StudentCode = "", FullName = ""
        });
    }

    public async Task<Result<long>> SubmitAsync(HomeworkSubmitDto dto)
    {
        var h = await _db.Homeworks.FirstOrDefaultAsync(x => x.HomeworkId == dto.HomeworkId && x.IsActive);
        if (h is null) return Result<long>.Fail("تکلیف یافت نشد یا غیرفعال است");

        var existing = await _db.HomeworkSubmissions
            .FirstOrDefaultAsync(s => s.HomeworkId == dto.HomeworkId && s.StudentId == dto.StudentId);

        if (existing is null)
        {
            var sub = new HomeworkSubmission
            {
                HomeworkId = dto.HomeworkId,
                StudentId = dto.StudentId,
                TextAnswer = dto.TextAnswer,
                AttachmentPath = dto.AttachmentPath,
                SubmittedAt = DateTime.UtcNow,
                IsLate = DateTime.Today > h.DueDate.Date
            };
            _db.HomeworkSubmissions.Add(sub);
            await _db.SaveChangesAsync();
            return Result<long>.Ok(sub.SubmissionId, "تکلیف ارسال شد");
        }
        else
        {
            // اگر هنوز نمره داده نشده، اجازه ویرایش
            if (existing.Score.HasValue)
                return Result<long>.Fail("این تکلیف قبلاً نمره‌گذاری شده و قابل تغییر نیست");

            existing.TextAnswer = dto.TextAnswer;
            existing.AttachmentPath = dto.AttachmentPath ?? existing.AttachmentPath;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.IsLate = DateTime.Today > h.DueDate.Date;
            await _db.SaveChangesAsync();
            return Result<long>.Ok(existing.SubmissionId, "تکلیف ویرایش شد");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class SubjectService : ISubjectService
{
    private readonly SmsDbContext _db;
    public SubjectService(SmsDbContext db) => _db = db;

    public async Task<List<SubjectDto>> GetAllAsync(bool onlyActive = true)
    {
        var q = _db.Subjects.AsNoTracking().AsQueryable();
        if (onlyActive) q = q.Where(s => s.IsActive);
        return await q.OrderBy(s => s.Name)
            .Select(s => new SubjectDto
            {
                SubjectId = s.SubjectId, Name = s.Name, Code = s.Code,
                Description = s.Description, IsActive = s.IsActive,
                GradeCount = _db.GradeSubjects.Count(gs => gs.SubjectId == s.SubjectId)
            }).ToListAsync();
    }

    public async Task<Result<int>> CreateAsync(SubjectCreateDto dto)
    {
        if (await _db.Subjects.AnyAsync(s => s.Name == dto.Name))
            return Result<int>.Fail("درس با این نام قبلاً ثبت شده");

        var s = new Subject { Name = dto.Name, Code = dto.Code, Description = dto.Description };
        _db.Subjects.Add(s);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(s.SubjectId, "درس ایجاد شد");
    }

    public async Task<Result> UpdateAsync(int id, SubjectCreateDto dto)
    {
        var s = await _db.Subjects.FindAsync(id);
        if (s is null) return Result.Fail("درس یافت نشد");
        s.Name = dto.Name;
        s.Code = dto.Code;
        s.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Result.Ok("درس ویرایش شد");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        if (await _db.GradeSubjects.AnyAsync(gs => gs.SubjectId == id))
            return Result.Fail("این درس به پایه‌ای تخصیص داده شده و قابل حذف نیست");

        var s = await _db.Subjects.FindAsync(id);
        if (s is null) return Result.Fail("درس یافت نشد");
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return Result.Ok("درس غیرفعال شد");
    }

    // ============== Grade Subjects ==============
    public async Task<List<GradeSubjectDto>> GetByGradeAsync(int gradeId)
    {
        return await _db.GradeSubjects
            .Include(gs => gs.Grade).Include(gs => gs.Subject)
            .Where(gs => gs.GradeId == gradeId)
            .AsNoTracking()
            .OrderBy(gs => gs.Subject.Name)
            .Select(gs => new GradeSubjectDto
            {
                GradeSubjectId = gs.GradeSubjectId,
                GradeId = gs.GradeId, GradeName = gs.Grade.Name,
                SubjectId = gs.SubjectId, SubjectName = gs.Subject.Name,
                Credits = gs.Credits, Coefficient = gs.Coefficient,
                WeeklyHours = gs.WeeklyHours, IsDescriptive = gs.IsDescriptive,
                MaxScore = gs.MaxScore, PassingScore = gs.PassingScore
            }).ToListAsync();
    }

    public async Task<Result<int>> AddToGradeAsync(GradeSubjectCreateDto dto)
    {
        if (await _db.GradeSubjects.AnyAsync(gs => gs.GradeId == dto.GradeId && gs.SubjectId == dto.SubjectId))
            return Result<int>.Fail("این درس قبلاً برای این پایه ثبت شده");

        var gs = new GradeSubject
        {
            GradeId = dto.GradeId, SubjectId = dto.SubjectId,
            Credits = dto.Credits, Coefficient = dto.Coefficient,
            WeeklyHours = dto.WeeklyHours, IsDescriptive = dto.IsDescriptive,
            MaxScore = dto.MaxScore, PassingScore = dto.PassingScore
        };
        _db.GradeSubjects.Add(gs);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(gs.GradeSubjectId, "درس به پایه اضافه شد");
    }

    public async Task<Result> RemoveFromGradeAsync(int gradeSubjectId)
    {
        if (await _db.ClassSubjects.AnyAsync(cs => cs.GradeSubjectId == gradeSubjectId))
            return Result.Fail("این درس به کلاسی تخصیص داده شده و قابل حذف نیست");

        var gs = await _db.GradeSubjects.FindAsync(gradeSubjectId);
        if (gs is null) return Result.Fail("یافت نشد");
        _db.GradeSubjects.Remove(gs);
        await _db.SaveChangesAsync();
        return Result.Ok("حذف شد");
    }

    // ============== Class Subject Teachers ==============
    public async Task<List<ClassSubjectTeacherDto>> GetByClassroomAsync(int classroomId)
    {
        return await _db.ClassSubjects
            .Include(cs => cs.Classroom)
            .Include(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(cs => cs.Staff).ThenInclude(s => s.Person)
            .Where(cs => cs.ClassroomId == classroomId)
            .AsNoTracking()
            .OrderBy(cs => cs.GradeSubject.Subject.Name)
            .Select(cs => new ClassSubjectTeacherDto
            {
                ClassSubjectId = cs.ClassSubjectId,
                ClassroomId = cs.ClassroomId, ClassroomName = cs.Classroom.Name,
                GradeSubjectId = cs.GradeSubjectId,
                SubjectName = cs.GradeSubject.Subject.Name,
                Coefficient = cs.GradeSubject.Coefficient,
                StaffId = cs.StaffId,
                TeacherName = cs.Staff.Person.FirstName + " " + cs.Staff.Person.LastName,
                IsActive = cs.IsActive
            }).ToListAsync();
    }

    public async Task<Result<int>> AssignTeacherAsync(AssignTeacherDto dto)
    {
        if (await _db.ClassSubjects.AnyAsync(cs =>
            cs.ClassroomId == dto.ClassroomId && cs.GradeSubjectId == dto.GradeSubjectId))
            return Result<int>.Fail("این درس قبلاً به معلمی تخصیص داده شده");

        var cs = new ClassSubjectTeacher
        {
            ClassroomId = dto.ClassroomId,
            GradeSubjectId = dto.GradeSubjectId,
            StaffId = dto.StaffId,
            StartDate = DateTime.Today
        };
        _db.ClassSubjects.Add(cs);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(cs.ClassSubjectId, "معلم به درس تخصیص داده شد");
    }

    public async Task<Result> UnassignTeacherAsync(int classSubjectId)
    {
        if (await _db.Exams.AnyAsync(e => e.ClassSubjectId == classSubjectId))
            return Result.Fail("برای این درس آزمون ثبت شده. قابل حذف نیست");

        var cs = await _db.ClassSubjects.FindAsync(classSubjectId);
        if (cs is null) return Result.Fail("یافت نشد");
        _db.ClassSubjects.Remove(cs);
        await _db.SaveChangesAsync();
        return Result.Ok("معلم از این درس حذف شد");
    }
}

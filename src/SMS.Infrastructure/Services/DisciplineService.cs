using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class DisciplineService : IDisciplineService
{
    private readonly SmsDbContext _db;
    public DisciplineService(SmsDbContext db) => _db = db;

    public async Task<PagedResult<DisciplinaryRecordDto>> GetPagedAsync(int? studentId, int? classroomId, string? category, int page = 1, int pageSize = 20)
    {
        var q = _db.DisciplinaryRecords
            .Include(d => d.Student).ThenInclude(s => s.Person)
            .Include(d => d.Classroom)
            .Include(d => d.Type)
            .Include(d => d.RecordedBy).ThenInclude(s => s.Person)
            .AsNoTracking()
            .AsQueryable();

        if (studentId.HasValue) q = q.Where(d => d.StudentId == studentId);
        if (classroomId.HasValue) q = q.Where(d => d.ClassroomId == classroomId);
        if (!string.IsNullOrEmpty(category)) q = q.Where(d => d.Type.Category == category);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(d => d.RecordDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DisciplinaryRecordDto
            {
                RecordId = d.RecordId, StudentId = d.StudentId,
                StudentName = d.Student.Person.FirstName + " " + d.Student.Person.LastName,
                StudentCode = d.Student.StudentCode,
                ClassroomId = d.ClassroomId, ClassroomName = d.Classroom.Name,
                TypeId = d.TypeId, TypeName = d.Type.Name, Category = d.Type.Category,
                RecordDate = d.RecordDate, Description = d.Description,
                ActionTaken = d.ActionTaken, ScoreImpact = d.ScoreImpact,
                IsParentNotified = d.IsParentNotified,
                RecordedByName = d.RecordedBy.Person.FirstName + " " + d.RecordedBy.Person.LastName
            }).ToListAsync();

        return new PagedResult<DisciplinaryRecordDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result<long>> CreateAsync(DisciplinaryRecordCreateDto dto, int recordedByStaffId)
    {
        // پیدا کردن کلاس فعلی دانش‌آموز
        var activeYear = await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive);
        if (activeYear is null) return Result<long>.Fail("سال تحصیلی فعال یافت نشد");

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId
                && e.AcademicYearId == activeYear.AcademicYearId && e.Status == "فعال");
        if (enrollment is null) return Result<long>.Fail("دانش‌آموز در کلاس فعالی نیست");

        var type = await _db.DisciplinaryTypes.FindAsync(dto.TypeId);
        if (type is null) return Result<long>.Fail("نوع انضباطی یافت نشد");

        var record = new DisciplinaryRecord
        {
            StudentId = dto.StudentId,
            ClassroomId = enrollment.ClassroomId,
            AcademicYearId = activeYear.AcademicYearId,
            TypeId = dto.TypeId,
            RecordDate = dto.RecordDate.Date,
            Description = dto.Description,
            ActionTaken = dto.ActionTaken,
            ScoreImpact = dto.ScoreImpact ?? type.DefaultScoreImpact,
            IsParentNotified = dto.NotifyParent,
            NotifiedAt = dto.NotifyParent ? DateTime.UtcNow : null,
            RecordedByStaffId = recordedByStaffId
        };
        _db.DisciplinaryRecords.Add(record);
        await _db.SaveChangesAsync();
        return Result<long>.Ok(record.RecordId, "رکورد انضباطی ثبت شد");
    }

    public async Task<Result> DeleteAsync(long id)
    {
        var r = await _db.DisciplinaryRecords.FindAsync(id);
        if (r is null) return Result.Fail("یافت نشد");
        _db.DisciplinaryRecords.Remove(r);
        await _db.SaveChangesAsync();
        return Result.Ok("رکورد حذف شد");
    }

    public async Task<List<DisciplinaryTypeDto>> GetTypesAsync()
    {
        return await _db.DisciplinaryTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category).ThenBy(t => t.Severity)
            .Select(t => new DisciplinaryTypeDto
            {
                TypeId = t.TypeId, Name = t.Name, Category = t.Category,
                Severity = t.Severity, DefaultScoreImpact = t.DefaultScoreImpact, IsActive = t.IsActive
            }).ToListAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class ClassroomService : IClassroomService
{
    private readonly SmsDbContext _db;
    public ClassroomService(SmsDbContext db) => _db = db;

    public async Task<List<ClassroomDto>> GetBySchoolAsync(int schoolId, int? academicYearId = null)
    {
        academicYearId ??= (await _db.AcademicYears.FirstOrDefaultAsync(a => a.IsActive))?.AcademicYearId;

        return await _db.Classrooms
            .Include(c => c.School)
            .Include(c => c.AcademicYear)
            .Include(c => c.Grade)
            .Include(c => c.HeadTeacher).ThenInclude(s => s!.Person)
            .Where(c => c.SchoolId == schoolId && (!academicYearId.HasValue || c.AcademicYearId == academicYearId))
            .AsNoTracking()
            .Select(c => new ClassroomDto
            {
                ClassroomId = c.ClassroomId,
                SchoolId = c.SchoolId,
                SchoolName = c.School.Name,
                AcademicYearId = c.AcademicYearId,
                AcademicYearTitle = c.AcademicYear.Title,
                GradeId = c.GradeId,
                GradeName = c.Grade.Name,
                Name = c.Name,
                Capacity = c.Capacity,
                CurrentStudentCount = c.Enrollments.Count(e => e.Status == "فعال"),
                HeadTeacherStaffId = c.HeadTeacherStaffId,
                HeadTeacherName = c.HeadTeacher != null ? c.HeadTeacher.Person.FirstName + " " + c.HeadTeacher.Person.LastName : null,
                RoomNumber = c.RoomNumber,
                IsActive = c.IsActive
            })
            .OrderBy(c => c.GradeId).ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Result<ClassroomDto>> GetByIdAsync(int id)
    {
        var c = await _db.Classrooms
            .Include(x => x.School).Include(x => x.AcademicYear).Include(x => x.Grade)
            .Include(x => x.HeadTeacher).ThenInclude(s => s!.Person)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassroomId == id);

        if (c is null) return Result<ClassroomDto>.Fail("کلاس یافت نشد");

        return Result<ClassroomDto>.Ok(new ClassroomDto
        {
            ClassroomId = c.ClassroomId,
            SchoolId = c.SchoolId,
            SchoolName = c.School.Name,
            AcademicYearId = c.AcademicYearId,
            AcademicYearTitle = c.AcademicYear.Title,
            GradeId = c.GradeId,
            GradeName = c.Grade.Name,
            Name = c.Name,
            Capacity = c.Capacity,
            CurrentStudentCount = c.Enrollments.Count(e => e.Status == "فعال"),
            HeadTeacherStaffId = c.HeadTeacherStaffId,
            HeadTeacherName = c.HeadTeacher != null ? c.HeadTeacher.Person.FirstName + " " + c.HeadTeacher.Person.LastName : null,
            RoomNumber = c.RoomNumber,
            IsActive = c.IsActive
        });
    }

    public async Task<Result<int>> CreateAsync(ClassroomCreateDto dto)
    {
        if (await _db.Classrooms.AnyAsync(c =>
            c.SchoolId == dto.SchoolId &&
            c.AcademicYearId == dto.AcademicYearId &&
            c.GradeId == dto.GradeId &&
            c.Name == dto.Name))
            return Result<int>.Fail("کلاسی با همین مشخصات قبلاً تعریف شده است");

        var classroom = new Classroom
        {
            SchoolId = dto.SchoolId,
            AcademicYearId = dto.AcademicYearId,
            GradeId = dto.GradeId,
            MajorId = dto.MajorId,
            Name = dto.Name,
            Capacity = dto.Capacity,
            HeadTeacherStaffId = dto.HeadTeacherStaffId,
            RoomNumber = dto.RoomNumber
        };
        _db.Classrooms.Add(classroom);
        await _db.SaveChangesAsync();
        return Result<int>.Ok(classroom.ClassroomId, "کلاس ایجاد شد");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var c = await _db.Classrooms.Include(x => x.Enrollments).FirstOrDefaultAsync(x => x.ClassroomId == id);
        if (c is null) return Result.Fail("کلاس یافت نشد");
        if (c.Enrollments.Any(e => e.Status == "فعال"))
            return Result.Fail("این کلاس دانش‌آموز فعال دارد و قابل حذف نیست");

        _db.Classrooms.Remove(c);
        await _db.SaveChangesAsync();
        return Result.Ok("کلاس حذف شد");
    }
}

using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;
using SMS.Shared.Helpers;

namespace SMS.Infrastructure.Services;

public class ReportCardService : IReportCardService
{
    private readonly SmsDbContext _db;

    public ReportCardService(SmsDbContext db) => _db = db;

    public async Task<Result<StudentReportCardDto>> GenerateAsync(int studentId, int termId)
    {
        var student = await _db.Students.Include(s => s.Person)
            .AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student is null) return Result<StudentReportCardDto>.Fail("دانش‌آموز یافت نشد");

        var term = await _db.Terms.Include(t => t.AcademicYear).AsNoTracking().FirstOrDefaultAsync(t => t.TermId == termId);
        if (term is null) return Result<StudentReportCardDto>.Fail("نوبت یافت نشد");

        var enrollment = await _db.Enrollments
            .Include(e => e.Classroom).ThenInclude(c => c.School)
            .Include(e => e.Classroom).ThenInclude(c => c.Grade)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == studentId
                && e.AcademicYearId == term.AcademicYearId && e.Status == "فعال");
        if (enrollment is null) return Result<StudentReportCardDto>.Fail("ثبت‌نام فعال یافت نشد");

        var dto = new StudentReportCardDto
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.Person.FullName,
            FatherName = student.Person.FatherName,
            NationalCode = student.Person.NationalCode,
            SchoolName = enrollment.Classroom.School.Name,
            ClassroomName = enrollment.Classroom.Name,
            GradeName = enrollment.Classroom.Grade.Name,
            AcademicYearTitle = term.AcademicYear.Title,
            TermName = term.Name
        };

        // محاسبه نمره هر درس
        var classSubjects = await _db.ClassSubjects
            .Include(cs => cs.GradeSubject).ThenInclude(gs => gs.Subject)
            .Include(cs => cs.Staff).ThenInclude(s => s.Person)
            .Where(cs => cs.ClassroomId == enrollment.ClassroomId)
            .AsNoTracking().ToListAsync();

        foreach (var cs in classSubjects)
        {
            // همه آزمون‌های این درس در این ترم
            var exams = await _db.Exams
                .Where(e => e.ClassSubjectId == cs.ClassSubjectId && e.TermId == termId)
                .AsNoTracking().ToListAsync();
            if (!exams.Any()) continue;

            var examIds = exams.Select(e => e.ExamId).ToList();
            var scores = await _db.ExamScores
                .Include(s => s.DescriptiveScaleItem)
                .Where(s => examIds.Contains(s.ExamId) && s.StudentId == studentId && !s.IsAbsent && !s.IsExempt)
                .AsNoTracking().ToListAsync();

            var row = new ReportCardSubjectRow
            {
                SubjectName = cs.GradeSubject.Subject.Name,
                Coefficient = cs.GradeSubject.Coefficient,
                IsDescriptive = cs.GradeSubject.IsDescriptive,
                TeacherName = cs.Staff.Person.FullName
            };

            if (cs.GradeSubject.IsDescriptive)
            {
                // توصیفی: مد (پرتکرارترین) درجه
                var mostFrequent = scores
                    .Where(s => s.DescriptiveScaleItemId.HasValue)
                    .GroupBy(s => s.DescriptiveScaleItem!.Label)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                row.DescriptiveLabel = mostFrequent?.Key;
                row.IsPassed = mostFrequent?.Key != "نیازمند تلاش بیشتر";
            }
            else
            {
                // عددی: میانگین وزنی
                decimal totalWeighted = 0;
                decimal totalWeight = 0;
                foreach (var s in scores.Where(s => s.NumericScore.HasValue))
                {
                    var exam = exams.First(e => e.ExamId == s.ExamId);
                    // نرمال‌سازی به نمره 20
                    var normalized = exam.MaxScore == 0 ? 0 : (s.NumericScore!.Value / exam.MaxScore * 20);
                    totalWeighted += normalized * exam.Weight;
                    totalWeight += exam.Weight;
                }
                row.NumericScore = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : null;
                row.IsPassed = row.NumericScore >= cs.GradeSubject.PassingScore;
            }

            // کامنت معلم (از TermSubjectGrades)
            var tsg = await _db.TermSubjectGrades.AsNoTracking()
                .FirstOrDefaultAsync(t => t.StudentId == studentId && t.TermId == termId && t.ClassSubjectId == cs.ClassSubjectId);
            row.TeacherComment = tsg?.TeacherComment;

            dto.Subjects.Add(row);
        }

        // معدل
        var withScores = dto.Subjects.Where(s => !s.IsDescriptive && s.NumericScore.HasValue).ToList();
        if (withScores.Any())
        {
            var totalCoef = withScores.Sum(s => s.Coefficient);
            var totalScore = withScores.Sum(s => s.NumericScore!.Value * s.Coefficient);
            dto.GPA = totalCoef > 0 ? Math.Round(totalScore / totalCoef, 3) : null;
        }

        // حضور و غیاب
        var attendances = await _db.Attendances.Include(a => a.Status)
            .Where(a => a.StudentId == studentId
                && a.AttendanceDate >= term.StartDate && a.AttendanceDate <= term.EndDate)
            .AsNoTracking().ToListAsync();
        dto.TotalAbsences = attendances.Count(a => a.Status.IsAbsent);
        dto.UnexcusedAbsences = attendances.Count(a => a.Status.Code == "ABSENT");
        dto.TotalTardies = attendances.Count(a => a.Status.IsTardy);

        // انضباطی
        var discRecords = await _db.DisciplinaryRecords
            .Where(d => d.StudentId == studentId
                && d.RecordDate >= term.StartDate && d.RecordDate <= term.EndDate
                && d.ScoreImpact.HasValue)
            .AsNoTracking().ToListAsync();
        var discScore = 20 + discRecords.Sum(d => d.ScoreImpact!.Value);
        dto.DisciplineScore = Math.Max(0, Math.Min(20, discScore));

        return Result<StudentReportCardDto>.Ok(dto);
    }

    public async Task<List<StudentReportCardDto>> GenerateClassReportAsync(int classroomId, int termId)
    {
        var students = await _db.Enrollments
            .Where(e => e.ClassroomId == classroomId && e.Status == "فعال")
            .Select(e => e.StudentId).ToListAsync();

        var list = new List<StudentReportCardDto>();
        foreach (var sid in students)
        {
            var r = await GenerateAsync(sid, termId);
            if (r.Success) list.Add(r.Data!);
        }
        // رتبه‌بندی بر اساس معدل
        int rank = 1;
        foreach (var s in list.OrderByDescending(s => s.GPA ?? 0))
        {
            s.RankInClass = rank++;
        }
        return list;
    }

    public async Task<Result> CalculateClassroomGradesAsync(int classroomId, int termId)
    {
        var reports = await GenerateClassReportAsync(classroomId, termId);
        if (!reports.Any()) return Result.Fail("داده‌ای برای محاسبه نیست");

        foreach (var r in reports)
        {
            // به‌روزرسانی یا اضافه کردن StudentTermGPA
            var gpa = await _db.StudentTermGPAs
                .FirstOrDefaultAsync(s => s.StudentId == r.StudentId && s.TermId == termId);

            if (gpa is null)
            {
                _db.StudentTermGPAs.Add(new StudentTermGPA
                {
                    StudentId = r.StudentId, TermId = termId, ClassroomId = classroomId,
                    GPA = r.GPA, DisciplineScore = r.DisciplineScore,
                    RankInClass = r.RankInClass,
                    TotalAbsences = (short?)r.TotalAbsences,
                    TotalTardies = (short?)r.TotalTardies
                });
            }
            else
            {
                gpa.GPA = r.GPA; gpa.DisciplineScore = r.DisciplineScore;
                gpa.RankInClass = r.RankInClass;
                gpa.TotalAbsences = (short?)r.TotalAbsences;
                gpa.TotalTardies = (short?)r.TotalTardies;
                gpa.CalculatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
        return Result.Ok($"معدل {reports.Count} دانش‌آموز محاسبه و ذخیره شد");
    }

    public Task<byte[]> ExportToPdfAsync(StudentReportCardDto dto)
    {
        // QuestPDF License = Community در Program.cs ست شده
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Tahoma"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("کارنامه تحصیلی")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().AlignCenter().Text(dto.SchoolName).FontSize(13);
                    col.Item().AlignCenter().Text($"{dto.AcademicYearTitle} | {dto.TermName}").FontSize(11);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // اطلاعات دانش‌آموز
                    col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text($"نام: {dto.FullName}").Bold();
                        row.RelativeItem().Text($"نام پدر: {dto.FatherName ?? "-"}");
                        row.RelativeItem().Text($"کد دانش‌آموزی: {dto.StudentCode}");
                    });
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text($"پایه: {dto.GradeName}");
                        row.RelativeItem().Text($"کلاس: {dto.ClassroomName}");
                        row.RelativeItem().Text($"کد ملی: {dto.NationalCode ?? "-"}");
                    });

                    // جدول نمرات
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);  // نام درس
                            c.RelativeColumn(1);  // ضریب
                            c.RelativeColumn(2);  // نمره
                            c.RelativeColumn(1);  // قبولی
                            c.RelativeColumn(3);  // معلم
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Lighten1).Padding(5).Text("نام درس").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Lighten1).Padding(5).Text("ضریب").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Lighten1).Padding(5).Text("نمره").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Lighten1).Padding(5).Text("وضعیت").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Lighten1).Padding(5).Text("معلم").Bold().FontColor(Colors.White);
                        });

                        foreach (var s in dto.Subjects)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(s.SubjectName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(s.Coefficient.ToString("0.#"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(
                                s.IsDescriptive ? (s.DescriptiveLabel ?? "-") : (s.NumericScore?.ToString("0.##") ?? "-"));

                            var passText = s.IsPassed == true ? "قبول" : (s.IsPassed == false ? "مردود" : "-");
                            var passColor = s.IsPassed == true ? Colors.Green.Medium : (s.IsPassed == false ? Colors.Red.Medium : Colors.Grey.Medium);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(passText).FontColor(passColor);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(s.TeacherName ?? "-").FontSize(9);
                        }
                    });

                    // خلاصه
                    col.Item().PaddingTop(15).Background(Colors.Indigo.Lighten4).Padding(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"معدل کل: {(dto.GPA?.ToString("0.000") ?? "-")}").Bold().FontSize(13);
                            c.Item().Text($"نمره انضباط: {(dto.DisciplineScore?.ToString("0.##") ?? "-")}");
                            if (dto.RankInClass.HasValue)
                                c.Item().Text($"رتبه در کلاس: {dto.RankInClass}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"دروس قبول: {dto.PassedCount}").FontColor(Colors.Green.Darken1);
                            c.Item().Text($"دروس مردود: {dto.FailedCount}").FontColor(Colors.Red.Darken1);
                            c.Item().Text($"کل غیبت: {dto.TotalAbsences} ({dto.UnexcusedAbsences} غیرموجه)");
                            c.Item().Text($"کل تاخیر: {dto.TotalTardies}");
                        });
                    });

                    if (!string.IsNullOrEmpty(dto.GeneralComment))
                    {
                        col.Item().PaddingTop(10).Background(Colors.Yellow.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("توضیحات").Bold();
                            c.Item().Text(dto.GeneralComment);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("تاریخ صدور: ").FontSize(9);
                    text.Span(PersianDate.ToPersianLong(DateTime.Now)).FontSize(9);
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }
}

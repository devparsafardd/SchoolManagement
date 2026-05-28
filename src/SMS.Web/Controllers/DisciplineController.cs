using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup + "," + RoleNames.Teacher + "," + RoleNames.Counselor)]
public class DisciplineController : Controller
{
    private readonly IDisciplineService _svc;
    private readonly IStudentService _studentSvc;
    private readonly ISchoolService _schoolSvc;
    private readonly IClassroomService _classSvc;
    private readonly ICurrentUserService _currentUser;

    public DisciplineController(IDisciplineService svc, IStudentService studentSvc,
        ISchoolService schoolSvc, IClassroomService classSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _studentSvc = studentSvc;
        _schoolSvc = schoolSvc; _classSvc = classSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? schoolId, int? classroomId, string? category, int page = 1)
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        if (schoolId.HasValue)
            ViewBag.Classrooms = await _classSvc.GetBySchoolAsync(schoolId.Value);
        else
            ViewBag.Classrooms = new List<ClassroomDto>();
        ViewBag.ClassroomId = classroomId;
        ViewBag.Category = category;

        var result = await _svc.GetPagedAsync(null, classroomId, category, page, 30);
        return View(result);
    }

    public async Task<IActionResult> Create(int? studentId)
    {
        ViewBag.Types = await _svc.GetTypesAsync();
        var dto = new DisciplinaryRecordCreateDto();
        if (studentId.HasValue)
        {
            dto.StudentId = studentId.Value;
            var st = await _studentSvc.GetByIdAsync(studentId.Value);
            if (st.Success) ViewBag.StudentName = st.Data!.FullName;
        }
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DisciplinaryRecordCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Types = await _svc.GetTypesAsync();
            return View(dto);
        }

        var userId = _currentUser.UserId ?? 0;
        // پیدا کردن StaffId از روی UserId
        // برای سادگی، فرض می‌کنیم staffId = userId است
        var r = await _svc.CreateAsync(dto, userId);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            ViewBag.Types = await _svc.GetTypesAsync();
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize]
public class SurveysController : Controller
{
    private readonly ISurveyService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ICurrentUserService _currentUser;

    public SurveysController(ISurveyService svc, ISchoolService schoolSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _currentUser.UserId ?? 0;
        var canManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.SchoolAdmin)
            || User.IsInRole(RoleNames.Principal) || User.IsInRole(RoleNames.VicePrincipal);
        ViewBag.CanManage = canManage;

        var list = canManage ? await _svc.GetAllAsync() : await _svc.GetForUserAsync(userId);
        // علامت‌گذاری اون‌هایی که کاربر جواب داده
        var responded = new Dictionary<int, bool>();
        foreach (var s in list)
            responded[s.SurveyId] = await _svc.HasUserRespondedAsync(s.SurveyId, userId);
        ViewBag.Responded = responded;
        return View(list);
    }

    public async Task<IActionResult> Take(int id)
    {
        var userId = _currentUser.UserId ?? 0;
        var sr = await _svc.GetByIdAsync(id);
        if (!sr.Success) { TempData["Error"] = sr.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        if (await _svc.HasUserRespondedAsync(id, userId))
        {
            TempData["Info"] = "شما قبلاً به این نظرسنجی پاسخ داده‌اید";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Survey = sr.Data;
        var qs = await _svc.GetQuestionsAsync(id);
        return View(qs);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SurveySubmitDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        var r = await _svc.SubmitAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.ManagerGroup)]
    public IActionResult Create() => View(new SurveyCreateDto());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Create(SurveyCreateDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        // پارس کردن سوالات از فرم (ساده)
        var r = await _svc.CreateAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Results(int id)
    {
        var r = await _svc.GetResultsAsync(id);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerGroup)]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

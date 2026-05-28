using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup)]
public class NotificationsController : Controller
{
    private readonly INotificationService _svc;
    private readonly ISchoolService _schoolSvc;

    public NotificationsController(INotificationService svc, ISchoolService schoolSvc)
    {
        _svc = svc; _schoolSvc = schoolSvc;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOverdueReminder(int? schoolId)
    {
        var r = await _svc.NotifyOverduePaymentsAsync(schoolId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendUpcomingHomeworks()
    {
        var r = await _svc.NotifyUpcomingHomeworksAsync();
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAbsenceForToday()
    {
        // فراخوانی یک متد ساده برای ارسال غیبت همه دانش‌آموزای امروز
        // این رو می‌تونیم بعداً به یه Background Job منتقل کنیم
        TempData["Info"] = "این قابلیت در نسخه آینده با Background Job پیاده می‌شود.";
        return RedirectToAction(nameof(Index));
    }
}

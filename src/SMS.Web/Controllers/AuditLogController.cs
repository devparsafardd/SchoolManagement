using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Services;
using SMS.Shared.Constants;
using SMS.Shared.Helpers;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.AdminGroup)]
public class AuditLogController : Controller
{
    private readonly IAuditLogService _svc;
    public AuditLogController(IAuditLogService svc) => _svc = svc;

    public async Task<IActionResult> Index(int? userId, string? entityName, string? action,
        string? fromDate, string? toDate, int page = 1)
    {
        DateTime? from = string.IsNullOrEmpty(fromDate) ? null : PersianDate.FromPersian(fromDate);
        DateTime? to = string.IsNullOrEmpty(toDate) ? null : PersianDate.FromPersian(toDate);

        var r = await _svc.GetPagedAsync(userId, entityName, action, from, to, page, 50);
        ViewBag.EntityName = entityName;
        ViewBag.Action = action;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        return View(r);
    }

    public async Task<IActionResult> Details(long id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (r is null) return NotFound();
        return View(r);
    }
}

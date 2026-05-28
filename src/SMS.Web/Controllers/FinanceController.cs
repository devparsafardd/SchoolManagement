using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Shared.Constants;

namespace SMS.Web.Controllers;

[Authorize(Roles = RoleNames.ManagerGroup + "," + RoleNames.Accountant)]
public class FinanceController : Controller
{
    private readonly IFinanceService _svc;
    private readonly ISchoolService _schoolSvc;
    private readonly ICurrentUserService _currentUser;

    public FinanceController(IFinanceService svc, ISchoolService schoolSvc, ICurrentUserService currentUser)
    {
        _svc = svc; _schoolSvc = schoolSvc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(int? studentId, int? schoolId, string? status, int page = 1)
    {
        var r = await _svc.GetInvoicesAsync(studentId, schoolId, status, page, 30);
        ViewBag.Schools = (await _schoolSvc.GetPagedAsync(null, 1, 1000)).Items;
        ViewBag.SchoolId = schoolId;
        ViewBag.Status = status;
        ViewBag.TotalDebt = await _svc.GetTotalDebtAsync(schoolId);
        ViewBag.TotalCollected = await _svc.GetTotalCollectedAsync(schoolId);
        return View(r);
    }

    public async Task<IActionResult> FeeTypes()
    {
        return View(await _svc.GetFeeTypesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeeType(string name, bool isRecurring, decimal? defaultAmount)
    {
        var r = await _svc.CreateFeeTypeAsync(name, isRecurring, defaultAmount);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(FeeTypes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeeType(int feeTypeId)
    {
        var r = await _svc.ToggleFeeTypeAsync(feeTypeId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(FeeTypes));
    }

    public async Task<IActionResult> Create(int? studentId)
    {
        ViewBag.FeeTypes = await _svc.GetFeeTypesAsync();
        return View(new InvoiceCreateDto { StudentId = studentId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.FeeTypes = await _svc.GetFeeTypesAsync();
            return View(dto);
        }
        var r = await _svc.CreateInvoiceAsync(dto);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            ViewBag.FeeTypes = await _svc.GetFeeTypesAsync();
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Details), new { id = r.Data });
    }

    public async Task<IActionResult> Details(long id)
    {
        var r = await _svc.GetInvoiceByIdAsync(id);
        if (!r.Success) return NotFound();
        ViewBag.Payments = await _svc.GetPaymentsAsync(id);
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(PaymentCreateDto dto)
    {
        var userId = _currentUser.UserId;
        var r = await _svc.AddPaymentAsync(dto, userId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id = dto.InvoiceId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePayment(long paymentId, long invoiceId)
    {
        var r = await _svc.DeletePaymentAsync(paymentId);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Details), new { id = invoiceId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteInvoice(long id)
    {
        var r = await _svc.DeleteInvoiceAsync(id);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

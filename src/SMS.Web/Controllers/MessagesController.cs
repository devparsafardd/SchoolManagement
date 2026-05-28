using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;

namespace SMS.Web.Controllers;

/// <summary>
/// پیام‌رسان داخلی - برای همه کاربران سیستم
/// </summary>
[Authorize]
public class MessagesController : Controller
{
    private readonly IMessageService _svc;
    private readonly ICurrentUserService _currentUser;

    public MessagesController(IMessageService svc, ICurrentUserService currentUser)
    {
        _svc = svc; _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(bool unreadOnly = false)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Forbid();
        var list = await _svc.GetInboxAsync(userId.Value, unreadOnly);
        ViewBag.UnreadOnly = unreadOnly;
        return View(list);
    }

    public async Task<IActionResult> Sent()
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Forbid();
        var list = await _svc.GetSentAsync(userId.Value);
        return View(list);
    }

    public async Task<IActionResult> View(long id)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Forbid();
        var r = await _svc.GetByIdAsync(id, userId.Value);
        if (!r.Success) { TempData["Error"] = r.Errors.FirstOrDefault(); return RedirectToAction(nameof(Index)); }
        return View(r.Data);
    }

    public async Task<IActionResult> Compose(int? to, long? replyTo)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Forbid();
        ViewBag.Contacts = await _svc.GetContactsAsync(userId.Value);
        var dto = new MessageSendDto { ToUserId = to ?? 0, ReplyToMessageId = replyTo };
        if (replyTo.HasValue)
        {
            var orig = await _svc.GetByIdAsync(replyTo.Value, userId.Value);
            if (orig.Success)
            {
                dto.ToUserId = orig.Data!.FromUserId;
                dto.Subject = orig.Data.Subject?.StartsWith("Re:") == true ? orig.Data.Subject : $"Re: {orig.Data.Subject}";
            }
        }
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Compose(MessageSendDto dto)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewBag.Contacts = await _svc.GetContactsAsync(userId.Value);
            return View(dto);
        }
        var r = await _svc.SendAsync(dto, userId.Value);
        if (!r.Success)
        {
            ModelState.AddModelError("", r.Errors.FirstOrDefault() ?? "");
            ViewBag.Contacts = await _svc.GetContactsAsync(userId.Value);
            return View(dto);
        }
        TempData["Success"] = r.Message;
        return RedirectToAction(nameof(Sent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Forbid();
        var r = await _svc.DeleteAsync(id, userId.Value);
        TempData[r.Success ? "Success" : "Error"] = r.Message ?? r.Errors.FirstOrDefault();
        return RedirectToAction(nameof(Index));
    }
}

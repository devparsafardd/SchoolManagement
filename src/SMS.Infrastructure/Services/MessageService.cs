using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly SmsDbContext _db;
    public MessageService(SmsDbContext db) => _db = db;

    public async Task<MessageInboxSummary> GetInboxSummaryAsync(int userId)
    {
        var total = await _db.Messages.CountAsync(m => m.ToUserId == userId && !m.IsDeletedByReceiver);
        var unread = await _db.Messages.CountAsync(m => m.ToUserId == userId && !m.IsDeletedByReceiver && m.ReadAt == null);
        var recent = await GetInboxAsync(userId);
        return new MessageInboxSummary
        {
            TotalMessages = total,
            UnreadCount = unread,
            RecentMessages = recent.Take(5).ToList()
        };
    }

    public async Task<List<MessageDto>> GetInboxAsync(int userId, bool unreadOnly = false)
    {
        var q = _db.Messages.AsNoTracking()
            .Include(m => m.FromUser).ThenInclude(u => u.Person)
            .Where(m => m.ToUserId == userId && !m.IsDeletedByReceiver);
        if (unreadOnly) q = q.Where(m => m.ReadAt == null);
        return await q.OrderByDescending(m => m.SentAt)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                FromUserId = m.FromUserId,
                FromUserName = m.FromUser.Person.FirstName + " " + m.FromUser.Person.LastName,
                ToUserId = m.ToUserId,
                Subject = m.Subject, Body = m.Body,
                SentAt = m.SentAt, ReadAt = m.ReadAt,
                Category = m.Category, ReplyToMessageId = m.ReplyToMessageId
            }).ToListAsync();
    }

    public async Task<List<MessageDto>> GetSentAsync(int userId)
    {
        return await _db.Messages.AsNoTracking()
            .Include(m => m.ToUser).ThenInclude(u => u.Person)
            .Where(m => m.FromUserId == userId && !m.IsDeletedBySender)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                FromUserId = m.FromUserId,
                ToUserId = m.ToUserId,
                ToUserName = m.ToUser.Person.FirstName + " " + m.ToUser.Person.LastName,
                Subject = m.Subject, Body = m.Body,
                SentAt = m.SentAt, ReadAt = m.ReadAt,
                Category = m.Category, ReplyToMessageId = m.ReplyToMessageId
            }).ToListAsync();
    }

    public async Task<Result<MessageDto>> GetByIdAsync(long id, int userId)
    {
        var m = await _db.Messages.AsNoTracking()
            .Include(x => x.FromUser).ThenInclude(u => u.Person)
            .Include(x => x.ToUser).ThenInclude(u => u.Person)
            .FirstOrDefaultAsync(x => x.MessageId == id);
        if (m is null) return Result<MessageDto>.Fail("پیام یافت نشد");
        if (m.FromUserId != userId && m.ToUserId != userId) return Result<MessageDto>.Fail("دسترسی ندارید");

        // اگر گیرنده است و خوانده نشده، علامت‌گذاری
        if (m.ToUserId == userId && m.ReadAt == null)
        {
            var tracked = await _db.Messages.FirstAsync(x => x.MessageId == id);
            tracked.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            m.ReadAt = tracked.ReadAt;
        }

        return Result<MessageDto>.Ok(new MessageDto
        {
            MessageId = m.MessageId,
            FromUserId = m.FromUserId,
            FromUserName = m.FromUser.Person.FirstName + " " + m.FromUser.Person.LastName,
            ToUserId = m.ToUserId,
            ToUserName = m.ToUser.Person.FirstName + " " + m.ToUser.Person.LastName,
            Subject = m.Subject, Body = m.Body,
            SentAt = m.SentAt, ReadAt = m.ReadAt,
            Category = m.Category, ReplyToMessageId = m.ReplyToMessageId
        });
    }

    public async Task<Result<long>> SendAsync(MessageSendDto dto, int fromUserId)
    {
        if (dto.ToUserId == fromUserId) return Result<long>.Fail("نمی‌توانید به خودتان پیام بفرستید");
        var toExists = await _db.Users.AnyAsync(u => u.UserId == dto.ToUserId);
        if (!toExists) return Result<long>.Fail("کاربر گیرنده یافت نشد");

        // چک امنیتی: گیرنده باید در لیست مخاطبین مجاز فرستنده باشد
        // (اگر پاسخ به پیام است، اجازه می‌دهیم چون قبلاً ارتباط برقرار شده)
        if (!dto.ReplyToMessageId.HasValue)
        {
            var allowedContacts = await GetContactsAsync(fromUserId);
            if (!allowedContacts.Any(c => c.UserId == dto.ToUserId))
                return Result<long>.Fail("شما اجازه ارسال پیام به این کاربر را ندارید");
        }


        var m = new Message
        {
            FromUserId = fromUserId,
            ToUserId = dto.ToUserId,
            Subject = dto.Subject,
            Body = dto.Body,
            Category = dto.Category,
            ReplyToMessageId = dto.ReplyToMessageId,
            SentAt = DateTime.UtcNow
        };
        _db.Messages.Add(m);
        await _db.SaveChangesAsync();
        return Result<long>.Ok(m.MessageId, "پیام ارسال شد");
    }

    public async Task<Result> MarkAsReadAsync(long id, int userId)
    {
        var m = await _db.Messages.FirstOrDefaultAsync(x => x.MessageId == id && x.ToUserId == userId);
        if (m is null) return Result.Fail("پیام یافت نشد");
        if (m.ReadAt == null)
        {
            m.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(long id, int userId)
    {
        var m = await _db.Messages.FirstOrDefaultAsync(x => x.MessageId == id);
        if (m is null) return Result.Fail("پیام یافت نشد");
        if (m.FromUserId == userId) m.IsDeletedBySender = true;
        else if (m.ToUserId == userId) m.IsDeletedByReceiver = true;
        else return Result.Fail("دسترسی ندارید");
        await _db.SaveChangesAsync();
        return Result.Ok("پیام حذف شد");
    }

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _db.Messages.CountAsync(m => m.ToUserId == userId && !m.IsDeletedByReceiver && m.ReadAt == null);

    public async Task<List<MessageContactDto>> GetContactsAsync(int userId)
    {
        // پیدا کردن نقش‌های کاربر فعلی
        var roles = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Select(ur => ur.Role.Name).ToListAsync();

        var personId = await _db.Users.Where(u => u.UserId == userId)
            .Select(u => u.PersonId).FirstOrDefaultAsync();

        var contactUserIds = new HashSet<int>();

        // ====== SuperAdmin / SchoolAdmin: همه کاربران ======
        if (roles.Contains("SuperAdmin") || roles.Contains("SchoolAdmin"))
        {
            var allUserIds = await _db.Users.Where(u => u.UserId != userId && !u.IsLocked)
                .Select(u => u.UserId).ToListAsync();
            foreach (var id in allUserIds) contactUserIds.Add(id);
        }
        // ====== Principal / VicePrincipal: همه کاربران مدرسه خودشان ======
        else if (roles.Contains("Principal") || roles.Contains("VicePrincipal"))
        {
            var staffId = await _db.Staff.Where(s => s.PersonId == personId).Select(s => (int?)s.StaffId).FirstOrDefaultAsync();
            if (staffId.HasValue)
            {
                var schoolIds = await _db.StaffAssignments
                    .Where(a => a.StaffId == staffId && a.IsActive
                        && (a.Position == "Principal" || a.Position == "VicePrincipal"))
                    .Select(a => a.SchoolId).Distinct().ToListAsync();

                if (schoolIds.Any())
                {
                    // کارکنان همان مدرسه
                    var staffPersonIds = await _db.StaffAssignments
                        .Where(a => schoolIds.Contains(a.SchoolId) && a.IsActive)
                        .Select(a => a.Staff.PersonId).Distinct().ToListAsync();
                    var staffUserIds = await _db.Users.Where(u => staffPersonIds.Contains(u.PersonId) && u.UserId != userId)
                        .Select(u => u.UserId).ToListAsync();
                    foreach (var id in staffUserIds) contactUserIds.Add(id);

                    // دانش‌آموزان همان مدرسه
                    var studentPersonIds = await _db.Enrollments
                        .Where(e => e.Status == "فعال" && schoolIds.Contains(e.Classroom.SchoolId))
                        .Select(e => e.Student.PersonId).Distinct().ToListAsync();
                    var studentUserIds = await _db.Users.Where(u => studentPersonIds.Contains(u.PersonId))
                        .Select(u => u.UserId).ToListAsync();
                    foreach (var id in studentUserIds) contactUserIds.Add(id);

                    // ولی‌های آن دانش‌آموزان
                    var studentIds = await _db.Enrollments
                        .Where(e => e.Status == "فعال" && schoolIds.Contains(e.Classroom.SchoolId))
                        .Select(e => e.StudentId).Distinct().ToListAsync();
                    var guardianPersonIds = await _db.StudentGuardians
                        .Where(sg => studentIds.Contains(sg.StudentId))
                        .Select(sg => sg.Guardian.PersonId).Distinct().ToListAsync();
                    var guardianUserIds = await _db.Users.Where(u => guardianPersonIds.Contains(u.PersonId))
                        .Select(u => u.UserId).ToListAsync();
                    foreach (var id in guardianUserIds) contactUserIds.Add(id);
                }
            }
        }
        // ====== Teacher / Counselor: دانش‌آموزان کلاس خودش، ولی‌های آنها، مدیر/معاون مدرسه ======
        else if (roles.Contains("Teacher") || roles.Contains("Counselor"))
        {
            var staffId = await _db.Staff.Where(s => s.PersonId == personId).Select(s => (int?)s.StaffId).FirstOrDefaultAsync();
            if (staffId.HasValue)
            {
                // کلاس‌هایی که این معلم درس می‌دهد
                var classroomIds = await _db.ClassSubjects
                    .Where(cs => cs.StaffId == staffId && cs.IsActive)
                    .Select(cs => cs.ClassroomId).Distinct().ToListAsync();

                // مدرسه‌هایی که در آن‌ها تدریس می‌کند
                var schoolIds = await _db.Classrooms
                    .Where(c => classroomIds.Contains(c.ClassroomId))
                    .Select(c => c.SchoolId).Distinct().ToListAsync();

                // دانش‌آموزان آن کلاس‌ها
                var studentIds = await _db.Enrollments
                    .Where(e => classroomIds.Contains(e.ClassroomId) && e.Status == "فعال")
                    .Select(e => e.StudentId).Distinct().ToListAsync();

                var studentPersonIds = await _db.Students.Where(s => studentIds.Contains(s.StudentId))
                    .Select(s => s.PersonId).ToListAsync();
                var studentUserIds = await _db.Users.Where(u => studentPersonIds.Contains(u.PersonId))
                    .Select(u => u.UserId).ToListAsync();
                foreach (var id in studentUserIds) contactUserIds.Add(id);

                // ولی‌های آن دانش‌آموزان
                var guardianPersonIds = await _db.StudentGuardians
                    .Where(sg => studentIds.Contains(sg.StudentId))
                    .Select(sg => sg.Guardian.PersonId).Distinct().ToListAsync();
                var guardianUserIds = await _db.Users.Where(u => guardianPersonIds.Contains(u.PersonId))
                    .Select(u => u.UserId).ToListAsync();
                foreach (var id in guardianUserIds) contactUserIds.Add(id);

                // مدیر و معاون همان مدرسه‌ها
                var mgrPersonIds = await _db.StaffAssignments
                    .Where(a => schoolIds.Contains(a.SchoolId) && a.IsActive
                        && (a.Position == "Principal" || a.Position == "VicePrincipal"))
                    .Select(a => a.Staff.PersonId).Distinct().ToListAsync();
                var mgrUserIds = await _db.Users.Where(u => mgrPersonIds.Contains(u.PersonId) && u.UserId != userId)
                    .Select(u => u.UserId).ToListAsync();
                foreach (var id in mgrUserIds) contactUserIds.Add(id);
            }
        }
        // ====== Student: معلم‌های خودش + مدیر/معاون مدرسه + ولی‌های خودش ======
        else if (roles.Contains("Student"))
        {
            var studentId = await _db.Students.Where(s => s.PersonId == personId).Select(s => (int?)s.StudentId).FirstOrDefaultAsync();
            if (studentId.HasValue)
            {
                var classroomIds = await _db.Enrollments
                    .Where(e => e.StudentId == studentId && e.Status == "فعال")
                    .Select(e => e.ClassroomId).ToListAsync();

                var schoolIds = await _db.Classrooms
                    .Where(c => classroomIds.Contains(c.ClassroomId))
                    .Select(c => c.SchoolId).Distinct().ToListAsync();

                // معلم‌های کلاس‌های خودش
                var teacherStaffIds = await _db.ClassSubjects
                    .Where(cs => classroomIds.Contains(cs.ClassroomId) && cs.IsActive)
                    .Select(cs => cs.StaffId).Distinct().ToListAsync();
                // مدیر/معاون مدرسه
                var mgrStaffIds = await _db.StaffAssignments
                    .Where(a => schoolIds.Contains(a.SchoolId) && a.IsActive
                        && (a.Position == "Principal" || a.Position == "VicePrincipal" || a.Position == "Counselor"))
                    .Select(a => a.StaffId).Distinct().ToListAsync();

                var allStaffIds = teacherStaffIds.Union(mgrStaffIds).ToList();
                var staffPersonIds = await _db.Staff.Where(s => allStaffIds.Contains(s.StaffId))
                    .Select(s => s.PersonId).ToListAsync();
                var staffUserIds = await _db.Users.Where(u => staffPersonIds.Contains(u.PersonId))
                    .Select(u => u.UserId).ToListAsync();
                foreach (var id in staffUserIds) contactUserIds.Add(id);
            }
        }
        // ====== Parent: معلم‌های فرزندان + مدیر/معاون مدرسه ======
        else if (roles.Contains("Parent"))
        {
            var guardianId = await _db.Guardians.Where(g => g.PersonId == personId).Select(g => (int?)g.GuardianId).FirstOrDefaultAsync();
            if (guardianId.HasValue)
            {
                var studentIds = await _db.StudentGuardians.Where(sg => sg.GuardianId == guardianId)
                    .Select(sg => sg.StudentId).ToListAsync();
                var classroomIds = await _db.Enrollments
                    .Where(e => studentIds.Contains(e.StudentId) && e.Status == "فعال")
                    .Select(e => e.ClassroomId).Distinct().ToListAsync();
                var schoolIds = await _db.Classrooms
                    .Where(c => classroomIds.Contains(c.ClassroomId))
                    .Select(c => c.SchoolId).Distinct().ToListAsync();

                var teacherStaffIds = await _db.ClassSubjects
                    .Where(cs => classroomIds.Contains(cs.ClassroomId) && cs.IsActive)
                    .Select(cs => cs.StaffId).Distinct().ToListAsync();
                var mgrStaffIds = await _db.StaffAssignments
                    .Where(a => schoolIds.Contains(a.SchoolId) && a.IsActive
                        && (a.Position == "Principal" || a.Position == "VicePrincipal" || a.Position == "Counselor"))
                    .Select(a => a.StaffId).Distinct().ToListAsync();

                var allStaffIds = teacherStaffIds.Union(mgrStaffIds).ToList();
                var staffPersonIds = await _db.Staff.Where(s => allStaffIds.Contains(s.StaffId))
                    .Select(s => s.PersonId).ToListAsync();
                var staffUserIds = await _db.Users.Where(u => staffPersonIds.Contains(u.PersonId))
                    .Select(u => u.UserId).ToListAsync();
                foreach (var id in staffUserIds) contactUserIds.Add(id);
            }
        }

        // برگرداندن اطلاعات کاربران مجاز (با حذف تکراری از HashSet)
        var ids = contactUserIds.ToList();
        var contacts = await _db.Users.AsNoTracking()
            .Include(u => u.Person)
            .Where(u => ids.Contains(u.UserId) && !u.IsLocked)
            .Select(u => new MessageContactDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.Person.FirstName + " " + u.Person.LastName,
                Role = _db.UserRoles.Where(ur => ur.UserId == u.UserId && ur.IsActive)
                    .Select(ur => ur.Role.Name).FirstOrDefault() ?? "—",
                RoleDisplay = _db.UserRoles.Where(ur => ur.UserId == u.UserId && ur.IsActive)
                    .Select(ur => ur.Role.DisplayName).FirstOrDefault()
            })
            .OrderBy(c => c.FullName)
            .ToListAsync();
        return contacts;
    }
}

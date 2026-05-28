using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Infrastructure.Identity;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly SmsDbContext _db;
    private readonly IPasswordHasher _hasher;

    public AccountService(SmsDbContext db, IPasswordHasher hasher)
    {
        _db = db; _hasher = hasher;
    }

    public async Task<Result<ProfileDto>> GetProfileAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user is null) return Result<ProfileDto>.Fail("کاربر یافت نشد");

        return Result<ProfileDto>.Ok(new ProfileDto
        {
            PersonId = user.PersonId,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            Mobile = user.Person.Mobile,
            Email = user.Person.Email,
            Username = user.Username,
            LastLoginAt = user.LastLoginAt,
            Roles = user.UserRoles.Where(r => r.IsActive).Select(r => r.Role.DisplayName).Distinct().ToList()
        });
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return Result.Fail("کاربر یافت نشد");

        if (!_hasher.Verify(dto.CurrentPassword, user.PasswordHash))
            return Result.Fail("رمز فعلی صحیح نیست");

        user.PasswordHash = _hasher.Hash(dto.NewPassword);
        await _db.SaveChangesAsync();
        return Result.Ok("رمز عبور با موفقیت تغییر کرد");
    }

    public async Task<Result> UpdateProfileAsync(int personId, string firstName, string lastName, string? mobile, string? email)
    {
        var person = await _db.Persons.FirstOrDefaultAsync(p => p.PersonId == personId);
        if (person is null) return Result.Fail("کاربر یافت نشد");

        person.FirstName = firstName;
        person.LastName = lastName;
        person.Mobile = mobile;
        person.Email = email;
        person.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Result.Ok("پروفایل به‌روزرسانی شد");
    }

    public async Task<Result<int>> CreateAccountForPersonAsync(CreateUserAccountDto dto)
    {
        var person = await _db.Persons.FindAsync(dto.PersonId);
        if (person is null) return Result<int>.Fail("شخص یافت نشد");

        if (await _db.Users.AnyAsync(u => u.PersonId == dto.PersonId))
            return Result<int>.Fail("برای این شخص قبلاً حساب کاربری ساخته شده");

        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return Result<int>.Fail("نام کاربری تکراری است");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
        if (role is null) return Result<int>.Fail("نقش یافت نشد");

        var user = new User
        {
            PersonId = dto.PersonId,
            Username = dto.Username,
            PasswordHash = _hasher.Hash(dto.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
        await _db.SaveChangesAsync();

        return Result<int>.Ok(user.UserId, $"حساب کاربری ساخته شد. نام کاربری: {dto.Username}");
    }

    public async Task<Result<string>> ResetPasswordAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Result<string>.Fail("کاربر یافت نشد");

        // ساخت رمز رندوم
        var newPassword = $"Sms{Random.Shared.Next(10000, 99999)}";
        user.PasswordHash = _hasher.Hash(newPassword);
        user.FailedLoginCount = 0;
        user.IsLocked = false;
        await _db.SaveChangesAsync();

        return Result<string>.Ok(newPassword, $"رمز جدید: {newPassword}");
    }

    public async Task<Result> ToggleLockAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Result.Fail("کاربر یافت نشد");
        user.IsLocked = !user.IsLocked;
        if (!user.IsLocked) user.FailedLoginCount = 0;
        await _db.SaveChangesAsync();
        return Result.Ok(user.IsLocked ? "حساب قفل شد" : "حساب باز شد");
    }
}

using Microsoft.EntityFrameworkCore;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Infrastructure.Identity;
using SMS.Infrastructure.Persistence;

namespace SMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly SmsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly JwtSettings _jwtSettings;

    public AuthService(SmsDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, JwtSettings jwtSettings)
    {
        _db = db; _hasher = hasher; _jwt = jwt; _jwtSettings = jwtSettings;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user is null)
            return Result<LoginResultDto>.Fail("نام کاربری یا رمز عبور اشتباه است");

        if (user.IsLocked)
            return Result<LoginResultDto>.Fail("حساب کاربری شما قفل شده است. با مدیر سیستم تماس بگیرید");

        if (!_hasher.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5) user.IsLocked = true;
            await _db.SaveChangesAsync();
            return Result<LoginResultDto>.Fail("نام کاربری یا رمز عبور اشتباه است");
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        await _db.SaveChangesAsync();

        var roles = user.UserRoles.Where(ur => ur.IsActive)
            .Select(ur => ur.Role.Name).Distinct().ToList();

        var token = _jwt.GenerateToken(user, roles);

        return Result<LoginResultDto>.Ok(new LoginResultDto
        {
            UserId = user.UserId,
            PersonId = user.PersonId,
            Token = token,
            Username = user.Username,
            FullName = user.Person.FullName,
            Roles = roles,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        });
    }
}

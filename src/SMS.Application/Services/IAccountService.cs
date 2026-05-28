using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IAccountService
{
    Task<Result<ProfileDto>> GetProfileAsync(int userId);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<Result> UpdateProfileAsync(int personId, string firstName, string lastName, string? mobile, string? email);
    Task<Result> UpdatePhotoAsync(int personId, string photoPath);
    Task<Result> RemovePhotoAsync(int personId);

    /// <summary>ساخت حساب کاربری برای یک Person موجود (دانش‌آموز/ولی/معلم)</summary>
    Task<Result<int>> CreateAccountForPersonAsync(CreateUserAccountDto dto);

    /// <summary>ریست رمز کاربر توسط مدیر</summary>
    Task<Result<string>> ResetPasswordAsync(int userId);

    /// <summary>قفل/بازکردن کاربر</summary>
    Task<Result> ToggleLockAsync(int userId);
}

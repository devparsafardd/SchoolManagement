using SMS.Application.Common;
using SMS.Application.DTOs;

namespace SMS.Application.Services;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto);
}

namespace SMS.Application.Common;

/// <summary>
/// نتیجه استاندارد برای سرویس‌ها (موفقیت یا خطا با پیام فارسی)
/// </summary>
public class Result
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result Ok(string? message = null) => new() { Success = true, Message = message };
    public static Result Fail(string error) => new() { Success = false, Errors = { error } };
    public static Result Fail(IEnumerable<string> errors) => new() { Success = false, Errors = errors.ToList() };
}

public class Result<T> : Result
{
    public T? Data { get; set; }
    public static Result<T> Ok(T data, string? msg = null) => new() { Success = true, Data = data, Message = msg };
    public new static Result<T> Fail(string error) => new() { Success = false, Errors = { error } };
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

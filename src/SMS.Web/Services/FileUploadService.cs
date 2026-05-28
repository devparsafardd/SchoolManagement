namespace SMS.Web.Services;

/// <summary>
/// سرویس آپلود فایل با مسیردهی امن و چک extension
/// </summary>
public interface IFileUploadService
{
    /// <summary>آپلود فایل و برگشت URL نسبی برای ذخیره در DB</summary>
    Task<string?> UploadAsync(IFormFile? file, string subFolder, long maxSizeBytes = 10 * 1024 * 1024);

    bool TryDelete(string? relativePath);
}

public class FileUploadService : IFileUploadService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".rtf",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".zip", ".rar", ".7z",
        ".mp3", ".mp4", ".wav"
    };

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger)
    {
        _env = env; _logger = logger;
    }

    public async Task<string?> UploadAsync(IFormFile? file, string subFolder, long maxSizeBytes = 10 * 1024 * 1024)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > maxSizeBytes)
            throw new InvalidOperationException($"حجم فایل بیش از حد مجاز ({maxSizeBytes / 1024 / 1024} مگابایت) است");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("نوع فایل مجاز نیست");

        var safeFolder = string.Join("_", subFolder.Split(Path.GetInvalidFileNameChars()));
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", safeFolder);
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);

        using (var fs = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(fs);

        _logger.LogInformation("File uploaded: {Path} ({Size} bytes)", fullPath, file.Length);
        return $"/uploads/{safeFolder}/{fileName}";
    }

    public bool TryDelete(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return false;
        try
        {
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete file {Path}", relativePath); }
        return false;
    }
}

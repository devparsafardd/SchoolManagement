using System.Globalization;

namespace SMS.Shared.Helpers;

/// <summary>
/// ابزار تبدیل تاریخ میلادی <-> شمسی
/// </summary>
public static class PersianDate
{
    private static readonly PersianCalendar PC = new();
    private static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    private static readonly string[] DayNames =
    {
        "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"
    };

    /// <summary>تبدیل DateTime به رشته شمسی - "1404/03/15"</summary>
    public static string ToPersian(DateTime dt)
    {
        var d = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        return $"{PC.GetYear(d):0000}/{PC.GetMonth(d):00}/{PC.GetDayOfMonth(d):00}";
    }

    public static string ToPersian(DateTime? dt) => dt.HasValue ? ToPersian(dt.Value) : string.Empty;

    /// <summary>"1404/03/15 14:25"</summary>
    public static string ToPersianDateTime(DateTime dt)
    {
        var d = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        return $"{ToPersian(d)} {d:HH:mm}";
    }

    public static string ToPersianDateTime(DateTime? dt) => dt.HasValue ? ToPersianDateTime(dt.Value) : string.Empty;

    /// <summary>"شنبه ۱۵ خرداد ۱۴۰۴"</summary>
    public static string ToPersianLong(DateTime dt)
    {
        var d = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        var dayName = DayNames[(int)PC.GetDayOfWeek(d)];
        var month = MonthNames[PC.GetMonth(d) - 1];
        return $"{dayName} {PC.GetDayOfMonth(d)} {month} {PC.GetYear(d)}";
    }

    /// <summary>تبدیل رشته شمسی به DateTime - "1404/03/15" یا "1404-03-15"</summary>
    public static DateTime? FromPersian(string? persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return null;
        try
        {
            persianDate = persianDate.Trim().Replace('-', '/').Replace('.', '/');
            // تبدیل اعداد فارسی به انگلیسی
            persianDate = ToEnglishDigits(persianDate);
            var parts = persianDate.Split('/');
            if (parts.Length != 3) return null;
            int y = int.Parse(parts[0]);
            int m = int.Parse(parts[1]);
            int d = int.Parse(parts[2]);
            if (y < 1300 || y > 1500) return null;
            if (m < 1 || m > 12) return null;
            if (d < 1 || d > 31) return null;
            return PC.ToDateTime(y, m, d, 0, 0, 0, 0);
        }
        catch { return null; }
    }

    /// <summary>تبدیل اعداد فارسی/عربی به انگلیسی</summary>
    public static string ToEnglishDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
        {
            // فارسی ۰-۹ (U+06F0..U+06F9)
            if (c >= '\u06F0' && c <= '\u06F9') sb.Append((char)('0' + (c - '\u06F0')));
            // عربی ٠-٩ (U+0660..U+0669)
            else if (c >= '\u0660' && c <= '\u0669') sb.Append((char)('0' + (c - '\u0660')));
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>تبدیل اعداد انگلیسی به فارسی</summary>
    public static string ToPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
            sb.Append(c >= '0' && c <= '9' ? (char)('\u06F0' + (c - '0')) : c);
        return sb.ToString();
    }

    /// <summary>سال تحصیلی جاری بر اساس تاریخ شمسی - مثلاً "1404-1405"</summary>
    public static string GetCurrentAcademicYear()
    {
        var now = DateTime.Now;
        int year = PC.GetYear(now);
        int month = PC.GetMonth(now);
        // مهر (7) شروع سال تحصیلی
        if (month >= 7) return $"{year}-{year + 1}";
        return $"{year - 1}-{year}";
    }
}

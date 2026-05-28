namespace SMS.Shared.Helpers;

/// <summary>
/// اعتبارسنج‌های مخصوص ایران (کد ملی، شماره موبایل، شبا، ...)
/// </summary>
public static class IranianValidators
{
    /// <summary>اعتبارسنجی کد ملی ۱۰ رقمی ایران</summary>
    public static bool IsValidNationalCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = PersianDate.ToEnglishDigits(code.Trim());
        if (code.Length != 10 || !code.All(char.IsDigit)) return false;
        // همه ارقام یکسان (مثل 0000000000) نباشد
        if (code.Distinct().Count() == 1) return false;

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);
        int check = sum % 11;
        int control = code[9] - '0';
        return (check < 2 && check == control) || (check >= 2 && control == 11 - check);
    }

    /// <summary>اعتبارسنجی شماره موبایل ایران (09xxxxxxxxx)</summary>
    public static bool IsValidMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return false;
        mobile = PersianDate.ToEnglishDigits(mobile.Trim());
        return System.Text.RegularExpressions.Regex.IsMatch(mobile, @"^09\d{9}$");
    }

    /// <summary>نرمال‌سازی شماره موبایل (پاک کردن +98 و فاصله‌ها)</summary>
    public static string? NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return null;
        mobile = PersianDate.ToEnglishDigits(mobile)
            .Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (mobile.StartsWith("+98")) mobile = "0" + mobile[3..];
        else if (mobile.StartsWith("0098")) mobile = "0" + mobile[4..];
        else if (mobile.StartsWith("98") && mobile.Length == 12) mobile = "0" + mobile[2..];
        return mobile;
    }

    /// <summary>اعتبارسنجی شبا (IR + 24 رقم)</summary>
    public static bool IsValidIBAN(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return false;
        iban = iban.Replace(" ", "").ToUpper();
        return System.Text.RegularExpressions.Regex.IsMatch(iban, @"^IR\d{24}$");
    }
}

namespace SMS.Application.Common;

public class JwtSettings
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "SMS";
    public string Audience { get; set; } = "SMS-Users";
    public int ExpiryMinutes { get; set; } = 120;
}

# 🎓 سیستم مدیریت مدارس - ASP.NET Core 8

## 📁 ساختار Solution

```
SchoolManagement/
├── SchoolManagement.sln
└── src/
    ├── SMS.Domain          → موجودیت‌ها (Entities)
    ├── SMS.Application     → DTO ها، Validators، Interface سرویس‌ها
    ├── SMS.Infrastructure  → EF Core، DbContext، پیاده‌سازی سرویس‌ها
    ├── SMS.Shared          → ثابت‌ها و موارد مشترک
    ├── SMS.Web             → پنل MVC با Cookie Auth (مدیریت)
    └── SMS.Api             → Web API با JWT (موبایل/SPA)
```

## 🏗️ معماری Clean (لایه‌ای)

```
SMS.Web ────┐
            ├──► SMS.Application ──► SMS.Domain
SMS.Api ────┤            ▲
            │            │
            └──► SMS.Infrastructure (پیاده‌سازی)
```

- **Domain**: قلب کسب‌وکار، بدون وابستگی
- **Application**: قواعد کسب‌وکار، DTO و Interface
- **Infrastructure**: EF Core، JWT، BCrypt و سایر فریم‌ورک‌ها
- **Web/Api**: لایه نمایش (Presentation)

---

## 🚀 راه‌اندازی

### پیش‌نیاز
- .NET 8 SDK
- SQL Server (LocalDB یا Express هم کافیه)
- Visual Studio 2022 یا VS Code

### مرحله ۱: ساخت دیتابیس
دیتابیس از طریق Migration به‌صورت خودکار ساخته می‌شه. ولی اگه از اسکریپت SQL (فایل قبلی) استفاده می‌کنید، در `appsettings.json` کانکشن استرینگ رو تنظیم کنید:

```json
"DefaultConnection": "Server=.;Database=SchoolManagement;Trusted_Connection=True;TrustServerCertificate=True"
```

### مرحله ۲: ساخت Migration اولیه
```bash
cd src/SMS.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../SMS.Web
dotnet ef database update --startup-project ../SMS.Web
```

### مرحله ۳: اجرای پنل وب
```bash
cd src/SMS.Web
dotnet run
```
آدرس: https://localhost:5001  
**کاربر پیش‌فرض**: `admin` / `Admin@123`

### مرحله ۴: اجرای API
```bash
cd src/SMS.Api
dotnet run
```
Swagger UI: https://localhost:7001/swagger

---

## 📦 پکیج‌های اصلی

| پکیج | نقش |
|------|-----|
| Microsoft.EntityFrameworkCore.SqlServer | EF Core برای SQL Server |
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT برای API |
| BCrypt.Net-Next | هش امن پسورد |
| FluentValidation | اعتبارسنجی DTO |
| Dapper | برای کوئری‌های سنگین (در آینده) |
| Swashbuckle.AspNetCore | Swagger UI |

---

## ✅ قابلیت‌های فعلی (MVP)

| ماژول | پنل وب | API |
|-------|--------|-----|
| ورود / خروج | ✅ Cookie | ✅ JWT |
| مدارس (CRUD) | ✅ | ✅ |
| کلاس‌ها (CRUD) | ✅ | ✅ |
| دانش‌آموزان (CRUD) | ✅ | ✅ |
| ثبت‌نام دانش‌آموز | ✅ | ✅ |
| Lookup ها | ✅ | ✅ |
| کنترل دسترسی نقش‌محور | ✅ | ✅ |

---

## 🔮 ماژول‌های آینده (فاز ۲)

- 👨‍🏫 معلمان و تخصیص به کلاس/درس
- ✅ حضور و غیاب (روزانه/زنگی)
- 📝 آزمون و نمره (با پشتیبانی توصیفی/عددی)
- 💰 شهریه و پرداخت‌ها
- 📊 کارنامه و گزارش
- 📲 SMS و پیام
- 👨‍👩‍👧 پنل اولیا

---

## 🧪 تست API با curl

### ورود
```bash
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```
پاسخ:
```json
{
  "token": "eyJhbGc...",
  "username": "admin",
  "fullName": "مدیر سیستم",
  "roles": ["SuperAdmin"]
}
```

### دریافت لیست مدارس
```bash
curl https://localhost:7001/api/schools \
  -H "Authorization: Bearer eyJhbGc..."
```

---

## 🎯 نکات مهم برای تیم برنامه‌نویس

1. **هر سرویس جدید**: یک Interface در `SMS.Application/Services` + پیاده‌سازی در `SMS.Infrastructure/Services` + ثبت در `DependencyInjection.cs`
2. **هر DTO جدید**: داخل `SMS.Application/DTOs` با Validator
3. **هر Entity جدید**: داخل `SMS.Domain/Entities` + اضافه به `SmsDbContext`
4. **پنل و API** هر دو از یک سرویس استفاده می‌کنند → DRY

## ⚠️ قبل از Production
- [ ] تغییر `JwtSettings:Secret` به یک کلید قوی
- [ ] HTTPS اجباری
- [ ] Rate Limiting روی API
- [ ] لاگ‌گیری کامل (Serilog توصیه می‌شود)
- [ ] Backup خودکار دیتابیس
- [ ] Connection Pooling
- [ ] Index Compression در SQL Server

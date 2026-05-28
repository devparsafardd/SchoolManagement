# 🚀 راهنمای ارتقاء به فاز ۰

## ✨ تغییرات این آپدیت:
- ✅ AdminLTE 4 - پنل حرفه‌ای فارسی
- ✅ تاریخ شمسی کامل
- ✅ Soft Delete سراسری
- ✅ Audit Log خودکار (ثبت همه تغییرات)
- ✅ Serilog (لاگ‌گیری در فایل)
- ✅ Export Excel
- ✅ سرویس پیامک (Interface آماده)
- ✅ Exception Handler سراسری
- ✅ Profile و Change Password

## 📋 قدم‌های اجرا:

### قدم ۱: کپی فایل‌های جدید
محتوای ZIP جدید (`SchoolManagement-Source.zip`) را روی پروژه فعلی **جایگزین کن** (Replace).

### قدم ۲: Restore پکیج‌های جدید
در Visual Studio:
- راست کلیک روی Solution → **Restore NuGet Packages**

### قدم ۳: حذف Migration قبلی و Database
چون موجودیت‌های جدید (AuditLog) اضافه شده و فیلدهای Soft Delete به entityها اضافه شده، باید Migration رو رفرش کنیم.

#### راه آسان: حذف کل دیتابیس و ساخت دوباره (پیشنهاد می‌شود برای محیط Dev)

در SSMS:
```sql
DROP DATABASE SchoolManagement;
```

سپس در Visual Studio، **پوشه `Migrations` در `SMS.Infrastructure`** را پاک کن.

در Package Manager Console:
```powershell
Add-Migration InitialCreate -StartupProject SMS.Web
Update-Database -StartupProject SMS.Web
```

### قدم ۴: اجرا
F5 بزن و لذت ببر! 🎉

---

## 🆕 امکانات جدید:

### 1. Audit Log
هر تغییری در دیتابیس به‌صورت خودکار در جدول `AuditLogs` ثبت می‌شود:
- چه کاربری
- چه عملیاتی (Insert/Update/Delete)
- چه فیلدهایی تغییر کردند
- مقادیر قبلی و جدید
- IP و User Agent

### 2. Soft Delete
وقتی موجودیتی `ISoftDeletable` رو implement کنه، با Remove() دیگه واقعاً حذف نمی‌شه، بلکه `IsDeleted = true` می‌شه و در کوئری‌های معمولی نشون داده نمی‌شه.

### 3. تاریخ شمسی
از کلاس `PersianDate` استفاده کن:
```csharp
@PersianDate.ToPersian(DateTime.Now)         // "1404/03/15"
@PersianDate.ToPersianLong(DateTime.Now)     // "شنبه ۱۵ خرداد ۱۴۰۴"
@PersianDate.ToPersianDigits("12345")        // "۱۲۳۴۵"
```

### 4. Excel Export
هر کنترلر می‌تونه از `IExcelExporter` استفاده کنه (نمونه: SchoolsController.ExportExcel)

### 5. لاگ Serilog
فایل‌های لاگ در پوشه `Logs/` پروژه ساخته می‌شن.

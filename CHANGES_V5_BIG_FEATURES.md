# 🚀 گزارش نسخه v5 — افزودنی‌های بزرگ

این نسخه شامل **۸ فاز** ارتقا با امکانات کاملاً جدید است.

---

## ⚠️ ابتدا: Migration اجباری

دو موجودیت جدید (`CalendarEvent`, `Survey`, `SurveyQuestion`, `SurveyAnswer`) اضافه شدن:

```powershell
cd src/SMS.Web
dotnet ef migrations add AddCalendarSurveyFeatures --project ../SMS.Infrastructure --startup-project .
dotnet ef database update --project ../SMS.Infrastructure --startup-project .
```

---

## ✨ فاز ۱: آپلود عکس پروفایل

### امکانات
- آپلود عکس JPG/PNG/GIF/WebP در صفحه پروفایل
- نمایش عکس به‌جای initials در topbar همه پنل‌ها
- حذف عکس
- محدودیت ۳MB حجم
- ذخیره در `wwwroot/uploads/profiles/`

### فایل‌های جدید/تغییریافته
- `Views/Shared/_UserAvatar.cshtml` (Partial View با کش)
- `Views/Account/Profile.cshtml` (بازنویسی با کارت عکس)
- `Controllers/AccountController.cs` (Actions: UploadPhoto, RemovePhoto)
- `Services/IAccountService.cs` (UpdatePhotoAsync, RemovePhotoAsync)
- `DTOs/AccountDtos.cs` (PhotoPath در ProfileDto)
- همه ۵ Layout — استفاده از `_UserAvatar`

---

## 📲 فاز ۲: پیامک خودکار + پیام داخلی

### امکانات
سرویس `NotificationService` خودکار پیامک + پیام داخلی می‌فرسته:

| رویداد | به‌صورت خودکار اتفاق می‌افته در... |
|---------|------------------------------------|
| 🔴 غیبت | هنگام `Save Attendance` (در `AttendanceService`) |
| 💰 صدور فاکتور | هنگام `CreateInvoice` (در `FinanceService`) |
| ✅ ثبت پرداخت | هنگام `AddPayment` |
| 📝 نهایی شدن نمره | هنگام `FinalizeExam` |
| ⭐ ثبت انضباطی | هنگام `Create` در `DisciplineService` |

### کنترل از طریق تنظیمات
کلیدهای زیر در Settings:
- `AutoNotifyAbsence` (پیش‌فرض: true)
- `AutoNotifyInvoice` (پیش‌فرض: true)
- `AutoNotifyPayment` (پیش‌فرض: true)
- `AutoNotifyDiscipline` (پیش‌فرض: true)
- `AutoNotifyExamScore` (پیش‌فرض: false)

### پنل مدیریت
صفحه `/Notifications` با دکمه‌های دستی:
- 📤 یادآوری دسته‌جمعی معوقات
- 📤 یادآوری تکالیف فردا

### فایل‌های جدید
- `Application/Services/INotificationService.cs`
- `Infrastructure/Services/NotificationService.cs`
- `Web/Controllers/NotificationsController.cs`
- `Web/Views/Notifications/Index.cshtml`

---

## 📊 فاز ۳: ایمپورت دسته‌جمعی Excel

### امکانات
- **قالب آماده** برای دانش‌آموزان (۱۶ ستون با راهنما)
- **قالب آماده** برای معلمان (۱۷ ستون)
- آپلود فایل پر شده → ساخت دسته‌جمعی Person + Student/Staff + User + Role + Enrollment
- گزارش کامل خطاها (ردیف به ردیف)

### فایل‌های جدید
- `DTOs/BulkImportDtos.cs`
- `Services/IBulkImportService.cs`
- `Infrastructure/Services/BulkImportService.cs` (~۲۸۰ خط)
- `Web/Controllers/BulkImportController.cs`
- `Web/Views/BulkImport/Index.cshtml`

### مسیر: منوی "📊 ایمپورت Excel" در پنل ادمین

---

## 📅 فاز ۴: تقویم آموزشی و رویدادها

### امکانات
- ثبت رویدادهای مدرسه (تعطیلی، آزمون، اردو، جلسه، مراسم)
- مخاطب: همه/دانش‌آموزان/اولیا/معلمان
- محدوده: سراسری / یک مدرسه / یک کلاس
- نمایش ماهانه + لیست ۳۰ روز پیش رو
- نمایش "امروز" و "فردا" با badge قرمز/نارنجی

### مسیر: منوی "📅 تقویم آموزشی"

---

## 📊 فاز ۷: سیستم نظرسنجی/فرم

### امکانات
- ساخت نظرسنجی با مخاطب مشخص (همه/دانش‌آموز/ولی/معلم)
- **۵ نوع سوال**: متنی، بله/خیر، امتیاز ۱-۵، تک‌گزینه‌ای، چندگزینه‌ای
- نظرسنجی ناشناس یا با هویت
- جلوگیری از پاسخ تکراری
- **گزارش نتایج** با Progress Bar و توزیع درصدی

### مسیر: منوی "📊 نظرسنجی‌ها"

---

## 👨‍👩‍👧 فاز ۵: داشبورد ولی پیشرفته‌تر

- دکمه "📊 گزارش جامع و نمودارها" روی هر کارت فرزند
- ولی می‌تونه گزارش کامل فرزندش رو با نمودار پیشرفت ببینه
- چک امنیتی: ولی فقط فرزندان خودش رو می‌تونه ببینه

---

## 📄 فاز ۸: PDF فارسی استاندارد

### امکانات
- سرویس عمومی `IPdfExporter` (با QuestPDF)
- پشتیبانی از **فونت Vazirmatn** (اگر فایل فونت موجود باشه)
- خروجی PDF برای: لیست دانش‌آموز، لیست معلم، گزارش کلاس

### نحوه فعال‌سازی فونت فارسی
1. از https://fonts.google.com/specimen/Vazirmatn فایل‌ها رو دانلود کنید
2. `Vazirmatn-Regular.ttf` و `Vazirmatn-Bold.ttf` رو در `src/SMS.Web/wwwroot/fonts/` بذارید
3. اپ رو ریستارت کنید

اگر فونت نباشه، fallback به Tahoma میشه.

---

## 📋 فایل‌های جدید (۲۰+)

### Domain
- `CalendarEvent.cs`
- `Survey.cs` (Survey, SurveyQuestion, SurveyAnswer)

### Application DTOs
- `CalendarDtos.cs`
- `SurveyDtos.cs`
- `BulkImportDtos.cs`

### Application Services
- `ICalendarService.cs`
- `ISurveyService.cs` (در همون فایل ICalendarService)
- `IBulkImportService.cs`
- `INotificationService.cs`

### Common
- `Common/Export/IPdfExporter.cs`

### Infrastructure
- `CalendarService.cs`
- `SurveyService.cs`
- `BulkImportService.cs`
- `NotificationService.cs`
- `Services/Export/PdfExporter.cs`
- `Services/FileUploadService.cs` (از قبل بود)

### Web/Controllers
- `BulkImportController.cs`
- `CalendarController.cs`
- `NotificationsController.cs`
- `SurveysController.cs`

### Web/Views
- `BulkImport/Index.cshtml`
- `Calendar/Index.cshtml`, `Create.cshtml`
- `Notifications/Index.cshtml`
- `Surveys/Index.cshtml`, `Create.cshtml`, `Take.cshtml`, `Results.cshtml`
- `Shared/_UserAvatar.cshtml`
- `Account/Profile.cshtml` (بازنویسی)

### فایل‌های ویرایش شده
- ۵ Layout (اضافه شدن منوهای جدید + UserAvatar)
- `Program.cs` (Session)
- `SmsDbContext.cs` (4 DbSet جدید + Relations)
- `DependencyInjection.cs` (ثبت ۶ سرویس جدید)
- `AccountController.cs` (آپلود عکس)
- `AccountService.cs` + `AccountDtos.cs`
- `AttendanceService.cs` (Hook نوتیفیکیشن)
- `FinanceService.cs` (Hook نوتیفیکیشن)
- `ExamService.cs` (Hook نوتیفیکیشن)
- `DisciplineService.cs` (Hook نوتیفیکیشن)
- `ExportController.cs` (Action های PDF)
- `AnalyticsController.cs` (دسترسی ولی به گزارش فرزند)
- `Views/Parent/Dashboard.cshtml` (دکمه گزارش جامع)
- `Views/Students/Index.cshtml`, `Staff/Index.cshtml`, `Analytics/Classroom.cshtml` (دکمه PDF)
- `Views/Announcements/Index.cshtml` (شرطی برای دکمه‌های مدیر)

---

## 🧪 سناریوهای تست

### آپلود عکس
1. به `/Account/Profile` برو
2. عکس انتخاب کن → خودکار آپلود می‌شه
3. عکس بالای صفحه (در topbar) آپدیت می‌شه

### پیامک خودکار غیبت
1. در Settings کلید `AutoNotifyAbsence` رو `true` کن
2. با `teacher1` لاگین کن → یه کلاس انتخاب کن → حضور و غیاب
3. یه نفر رو غایب علامت بزن → ذخیره
4. در `/Sms/Index` می‌بینی پیامک ارسال شده

### Bulk Import
1. به `/BulkImport` برو
2. دکمه "دانلود قالب نمونه" → فایل Excel دانلود
3. پر کن و آپلود کن

### نظرسنجی
1. با ادمین برو `/Surveys/Create`
2. سوالات مختلف اضافه کن
3. با کاربر دیگری لاگین کن → نظرسنجی رو ببین و پاسخ بده
4. با ادمین دکمه "نتایج" رو بزن

### تقویم
1. به `/Calendar` برو
2. مدیر "افزودن رویداد" کلیک کنه

### PDF
1. لیست دانش‌آموزان → دکمه 📄 PDF (در tab جدید باز می‌شه)
2. برای کارنامه از مسیر MyPortal → کارنامه → دکمه PDF (فونت Vazirmatn اگر نصب باشه)

---

## 🚀 اجرا

```powershell
# اول Migration
cd src/SMS.Web
dotnet ef migrations add AddCalendarSurveyFeatures --project ../SMS.Infrastructure --startup-project .
dotnet ef database update --project ../SMS.Infrastructure --startup-project .

# بعد اجرا
dotnet run
```

**حتماً Ctrl+F5 توی مرورگر بزن** که کش پاک بشه.

---

## 🎯 برای فاز بعدی پیشنهاد:

- 🛡️ **امنیت کامل** (Account Lockout، Strong Password، Rate Limiting)
- ⚡ **SignalR** (نوتیفیکیشن لحظه‌ای + پیام Real-time)
- 🌐 **چندزبانه** (فارسی/انگلیسی/عربی)
- 🎨 **Dark Mode**
- 📱 **اپلیکیشن موبایل** (با همین API)

# 🎨 گزارش نسخه پولیش v3

## ✅ مشکلات اصلاح‌شده

### 1️⃣ نشت پنل ادمین در پنل‌های دیگه
**ریشه:** وقتی کاربر non-admin (مثل Principal) به `/Home/Index` می‌رفت، محتوای داشبورد ادمین با Layout بنفش نمایش داده می‌شد.

**حل:** `HomeController.Index` حالا کاربر non-admin رو **خودکار به پنل خودش redirect می‌کنه**.

### 2️⃣ متن "پیش‌نویس" → "قابل ویرایش"
کلمه پیش‌نویس گیج‌کننده بود. در همه جا به "قابل ویرایش" تغییر کرد.

### 3️⃣ امکان بازگشایی آزمون نهایی شده
**جواب سوال شما:** نهایی‌سازی برای جلوگیری از دستکاری نمراست، ولی الان اضافه شد:
- معلم نمی‌تونه نمره نهایی رو تغییر بده ✅ (سیاست امنیتی)
- **مدیر مدرسه** و **ادمین** حالا می‌تونن دکمه **🔓 بازگشایی** رو بزنن تا قفل برداشته بشه و معلم بتونه ویرایش کنه
- این دکمه فقط در صفحه نمرات (Scores) برای نقش `SuperAdmin/SchoolAdmin/Principal` نمایش داده می‌شه

---

## 🆕 امکانات جدید

### 4️⃣ DataTables.net حرفه‌ای + خروجی Excel/CSV/Print
**فایل کلیدی:** `Views/Shared/_AdvancedScripts.cshtml`

یک Partial View مشترک ساخته شد که شامل:
- jQuery
- **DataTables 2.x** با ترجمه کامل فارسی
- Buttons: 📊 Excel، 📄 CSV، 🖨 Print (سرور-ساید)
- **Persian DatePicker** (تقویم شمسی)

اعمال شده روی:
- ✅ لیست دانش‌آموزان (Students/Index)
- ✅ لیست معلمان (Staff/Index)
- ✅ لیست کلاس‌ها (Classrooms/Index)

ویژگی‌ها:
- جستجوی زنده در همه ستون‌ها
- مرتب‌سازی روی هر ستون
- خروجی Excel/CSV/Print با یک کلیک
- ستون "عملیات" به‌صورت خودکار از خروجی حذف میشه (با کلاس `no-export`)

### 5️⃣ سرویس آپلود فایل واقعی
**فایل‌های جدید:**
- `Web/Services/FileUploadService.cs` - سرویس امن آپلود
- ثبت در `Program.cs`

**ویژگی‌ها:**
- چک extension (فقط فایل‌های مجاز: PDF, Office, تصویر, ZIP, ...)
- چک حجم (پیش‌فرض ۱۰ مگابایت)
- نام‌گذاری امن با GUID
- پوشه‌بندی خودکار `wwwroot/uploads/<context>/`
- پشتیبانی از حذف فایل

**اعمال در:**
- ✅ ایجاد تکلیف توسط معلم (فایل ضمیمه)
- ✅ تحویل تکلیف توسط دانش‌آموز (فایل پاسخ)

### 6️⃣ ExportController جامع
**فایل جدید:** `Controllers/ExportController.cs`

خروجی Excel/PDF برای:
- 📊 لیست دانش‌آموزان (با فیلتر)
- 📊 لیست معلمان
- 📊 لیست کلاس‌ها
- 📊 گزارش حضور و غیاب یک کلاس
- 📊 گزارش جامع یک کلاس
- 📊 گزارش جامع یک دانش‌آموز
- 📄 کارنامه PDF
- 📊 کارنامه Excel

### 7️⃣ Persian DatePicker در همه فرم‌ها
ورودی‌های تاریخ به تقویم شمسی تبدیل شدن در:
- ✅ ایجاد آزمون (Teacher + Admin)
- ✅ ایجاد تکلیف
- ✅ ثبت حضور و غیاب
- ✅ گزارش حضور و غیاب
- ✅ گزارش‌های Analytics (حضور، انضباطی، مالی)
- ✅ پنل ولی/دانش‌آموز - گزارش حضور
- ✅ AuditLog

### 8️⃣ دکمه پرینت/PDF برای برنامه هفتگی
- ✅ `Teacher/Schedule` - دکمه 🖨 پرینت / ذخیره PDF
- ✅ `Schedules/Classroom` - دکمه 🖨 پرینت / PDF
- CSS مخصوص چاپ که نوار کناری و دکمه‌ها مخفی می‌شن

### 9️⃣ دکمه پرینت در صفحات گزارش
- ✅ گزارش جامع کلاس
- ✅ گزارش جامع دانش‌آموز
- ✅ کارنامه

---

## 📋 فایل‌های تغییریافته

### جدید (3 فایل):
1. `src/SMS.Web/Views/Shared/_AdvancedScripts.cshtml`
2. `src/SMS.Web/Services/FileUploadService.cs`
3. `src/SMS.Web/Controllers/ExportController.cs`

### تغییریافته:
- `src/SMS.Web/Program.cs` - ثبت FileUploadService
- `src/SMS.Web/Controllers/HomeController.cs` - redirect نقش‌محور
- `src/SMS.Web/Controllers/HomeworksController.cs` - آپلود فایل
- `src/SMS.Web/Controllers/ExamsController.cs` - Action Unfinalize
- `src/SMS.Application/Services/IStaffService.cs` - UnfinalizeAsync
- `src/SMS.Infrastructure/Services/ExamService.cs` - UnfinalizeAsync
- `src/SMS.Web/Views/Principal/SelectSchool.cshtml` - حذف Layout صریح
- `src/SMS.Web/Views/Principal/NoAccess.cshtml` - حذف Layout صریح
- `src/SMS.Web/Views/Teacher/NoAccess.cshtml` - حذف Layout صریح
- `src/SMS.Web/Views/Exams/Index.cshtml` - متن "قابل ویرایش"
- `src/SMS.Web/Views/Exams/Scores.cshtml` - دکمه بازگشایی
- `src/SMS.Web/Views/Exams/Create.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/Teacher/Exams.cshtml` - متن "قابل ویرایش"
- `src/SMS.Web/Views/Teacher/CreateExam.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/Teacher/Schedule.cshtml` - دکمه پرینت
- `src/SMS.Web/Views/Teacher/Attendance.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/Schedules/Classroom.cshtml` - دکمه پرینت
- `src/SMS.Web/Views/Homeworks/Create.cshtml` - آپلود فایل + DatePicker
- `src/SMS.Web/Views/Homeworks/Submit.cshtml` - آپلود فایل
- `src/SMS.Web/Views/Students/Index.cshtml` - DataTables + Excel
- `src/SMS.Web/Views/Staff/Index.cshtml` - DataTables + Excel
- `src/SMS.Web/Views/Classrooms/Index.cshtml` - DataTables + Excel
- `src/SMS.Web/Views/Attendance/Take.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/Attendance/Report.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/Analytics/*.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/Analytics/Classroom.cshtml` - دکمه Excel + Print
- `src/SMS.Web/Views/Analytics/Student.cshtml` - دکمه Excel + Print
- `src/SMS.Web/Views/MyPortal/Attendance.cshtml` - Persian DatePicker
- `src/SMS.Web/Views/MyPortal/ReportCard.cshtml` - دکمه Excel + Print
- `src/SMS.Web/Views/Reports/Student.cshtml` - دکمه Excel
- `src/SMS.Web/Views/AuditLog/Index.cshtml` - Persian DatePicker

---

## 🚀 برای استفاده

نیازی به Migration نیست. **پوشه `wwwroot/uploads`** به‌صورت خودکار ساخته میشه.

```powershell
dotnet run
```

## 💡 نکات تست

### تست آپلود فایل:
1. با `teacher1` لاگین کن → یه کلاس انتخاب کن → "تکلیف جدید"
2. فایل ضمیمه آپلود کن
3. با `student_1_1` (یا یکی از دانش‌آموزای اون کلاس) لاگین کن
4. تکلیف رو دانلود و فایل پاسخ آپلود کن

### تست بازگشایی نمره:
1. با `teacher1` آزمونی بساز و نمره وارد کن
2. روی "🔒 نهایی‌سازی" کلیک کن
3. حالا با `principal1` (یا `admin`) به همون آزمون برو
4. دکمه "🔓 بازگشایی" رو می‌بینی → کلیک کن
5. حالا معلم می‌تونه دوباره ویرایش کنه

### تست DataTables:
1. به Students/Index برو
2. سرچ، مرتب‌سازی، خروجی Excel و Print امتحان کن

### تست Persian DatePicker:
1. ایجاد آزمون جدید → روی فیلد تاریخ کلیک کن → تقویم شمسی نمایش داده میشه

### تست پرینت:
1. `Teacher/Schedule` → دکمه پرینت → پنجره پرینت مرورگر باز میشه
2. می‌تونی "Save as PDF" بزنی

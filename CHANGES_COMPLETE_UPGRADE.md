# 📋 گزارش کامل ارتقای سیستم مدیریت مدارس

> همه فازهای ۱، ۲ و ۳ کامل شد. تاریخ: 1404/03/07

---

## 📊 خلاصه آماری

- **۸ Entity جدید** اضافه شد
- **۲۰+ DTO جدید** ساخته شد
- **۶ سرویس بزرگ جدید** (TeacherPortal، PrincipalPortal، Analytics، Homework، Message، Schedule)
- **۶ کنترلر جدید**
- **۲۰+ View جدید** + ۲ Layout جدید (مدیر، ولی)
- **۳ Layout موجود** آپدیت شد (افزودن منو پیام و گزارش)
- **AccountController** برای ریدایرکت خودکار نقش‌محور آپدیت شد
- **SmsDbContext** و **DependencyInjection** برای ثبت‌های جدید آپدیت شدند

---

## ✨ فاز ۱: تکمیل پنل‌ها

### ✅ فاز ۱.۱ — پنل معلم (Teacher Panel)
**رنگ Layout:** سبز 🟢

#### فایل‌های جدید:
- `src/SMS.Domain/Entities/ClassSchedule.cs` — موجودیت‌های `SchoolPeriod` و `ClassSchedule`
- `src/SMS.Application/DTOs/TeacherPortalDtos.cs` — ۱۰ DTO
- `src/SMS.Application/Services/ITeacherPortalService.cs`
- `src/SMS.Infrastructure/Services/TeacherPortalService.cs` — با Row-Level Security
- `src/SMS.Web/Controllers/TeacherController.cs` — ۱۲ Action
- `src/SMS.Web/Views/Shared/_TeacherLayout.cshtml`
- `src/SMS.Web/Views/Teacher/`:
  - `_ViewStart.cshtml`, `Dashboard.cshtml`, `MyClasses.cshtml`, `ClassDetail.cshtml`,
    `Schedule.cshtml`, `Students.cshtml`, `Attendance.cshtml`, `Exams.cshtml`,
    `CreateExam.cshtml`, `ExamScores.cshtml`, `NoAccess.cshtml`

#### ویژگی‌ها:
- داشبورد با ۴ کارت آمار: کلاس‌ها، دانش‌آموزان، کلاس امروز، آزمون‌های پیش رو
- کلاس‌های امروز با وضعیت ثبت حضور
- جزئیات هر کلاس با: لیست دانش‌آموزان، میانگین، غیبت، تشویق/تنبیه، آزمون‌های اخیر
- ثبت حضور و غیاب
- ایجاد آزمون و ورود نمرات (عددی و توصیفی)
- لیست تکالیف (در منو)

#### امنیت:
- `OwnsClassSubjectAsync` و `OwnsClassroomAsync` چک می‌کنند
- معلم به کلاس کس دیگه دسترسی نداره → 403

---

### ✅ فاز ۱.۲ — پنل مدیر مدرسه (Principal Panel)
**رنگ Layout:** بنفش 🟣

#### فایل‌های جدید:
- `src/SMS.Application/DTOs/PrincipalDtos.cs`
- `src/SMS.Application/Services/IPrincipalPortalService.cs`
- `src/SMS.Infrastructure/Services/PrincipalPortalService.cs`
- `src/SMS.Web/Controllers/PrincipalController.cs`
- `src/SMS.Web/Views/Shared/_PrincipalLayout.cshtml`
- `src/SMS.Web/Views/Principal/`:
  - `_ViewStart.cshtml`, `Dashboard.cshtml` (با ۲ نمودار Chart.js),
    `Classrooms.cshtml`, `SelectSchool.cshtml`, `NoAccess.cshtml`

#### ویژگی‌ها:
- **پشتیبانی از چند مدرسه:** اگر کاربر در چند مدرسه مدیر/معاون باشد، اول انتخاب می‌کند
- **داشبورد گرافیکی** با:
  - ۸ کارت آماری (دانش‌آموز، کلاس، کارمند، حضور امروز، درآمد، پرداخت، معوقات، انضباطی)
  - نمودار میله‌ای روند حضور ۷ روز
  - نمودار دایره‌ای توزیع دانش‌آموزان بر اساس پایه
  - لیست کلاس‌های مدرسه
  - فعالیت‌های اخیر (ثبت‌نام، پرداخت، انضباطی)
- **SuperAdmin** هم می‌تواند این پنل را برای هر مدرسه ببیند

#### امنیت:
- `CanAccessSchoolAsync` چک می‌کند کاربر در آن مدرسه `Principal/VicePrincipal/Admin` است
- همه Query ها فیلتر `SchoolId` دارند

---

### ✅ فاز ۱.۳ — جداسازی پنل دانش‌آموز و ولی
**رنگ Layout:** نارنجی 🟠 (ولی)، آبی 🔵 (دانش‌آموز در `_PortalLayout`)

#### فایل‌های جدید:
- `src/SMS.Web/Controllers/ParentController.cs`
- `src/SMS.Web/Views/Shared/_ParentLayout.cshtml`
- `src/SMS.Web/Views/Parent/_ViewStart.cshtml`, `Dashboard.cshtml`

#### ویژگی‌ها:
- داشبورد چند فرزندی برای ولی: لیست همه فرزندان با کارت خلاصه (معدل، غیبت، بدهی)
- ولی روی کارت هر فرزند کلیک می‌کند → به `MyPortal/Dashboard` می‌رود
- `MyPortal` همان شکل قبلی برای دانش‌آموز باقی ماند (DRY)
- در لاگین، ولی به `/Parent` و دانش‌آموز به `/MyPortal` می‌رود

---

## 📊 فاز ۲: گزارش‌ها و نمودارها

### ✅ سرویس `AnalyticsService` — ۴ گزارش کلان

#### فایل‌های جدید:
- `src/SMS.Application/DTOs/AnalyticsDtos.cs` — ۲۰+ DTO
- `src/SMS.Application/Services/IAnalyticsService.cs`
- `src/SMS.Infrastructure/Services/AnalyticsService.cs`
- `src/SMS.Web/Controllers/AnalyticsController.cs`
- `src/SMS.Web/Views/Analytics/`:
  - `Index.cshtml` (صفحه راهنما)
  - `Attendance.cshtml` — نمودار خطی روزانه + جدول کلاس‌ها + Top غایبان
  - `Academic.cshtml` — نمودار میله‌ای دروس + دونات توزیع نمرات + Top/Weak دانش‌آموزان
  - `Financial.cshtml` — نمودار درآمد ماهانه + pie فی‌تایپ + دونات روش پرداخت + بدهکاران
  - `Discipline.cshtml` — نمودار خطی ماهانه + جدول‌های تشویق/تنبیه

#### ویژگی‌ها:
- استفاده از **Chart.js 4.4** (از CDN)
- همه گزارش‌ها فیلتر تاریخ شمسی، مدرسه، ترم/سال تحصیلی دارند
- آماده برای Export (در فاز بعدی)

---

## 🔧 فاز ۳: امکانات تکمیلی

### ✅ ۳.۱ — سیستم تکالیف (Homework)

#### فایل‌های جدید:
- `src/SMS.Domain/Entities/Homework.cs` — `Homework` + `HomeworkSubmission`
- `src/SMS.Application/DTOs/HomeworkDtos.cs`
- `src/SMS.Application/Services/IHomeworkService.cs` (در همان فایل با Message)
- `src/SMS.Infrastructure/Services/HomeworkService.cs`
- `src/SMS.Web/Controllers/HomeworksController.cs`
- `src/SMS.Web/Views/Homeworks/`:
  - `TeacherList.cshtml`, `Create.cshtml`, `Submissions.cshtml`
  - `StudentList.cshtml`, `Submit.cshtml`

#### ویژگی‌ها:
- **معلم:** ایجاد تکلیف، مشاهده تحویل‌ها، نمره‌گذاری با بازخورد
- **دانش‌آموز:** مشاهده لیست، تحویل (متن + فایل)، مشاهده نمره
- تشخیص خودکار "دیر" بودن تحویل

---

### ✅ ۳.۲ — برنامه هفتگی (Timetable)

#### فایل‌های جدید:
- `src/SMS.Application/DTOs/ScheduleDtos.cs`
- `src/SMS.Application/Services/IScheduleService.cs`
- `src/SMS.Infrastructure/Services/ScheduleService.cs`
- `src/SMS.Web/Controllers/SchedulesController.cs`
- `src/SMS.Web/Views/Schedules/`:
  - `Periods.cshtml` (مدیریت زنگ‌های مدرسه)
  - `Classroom.cshtml` (جدول هفته‌ای ۷×N برای کلاس)

#### ویژگی‌ها:
- **زنگ‌ها (Periods):** هر مدرسه زنگ‌های خودش رو تعریف می‌کنه (زنگ اول 8:00-8:45)
- **برنامه کلاس:** جدول ۷ روز در N زنگ، تخصیص درس+معلم به هر سلول
- **روز هفته به سبک ایرانی:** شنبه=0, ..., جمعه=6

---

### ✅ ۳.۳ — پیام‌رسان داخلی (Messaging)

#### فایل‌های جدید:
- `src/SMS.Domain/Entities/Message.cs`
- `src/SMS.Application/DTOs/MessageDtos.cs`
- `src/SMS.Infrastructure/Services/MessageService.cs`
- `src/SMS.Web/Controllers/MessagesController.cs`
- `src/SMS.Web/Views/Messages/`:
  - `Index.cshtml` (Inbox), `Sent.cshtml`, `View.cshtml`, `Compose.cshtml`

#### ویژگی‌ها:
- پیام بین همه کاربران سیستم (معلم↔ولی، مدیر↔معلم، ...)
- **پاسخ (Reply)** + **دسته‌بندی** (Notice/Question/Complaint/Request)
- علامت‌گذاری خوانده شده خودکار هنگام باز کردن
- حذف به‌صورت Soft (هر طرف جدا)

---

## ✏️ فایل‌های ویرایش شده

| فایل | تغییر |
|------|--------|
| `src/SMS.Infrastructure/Persistence/SmsDbContext.cs` | افزودن ۵ DbSet جدید + HasKey + Unique Indexes |
| `src/SMS.Infrastructure/DependencyInjection.cs` | ثبت ۶ سرویس جدید |
| `src/SMS.Web/Controllers/AccountController.cs` | ریدایرکت خودکار بعد از لاگین بر اساس نقش |
| `src/SMS.Web/Views/Shared/_Layout.cshtml` | منوی Analytics، Messages، Schedules اضافه شد |
| `src/SMS.Web/Views/Shared/_TeacherLayout.cshtml` | منوی Messages و Homeworks اضافه شد |
| `src/SMS.Web/Views/Shared/_PrincipalLayout.cshtml` | منوی Messages و Analytics اضافه شد |
| `src/SMS.Web/Views/Shared/_ParentLayout.cshtml` | منوی Messages اضافه شد |
| `src/SMS.Web/Views/Shared/_PortalLayout.cshtml` | منوی Homeworks و Messages اضافه شد |

---

## 🗄️ Migration دیتابیس

⚠️ **مهم:** ۵ جدول جدید اضافه شدند. در پروژه خودت اجرا کن:

```bash
cd src/SMS.Infrastructure
dotnet ef migrations add UpgradePhase1_2_3_AllFeatures --startup-project ../SMS.Web
dotnet ef database update --startup-project ../SMS.Web
```

### جداول جدید:
1. `SchoolPeriods` (زنگ‌های مدرسه)
2. `ClassSchedules` (برنامه هفتگی)
3. `Homeworks` (تکالیف)
4. `HomeworkSubmissions` (تحویل تکالیف)
5. `Messages` (پیام‌های داخلی)

اگر می‌خوای SQL دستی بزنی:

```sql
CREATE TABLE SchoolPeriods (
    PeriodId    INT IDENTITY(1,1) PRIMARY KEY,
    SchoolId    INT NOT NULL FOREIGN KEY REFERENCES Schools(SchoolId),
    PeriodNo    TINYINT NOT NULL,
    Name        NVARCHAR(50) NOT NULL,
    StartTime   TIME NOT NULL,
    EndTime     TIME NOT NULL,
    IsBreak     BIT NOT NULL DEFAULT 0,
    IsActive    BIT NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX UQ_SchoolPeriods ON SchoolPeriods(SchoolId, PeriodNo);

CREATE TABLE ClassSchedules (
    ScheduleId      INT IDENTITY(1,1) PRIMARY KEY,
    ClassroomId     INT NOT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId),
    ClassSubjectId  INT NOT NULL FOREIGN KEY REFERENCES ClassSubjectTeachers(ClassSubjectId),
    PeriodId        INT NOT NULL FOREIGN KEY REFERENCES SchoolPeriods(PeriodId),
    DayOfWeek       TINYINT NOT NULL,
    RoomNumber      NVARCHAR(30) NULL,
    StartDate       DATETIME2 NULL,
    EndDate         DATETIME2 NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX UQ_ClassSchedule ON ClassSchedules(ClassroomId, DayOfWeek, PeriodId);

CREATE TABLE Homeworks (
    HomeworkId          BIGINT IDENTITY(1,1) PRIMARY KEY,
    ClassSubjectId      INT NOT NULL FOREIGN KEY REFERENCES ClassSubjectTeachers(ClassSubjectId),
    CreatedByStaffId    INT NOT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    Title               NVARCHAR(300) NOT NULL,
    Description         NVARCHAR(4000) NULL,
    AssignedDate        DATETIME2 NOT NULL,
    DueDate             DATETIME2 NOT NULL,
    MaxScore            DECIMAL(5,2) NULL,
    AttachmentPath      NVARCHAR(500) NULL,
    AllowFileSubmission BIT NOT NULL DEFAULT 1,
    IsActive            BIT NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE HomeworkSubmissions (
    SubmissionId        BIGINT IDENTITY(1,1) PRIMARY KEY,
    HomeworkId          BIGINT NOT NULL FOREIGN KEY REFERENCES Homeworks(HomeworkId),
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    SubmittedAt         DATETIME2 NOT NULL,
    IsLate              BIT NOT NULL DEFAULT 0,
    TextAnswer          NVARCHAR(4000) NULL,
    AttachmentPath      NVARCHAR(500) NULL,
    Score               DECIMAL(5,2) NULL,
    TeacherFeedback     NVARCHAR(1000) NULL,
    GradedByStaffId     INT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    GradedAt            DATETIME2 NULL
);
CREATE UNIQUE INDEX UQ_HomeworkSubmission ON HomeworkSubmissions(HomeworkId, StudentId);

CREATE TABLE Messages (
    MessageId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    FromUserId          INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    ToUserId            INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Subject             NVARCHAR(200) NULL,
    Body                NVARCHAR(MAX) NOT NULL,
    SentAt              DATETIME2 NOT NULL,
    ReadAt              DATETIME2 NULL,
    IsDeletedBySender   BIT NOT NULL DEFAULT 0,
    IsDeletedByReceiver BIT NOT NULL DEFAULT 0,
    ReplyToMessageId    BIGINT NULL,
    Category            NVARCHAR(50) NULL
);
CREATE INDEX IX_Messages_To_Read ON Messages(ToUserId, ReadAt);
```

---

## ✅ چک‌لیست تست بعد از Migration

### کلی
- [ ] پروژه با موفقیت Build میشه
- [ ] `dotnet ef migrations add` خطا نمی‌ده
- [ ] `dotnet ef database update` موفقیت‌آمیز

### تست هر نقش
- [ ] **SuperAdmin / SchoolAdmin** لاگین → داشبورد عمومی → منوی Analytics و Schedules قابل دسترس
- [ ] **Principal** لاگین → اگر یک مدرسه: داشبورد مدیر / اگر چند: SelectSchool
- [ ] **Teacher** لاگین → داشبورد سبز معلم → کلاس‌ها/نمره/حضور
- [ ] **Parent** لاگین → داشبورد نارنجی → لیست فرزندان → کلیک: MyPortal فرزند
- [ ] **Student** لاگین → MyPortal با منوی تکالیف و پیام‌ها

### تست عملیاتی
- [ ] ایجاد زنگ‌های مدرسه از `Schedules/Periods?schoolId=X`
- [ ] تخصیص برنامه به کلاس از `Schedules/Classroom?classroomId=Y`
- [ ] معلم تکلیف بسازه، دانش‌آموز تحویل بده، معلم نمره بده
- [ ] پیام بین دو کاربر مختلف
- [ ] گزارش‌گیری از `Analytics/Attendance` و `Analytics/Academic`

### تست امنیت (مهم!)
- [ ] معلم URL کلاس کس دیگه رو بزنه → 403
- [ ] مدیر مدرسه URL مدرسه دیگه رو بزنه → 403
- [ ] دانش‌آموز تحویل تکلیف دیگری رو نتونه ببینه

---

## 🎯 فازهای آینده (پیشنهادی)

### فاز ۴: کیفیت و امنیت
- [ ] Unit Tests + Integration Tests (با xUnit + Moq)
- [ ] Rate Limiting روی APIs
- [ ] فعال‌سازی CSRF برای همه فرم‌ها (اکثرشون دارن)
- [ ] Serilog روی همه عملیات حساس
- [ ] API Documentation با Swagger Tags

### فاز ۵: امکانات اضافی
- [ ] **آپلود واقعی فایل** (تکالیف، عکس پروفایل، فایل ضمیمه پیام)
- [ ] **Export PDF** کارنامه با فونت فارسی (QuestPDF آماده‌ست)
- [ ] **Notification Bell** بالای صفحه با تعداد unread
- [ ] **WebSocket / SignalR** برای پیام لحظه‌ای
- [ ] **کلاس آنلاین** (لینک جلسه + ضبط)
- [ ] **کتابخانه** (امانت/بازگشت کتاب)

---

## 📁 لینک سریع به فایل‌های اصلی

### سرویس‌ها (هر نقش)
- `src/SMS.Infrastructure/Services/TeacherPortalService.cs`
- `src/SMS.Infrastructure/Services/PrincipalPortalService.cs`
- `src/SMS.Infrastructure/Services/AnalyticsService.cs`
- `src/SMS.Infrastructure/Services/HomeworkService.cs`
- `src/SMS.Infrastructure/Services/MessageService.cs`
- `src/SMS.Infrastructure/Services/ScheduleService.cs`

### کنترلرها
- `src/SMS.Web/Controllers/TeacherController.cs`
- `src/SMS.Web/Controllers/PrincipalController.cs`
- `src/SMS.Web/Controllers/ParentController.cs`
- `src/SMS.Web/Controllers/AnalyticsController.cs`
- `src/SMS.Web/Controllers/HomeworksController.cs`
- `src/SMS.Web/Controllers/MessagesController.cs`
- `src/SMS.Web/Controllers/SchedulesController.cs`

### Layouts
- `src/SMS.Web/Views/Shared/_Layout.cshtml` (ادمین — موجود)
- `src/SMS.Web/Views/Shared/_TeacherLayout.cshtml` (جدید — سبز)
- `src/SMS.Web/Views/Shared/_PrincipalLayout.cshtml` (جدید — بنفش)
- `src/SMS.Web/Views/Shared/_ParentLayout.cshtml` (جدید — نارنجی)
- `src/SMS.Web/Views/Shared/_PortalLayout.cshtml` (دانش‌آموز — موجود)

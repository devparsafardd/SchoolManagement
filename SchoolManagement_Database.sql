/*
============================================================================
 سیستم جامع مدیریت مدارس - School Management System (SMS)
 Database: SQL Server 2019+
 طراحی: ساده، قدرتمند، انعطاف‌پذیر و بهینه برای حجم بالا
============================================================================
*/

-- CREATE DATABASE SchoolManagement;
-- GO
-- USE SchoolManagement;
-- GO

/* ============================================================================
   بخش ۱: جداول پایه و Lookup
   نکته: به جای ENUM از جداول Lookup استفاده می‌کنیم تا انعطاف داشته باشیم
============================================================================ */

-- استان‌ها و شهرها (برای مدارس و آدرس‌ها)
CREATE TABLE Provinces (
    ProvinceId      INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Cities (
    CityId          INT IDENTITY(1,1) PRIMARY KEY,
    ProvinceId      INT NOT NULL FOREIGN KEY REFERENCES Provinces(ProvinceId),
    Name            NVARCHAR(100) NOT NULL
);
CREATE INDEX IX_Cities_Province ON Cities(ProvinceId);

-- مقاطع تحصیلی: ابتدایی، متوسطه اول، متوسطه دوم، فنی و حرفه‌ای، کاردانش
CREATE TABLE EducationLevels (
    EducationLevelId    INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(100) NOT NULL,    -- ابتدایی / متوسطه اول / متوسطه دوم
    Code                NVARCHAR(20)  NOT NULL UNIQUE, -- ELEM / MID / HIGH / TECH
    IsDescriptive       BIT NOT NULL DEFAULT 0,    -- ارزشیابی توصیفی؟
    MinGrade            TINYINT NULL,              -- مثلاً ۱
    MaxGrade            TINYINT NULL               -- مثلاً ۶
);

-- پایه‌های تحصیلی: اول دبستان، دوم دبستان، ...، دوازدهم
CREATE TABLE Grades (
    GradeId             INT IDENTITY(1,1) PRIMARY KEY,
    EducationLevelId    INT NOT NULL FOREIGN KEY REFERENCES EducationLevels(EducationLevelId),
    Name                NVARCHAR(50) NOT NULL,     -- مثلا "پنجم ابتدایی"
    OrderNo             TINYINT NOT NULL           -- ۱ تا ۱۲
);

-- رشته‌های تحصیلی (برای متوسطه دوم)
CREATE TABLE Majors (
    MajorId             INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(150) NOT NULL,    -- ریاضی / تجربی / انسانی / کامپیوتر / حسابداری
    Code                NVARCHAR(30)  NULL,
    EducationLevelId    INT NULL FOREIGN KEY REFERENCES EducationLevels(EducationLevelId),
    IsActive            BIT NOT NULL DEFAULT 1
);

/* ============================================================================
   بخش ۲: مدارس و سال تحصیلی (Multi-Tenant)
============================================================================ */

CREATE TABLE Schools (
    SchoolId            INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(200) NOT NULL,
    Code                NVARCHAR(50)  NOT NULL UNIQUE,  -- کد مدرسه (وزارت آموزش)
    CityId              INT NOT NULL FOREIGN KEY REFERENCES Cities(CityId),
    Address             NVARCHAR(500) NULL,
    Phone               NVARCHAR(30)  NULL,
    Gender              CHAR(1) NOT NULL CHECK (Gender IN ('M','F','B')), -- پسرانه/دخترانه/مختلط
    SchoolType          NVARCHAR(50)  NULL,  -- دولتی / غیرانتفاعی / نمونه / تیزهوشان
    EducationLevelId    INT NOT NULL FOREIGN KEY REFERENCES EducationLevels(EducationLevelId),
    PrincipalUserId     INT NULL,            -- بعداً FK میشه به Users
    IsActive            BIT NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Schools_City ON Schools(CityId);

-- سال تحصیلی: 1404-1405
CREATE TABLE AcademicYears (
    AcademicYearId      INT IDENTITY(1,1) PRIMARY KEY,
    Title               NVARCHAR(30) NOT NULL UNIQUE,   -- "1404-1405"
    StartDate           DATE NOT NULL,
    EndDate             DATE NOT NULL,
    IsActive            BIT  NOT NULL DEFAULT 0         -- سال جاری
);

-- ترم/نیم‌سال: نوبت اول، نوبت دوم، تابستان (یا پودمان ۱..۸)
CREATE TABLE Terms (
    TermId              INT IDENTITY(1,1) PRIMARY KEY,
    AcademicYearId      INT NOT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    Name                NVARCHAR(50) NOT NULL,   -- "نوبت اول"
    OrderNo             TINYINT NOT NULL,
    StartDate           DATE NOT NULL,
    EndDate             DATE NOT NULL
);
CREATE INDEX IX_Terms_Year ON Terms(AcademicYearId);

/* ============================================================================
   بخش ۳: کاربران، نقش‌ها و دسترسی‌ها (هسته احراز هویت)
   نکته کلیدی: یک شخص = یک رکورد در Persons
   این شخص می‌تواند چند نقش داشته باشد (معلم + معاون) در چند مدرسه
============================================================================ */

-- جدول مرجع همه افراد سیستم (دانش‌آموز، معلم، کارمند، اولیا)
CREATE TABLE Persons (
    PersonId            INT IDENTITY(1,1) PRIMARY KEY,
    NationalCode        NVARCHAR(10) NULL UNIQUE,   -- کد ملی (یکتا)
    FirstName           NVARCHAR(100) NOT NULL,
    LastName            NVARCHAR(100) NOT NULL,
    FatherName          NVARCHAR(100) NULL,
    Gender              CHAR(1) NOT NULL CHECK (Gender IN ('M','F')),
    BirthDate           DATE NULL,
    BirthPlace          NVARCHAR(100) NULL,
    Mobile              NVARCHAR(15) NULL,
    Email               NVARCHAR(150) NULL,
    Address             NVARCHAR(500) NULL,
    PhotoPath           NVARCHAR(500) NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ModifiedAt          DATETIME2 NULL
);
CREATE INDEX IX_Persons_Name ON Persons(LastName, FirstName);
CREATE INDEX IX_Persons_Mobile ON Persons(Mobile) WHERE Mobile IS NOT NULL;

-- اطلاعات لاگین (هر شخصی که نیاز به ورود به سیستم داره)
CREATE TABLE Users (
    UserId              INT IDENTITY(1,1) PRIMARY KEY,
    PersonId            INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Persons(PersonId),
    Username            NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash        NVARCHAR(255) NOT NULL,
    PasswordSalt        NVARCHAR(100) NULL,
    IsLocked            BIT NOT NULL DEFAULT 0,
    LastLoginAt         DATETIME2 NULL,
    FailedLoginCount    SMALLINT NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- نقش‌ها: SuperAdmin, SchoolAdmin, Principal, ViceP, Teacher, Student, Parent, Accountant
CREATE TABLE Roles (
    RoleId              INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(50) NOT NULL UNIQUE,    -- مثلا "Teacher"
    DisplayName         NVARCHAR(100) NOT NULL,          -- "معلم"
    Description         NVARCHAR(300) NULL
);

-- یک کاربر می‌تواند چند نقش در چند مدرسه داشته باشد ✅ (نکته مهم شما)
-- مثلا: علی هم معاون مدرسه الف هست هم معلم مدرسه الف و ب
CREATE TABLE UserRoles (
    UserRoleId          BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId              INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    RoleId              INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
    SchoolId            INT NULL FOREIGN KEY REFERENCES Schools(SchoolId), -- NULL = نقش سراسری
    AcademicYearId      INT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    StartDate           DATE NOT NULL DEFAULT CAST(SYSDATETIME() AS DATE),
    EndDate             DATE NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_UserRoles UNIQUE(UserId, RoleId, SchoolId, AcademicYearId)
);
CREATE INDEX IX_UserRoles_School ON UserRoles(SchoolId, RoleId) WHERE IsActive = 1;

-- مجوزها (Permission) - برای کنترل دسترسی ریزدانه
CREATE TABLE Permissions (
    PermissionId        INT IDENTITY(1,1) PRIMARY KEY,
    Code                NVARCHAR(100) NOT NULL UNIQUE,  -- "Student.Create"
    DisplayName         NVARCHAR(150) NOT NULL,
    Category            NVARCHAR(50) NULL
);

CREATE TABLE RolePermissions (
    RoleId              INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
    PermissionId        INT NOT NULL FOREIGN KEY REFERENCES Permissions(PermissionId),
    PRIMARY KEY (RoleId, PermissionId)
);

/* ============================================================================
   بخش ۴: کارکنان (Staff) - معلم، معاون، مدیر، خدمات
   به جای جدول‌های جدا برای Teacher/VicePrincipal/...، یک جدول Staff داریم
   چون یک نفر می‌تواند هم معلم باشد هم معاون ✅
============================================================================ */

CREATE TABLE Staff (
    StaffId             INT IDENTITY(1,1) PRIMARY KEY,
    PersonId            INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Persons(PersonId),
    PersonnelCode       NVARCHAR(30) NULL UNIQUE,    -- شماره پرسنلی
    EmploymentType      NVARCHAR(50) NULL,           -- رسمی / پیمانی / حق‌التدریس / قراردادی
    Degree              NVARCHAR(100) NULL,          -- لیسانس/فوق‌لیسانس/دکتری
    FieldOfStudy        NVARCHAR(200) NULL,          -- رشته تحصیلی
    HireDate            DATE NULL,
    IBAN                NVARCHAR(26) NULL,
    IsActive            BIT NOT NULL DEFAULT 1
);

-- ارتباط Staff با مدرسه (یک معلم در چند مدرسه می‌تواند کار کند) ✅
CREATE TABLE StaffSchoolAssignments (
    AssignmentId        INT IDENTITY(1,1) PRIMARY KEY,
    StaffId             INT NOT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    SchoolId            INT NOT NULL FOREIGN KEY REFERENCES Schools(SchoolId),
    AcademicYearId      INT NOT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    Position            NVARCHAR(50) NOT NULL,  -- Teacher/VicePrincipal/Principal/Counselor/Admin
    WeeklyHours         DECIMAL(5,2) NULL,
    StartDate           DATE NOT NULL,
    EndDate             DATE NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_StaffSchool UNIQUE(StaffId, SchoolId, AcademicYearId, Position)
);
CREATE INDEX IX_StaffAssign_School ON StaffSchoolAssignments(SchoolId, AcademicYearId);
CREATE INDEX IX_StaffAssign_Staff ON StaffSchoolAssignments(StaffId, AcademicYearId);

/* ============================================================================
   بخش ۵: دانش‌آموزان و اولیا
============================================================================ */

CREATE TABLE Students (
    StudentId           INT IDENTITY(1,1) PRIMARY KEY,
    PersonId            INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Persons(PersonId),
    StudentCode         NVARCHAR(30) NOT NULL UNIQUE,  -- شماره دانش‌آموزی
    EnrollmentDate      DATE NOT NULL DEFAULT CAST(SYSDATETIME() AS DATE),
    BloodType           NVARCHAR(5) NULL,
    SpecialNeeds        NVARCHAR(500) NULL,           -- بیماری/شرایط خاص
    IsActive            BIT NOT NULL DEFAULT 1
);

-- اولیا (پدر، مادر، سرپرست)
CREATE TABLE Guardians (
    GuardianId          INT IDENTITY(1,1) PRIMARY KEY,
    PersonId            INT NOT NULL FOREIGN KEY REFERENCES Persons(PersonId),
    Occupation          NVARCHAR(150) NULL,
    WorkplacePhone      NVARCHAR(30) NULL,
    EducationLevel      NVARCHAR(100) NULL
);

CREATE TABLE StudentGuardians (
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    GuardianId          INT NOT NULL FOREIGN KEY REFERENCES Guardians(GuardianId),
    Relationship        NVARCHAR(30) NOT NULL,  -- پدر / مادر / عمو / ...
    IsPrimary           BIT NOT NULL DEFAULT 0, -- سرپرست اصلی
    HasCustody          BIT NOT NULL DEFAULT 1, -- حق حضانت
    CanPickup           BIT NOT NULL DEFAULT 1,
    PRIMARY KEY (StudentId, GuardianId)
);

/* ============================================================================
   بخش ۶: کلاس‌ها (Class) و ثبت‌نام دانش‌آموز در کلاس
   مفهوم Class: یک گروه از دانش‌آموزان در یک پایه/رشته در یک سال تحصیلی
   مثال: "ششم الف" مدرسه شهید بهشتی سال 1404
============================================================================ */

CREATE TABLE Classrooms (
    ClassroomId         INT IDENTITY(1,1) PRIMARY KEY,
    SchoolId            INT NOT NULL FOREIGN KEY REFERENCES Schools(SchoolId),
    AcademicYearId      INT NOT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    GradeId             INT NOT NULL FOREIGN KEY REFERENCES Grades(GradeId),
    MajorId             INT NULL FOREIGN KEY REFERENCES Majors(MajorId), -- برای متوسطه دوم
    Name                NVARCHAR(50) NOT NULL,        -- "ششم الف" یا "201"
    Capacity            SMALLINT NULL,
    HeadTeacherStaffId  INT NULL FOREIGN KEY REFERENCES Staff(StaffId), -- معلم/سرپرست کلاس
    RoomNumber          NVARCHAR(20) NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Classroom UNIQUE(SchoolId, AcademicYearId, GradeId, Name)
);
CREATE INDEX IX_Classroom_School_Year ON Classrooms(SchoolId, AcademicYearId);

-- ثبت‌نام دانش‌آموز در کلاس (Enrollment)
-- یک دانش‌آموز در یک سال = در یک کلاس اصلی
CREATE TABLE StudentEnrollments (
    EnrollmentId        BIGINT IDENTITY(1,1) PRIMARY KEY,
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    ClassroomId         INT NOT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId),
    AcademicYearId      INT NOT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    EnrollmentDate      DATE NOT NULL,
    Status              NVARCHAR(30) NOT NULL DEFAULT N'فعال', -- فعال/انتقالی/انصراف/مردود/قبول
    LeaveDate           DATE NULL,
    LeaveReason         NVARCHAR(300) NULL,
    CONSTRAINT UQ_StudentYear UNIQUE(StudentId, AcademicYearId)
);
CREATE INDEX IX_Enroll_Class ON StudentEnrollments(ClassroomId);
CREATE INDEX IX_Enroll_Student ON StudentEnrollments(StudentId);

/* ============================================================================
   بخش ۷: دروس و تخصیص معلم به کلاس/درس
============================================================================ */

-- دروس کلی (کاتالوگ): ریاضی، علوم، فارسی، ...
CREATE TABLE Subjects (
    SubjectId           INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(150) NOT NULL,
    Code                NVARCHAR(30)  NULL UNIQUE,
    Description         NVARCHAR(500) NULL,
    IsActive            BIT NOT NULL DEFAULT 1
);

-- ارائه درس در یک پایه خاص (با تعداد واحد و ضریب)
-- مثلا: ریاضی پایه ششم ابتدایی 4 ساعت در هفته
CREATE TABLE GradeSubjects (
    GradeSubjectId      INT IDENTITY(1,1) PRIMARY KEY,
    GradeId             INT NOT NULL FOREIGN KEY REFERENCES Grades(GradeId),
    SubjectId           INT NOT NULL FOREIGN KEY REFERENCES Subjects(SubjectId),
    MajorId             INT NULL FOREIGN KEY REFERENCES Majors(MajorId),
    Credits             DECIMAL(4,2) NULL,    -- تعداد واحد
    Coefficient         DECIMAL(4,2) NOT NULL DEFAULT 1, -- ضریب (برای معدل وزنی)
    WeeklyHours         DECIMAL(4,2) NULL,
    IsDescriptive       BIT NOT NULL DEFAULT 0,    -- این درس توصیفی است یا نمره‌ای؟
    MaxScore            DECIMAL(5,2) NOT NULL DEFAULT 20,
    PassingScore        DECIMAL(5,2) NOT NULL DEFAULT 10,
    CONSTRAINT UQ_GradeSubject UNIQUE(GradeId, SubjectId, MajorId)
);

-- تخصیص معلم به یک درس در یک کلاس
-- یک کلاس می‌تواند چند درس داشته باشد، هر درس یک معلم
CREATE TABLE ClassSubjectTeachers (
    ClassSubjectId      INT IDENTITY(1,1) PRIMARY KEY,
    ClassroomId         INT NOT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId),
    GradeSubjectId      INT NOT NULL FOREIGN KEY REFERENCES GradeSubjects(GradeSubjectId),
    StaffId             INT NOT NULL FOREIGN KEY REFERENCES Staff(StaffId), -- معلم
    StartDate           DATE NULL,
    EndDate             DATE NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_ClassSubject UNIQUE(ClassroomId, GradeSubjectId)
);
CREATE INDEX IX_CST_Class ON ClassSubjectTeachers(ClassroomId);
CREATE INDEX IX_CST_Staff ON ClassSubjectTeachers(StaffId);

/* ============================================================================
   بخش ۸: برنامه هفتگی (Timetable)
============================================================================ */

CREATE TABLE TimeSlots (
    TimeSlotId          INT IDENTITY(1,1) PRIMARY KEY,
    SchoolId            INT NOT NULL FOREIGN KEY REFERENCES Schools(SchoolId),
    Name                NVARCHAR(50) NOT NULL,  -- "زنگ اول"
    StartTime           TIME NOT NULL,
    EndTime             TIME NOT NULL,
    OrderNo             TINYINT NOT NULL
);

CREATE TABLE WeeklySchedules (
    ScheduleId          BIGINT IDENTITY(1,1) PRIMARY KEY,
    ClassSubjectId      INT NOT NULL FOREIGN KEY REFERENCES ClassSubjectTeachers(ClassSubjectId),
    DayOfWeek           TINYINT NOT NULL CHECK (DayOfWeek BETWEEN 0 AND 6), -- 0=شنبه
    TimeSlotId          INT NOT NULL FOREIGN KEY REFERENCES TimeSlots(TimeSlotId),
    RoomNumber          NVARCHAR(20) NULL,
    CONSTRAINT UQ_Schedule UNIQUE(ClassSubjectId, DayOfWeek, TimeSlotId)
);

/* ============================================================================
   بخش ۹: حضور و غیاب
   نکته بهینه‌سازی: برای حجم بالا، فقط رخدادهای غیرعادی (غیبت/تاخیر) ثبت میشه
   اگر چیزی ثبت نشده باشه = حاضر بوده
============================================================================ */

CREATE TABLE AttendanceStatuses (
    StatusId            TINYINT PRIMARY KEY,
    Name                NVARCHAR(30) NOT NULL UNIQUE,  -- حاضر / غایب موجه / غایب غیرموجه / تاخیر / مرخصی / فرار
    Code                NVARCHAR(20) NOT NULL UNIQUE,
    IsAbsent            BIT NOT NULL DEFAULT 0,
    IsTardy             BIT NOT NULL DEFAULT 0
);

-- جدول اصلی حضور و غیاب (Partition-Ready برای حجم بالا)
CREATE TABLE Attendances (
    AttendanceId        BIGINT IDENTITY(1,1) NOT NULL,
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    ClassroomId         INT NOT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId),
    ClassSubjectId      INT NULL FOREIGN KEY REFERENCES ClassSubjectTeachers(ClassSubjectId), -- اگر تخصصی درس باشد
    AttendanceDate      DATE NOT NULL,
    TimeSlotId          INT NULL FOREIGN KEY REFERENCES TimeSlots(TimeSlotId),
    StatusId            TINYINT NOT NULL FOREIGN KEY REFERENCES AttendanceStatuses(StatusId),
    TardyMinutes        SMALLINT NULL,           -- دقایق تاخیر
    Description         NVARCHAR(300) NULL,
    RecordedByStaffId   INT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Attendance PRIMARY KEY CLUSTERED (AttendanceDate, AttendanceId)
    -- کلید کلاستر روی تاریخ + ID برای Partition-بندی آینده روی تاریخ
);
CREATE INDEX IX_Attend_Student_Date ON Attendances(StudentId, AttendanceDate) INCLUDE (StatusId);
CREATE INDEX IX_Attend_Class_Date ON Attendances(ClassroomId, AttendanceDate);

-- جدول داده‌های اولیه برای AttendanceStatuses:
-- 1=حاضر، 2=غایب غیرموجه، 3=غایب موجه، 4=تاخیر، 5=مرخصی، 6=فرار از مدرسه

/* ============================================================================
   بخش ۱۰: تشویق و تنبیه (انضباطی)
============================================================================ */

CREATE TABLE DisciplinaryTypes (
    TypeId              INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(100) NOT NULL UNIQUE,    -- "تشویق کلاسی" / "اخطار" / "احضار اولیا"
    Category            CHAR(1) NOT NULL CHECK (Category IN ('R','P')), -- Reward / Punishment
    Severity            TINYINT NULL,    -- شدت 1 تا 5
    DefaultScoreImpact  DECIMAL(4,2) NULL -- تاثیر در نمره انضباط (+/-)
);

CREATE TABLE DisciplinaryRecords (
    RecordId            BIGINT IDENTITY(1,1) PRIMARY KEY,
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    ClassroomId         INT NOT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId),
    AcademicYearId      INT NOT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    TypeId              INT NOT NULL FOREIGN KEY REFERENCES DisciplinaryTypes(TypeId),
    RecordDate          DATE NOT NULL,
    Description         NVARCHAR(1000) NOT NULL,
    ActionTaken         NVARCHAR(500) NULL,         -- اقدام انجام‌شده
    ScoreImpact         DECIMAL(4,2) NULL,          -- اثر روی نمره انضباط
    IsParentNotified    BIT NOT NULL DEFAULT 0,
    NotifiedAt          DATETIME2 NULL,
    RecordedByStaffId   INT NOT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Disc_Student ON DisciplinaryRecords(StudentId, AcademicYearId);

/* ============================================================================
   بخش ۱۱: آزمون‌ها و نمرات (با پشتیبانی از توصیفی و نمره‌ای)
   این بخش قلب سیستم است و انعطاف بالایی نیاز دارد
============================================================================ */

-- انواع آزمون: کلاسی، میان‌ترم، پایان‌ترم، پودمانی، پروژه، پرسش شفاهی، ...
CREATE TABLE ExamTypes (
    ExamTypeId          INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(80) NOT NULL UNIQUE,  -- "آزمون کلاسی" / "نوبت اول" / "پودمان ۱"
    Code                NVARCHAR(30) NULL UNIQUE,
    DefaultWeight       DECIMAL(5,2) NOT NULL DEFAULT 1,   -- وزن پیش‌فرض در محاسبه معدل ترم
    IsFinal             BIT NOT NULL DEFAULT 0,            -- آزمون پایانی؟
    CountsForGPA        BIT NOT NULL DEFAULT 1
);

-- مقیاس‌های ارزشیابی توصیفی (برای ابتدایی)
-- "خیلی خوب / خوب / قابل قبول / نیازمند تلاش بیشتر"
CREATE TABLE GradeScales (
    GradeScaleId        INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(100) NOT NULL,        -- "توصیفی ابتدایی 4 سطحی"
    IsDescriptive       BIT NOT NULL DEFAULT 1,
    IsActive            BIT NOT NULL DEFAULT 1
);

CREATE TABLE GradeScaleItems (
    GradeScaleItemId    INT IDENTITY(1,1) PRIMARY KEY,
    GradeScaleId        INT NOT NULL FOREIGN KEY REFERENCES GradeScales(GradeScaleId),
    Symbol              NVARCHAR(20) NOT NULL,   -- "خ" / "A"
    Label               NVARCHAR(80) NOT NULL,   -- "خیلی خوب"
    NumericEquivalent   DECIMAL(5,2) NULL,       -- معادل عددی برای محاسبات (مثلا 19)
    OrderNo             TINYINT NOT NULL
);
/* نمونه:
   GradeScales: "توصیفی 4 سطحی"
   Items:
     خیلی خوب  -> 19
     خوب       -> 16
     قابل قبول  -> 13
     نیازمند تلاش -> 9
*/

-- تعریف یک آزمون مشخص
-- مثال: "آزمون فصل ۲ ریاضی - ششم الف - مهر ۱۴۰۴"
CREATE TABLE Exams (
    ExamId              BIGINT IDENTITY(1,1) PRIMARY KEY,
    Title               NVARCHAR(200) NOT NULL,
    ClassSubjectId      INT NOT NULL FOREIGN KEY REFERENCES ClassSubjectTeachers(ClassSubjectId),
    ExamTypeId          INT NOT NULL FOREIGN KEY REFERENCES ExamTypes(ExamTypeId),
    TermId              INT NOT NULL FOREIGN KEY REFERENCES Terms(TermId),
    ExamDate            DATE NOT NULL,
    StartTime           TIME NULL,
    DurationMinutes     SMALLINT NULL,
    MaxScore            DECIMAL(5,2) NOT NULL DEFAULT 20,
    Weight              DECIMAL(5,2) NOT NULL DEFAULT 1,   -- وزن این آزمون در نمره ترم
    IsDescriptive       BIT NOT NULL DEFAULT 0,
    GradeScaleId        INT NULL FOREIGN KEY REFERENCES GradeScales(GradeScaleId), -- اگر توصیفی
    Description         NVARCHAR(500) NULL,
    IsFinalized         BIT NOT NULL DEFAULT 0,            -- نهایی شده (دیگه قابل تغییر نیست)
    CreatedByStaffId    INT NOT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Exam_ClassSubject ON Exams(ClassSubjectId);
CREATE INDEX IX_Exam_Term ON Exams(TermId);

-- نمرات (یکپارچه برای توصیفی و عددی) ✅ کلید انعطاف
CREATE TABLE ExamScores (
    ScoreId             BIGINT IDENTITY(1,1) NOT NULL,
    ExamId              BIGINT NOT NULL FOREIGN KEY REFERENCES Exams(ExamId),
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    NumericScore        DECIMAL(5,2) NULL,        -- نمره عددی
    DescriptiveScaleItemId INT NULL FOREIGN KEY REFERENCES GradeScaleItems(GradeScaleItemId), -- نمره توصیفی
    IsAbsent            BIT NOT NULL DEFAULT 0,   -- غایب در جلسه امتحان
    IsExempt            BIT NOT NULL DEFAULT 0,   -- معاف
    Comment             NVARCHAR(500) NULL,
    EnteredByStaffId    INT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    EnteredAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ModifiedAt          DATETIME2 NULL,
    CONSTRAINT PK_ExamScore PRIMARY KEY CLUSTERED (ExamId, StudentId), -- index بهینه
    CONSTRAINT CK_Score_OneType CHECK (
        (NumericScore IS NOT NULL AND DescriptiveScaleItemId IS NULL) OR
        (NumericScore IS NULL AND DescriptiveScaleItemId IS NOT NULL) OR
        (NumericScore IS NULL AND DescriptiveScaleItemId IS NULL AND (IsAbsent=1 OR IsExempt=1))
    )
);
CREATE INDEX IX_Score_Student ON ExamScores(StudentId);

-- خلاصه نمرات ترمی (Aggregate Cache برای performance)
-- پر شدنش با Job یا Trigger، برای جلوگیری از محاسبه دائمی
CREATE TABLE TermSubjectGrades (
    TermSubjectGradeId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    TermId              INT NOT NULL FOREIGN KEY REFERENCES Terms(TermId),
    ClassSubjectId      INT NOT NULL FOREIGN KEY REFERENCES ClassSubjectTeachers(ClassSubjectId),
    FinalNumericScore   DECIMAL(5,2) NULL,
    FinalDescriptiveItemId INT NULL FOREIGN KEY REFERENCES GradeScaleItems(GradeScaleItemId),
    TeacherComment      NVARCHAR(500) NULL,
    IsPassed            BIT NULL,
    CalculatedAt        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_TermGrade UNIQUE(StudentId, TermId, ClassSubjectId)
);

-- معدل ترمی و سالانه دانش‌آموز
CREATE TABLE StudentTermGPA (
    StudentTermGPAId    BIGINT IDENTITY(1,1) PRIMARY KEY,
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    TermId              INT NOT NULL FOREIGN KEY REFERENCES Terms(TermId),
    ClassroomId         INT NOT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId),
    GPA                 DECIMAL(5,3) NULL,    -- معدل وزنی
    DisciplineScore     DECIMAL(5,2) NULL,    -- نمره انضباط
    RankInClass         INT NULL,
    TotalAbsences       SMALLINT NULL,
    TotalTardies        SMALLINT NULL,
    CalculatedAt        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_TermGPA UNIQUE(StudentId, TermId)
);

/* ============================================================================
   بخش ۱۲: مالی (شهریه و پرداخت‌ها) - برای غیرانتفاعی‌ها
============================================================================ */

CREATE TABLE FeeTypes (
    FeeTypeId           INT IDENTITY(1,1) PRIMARY KEY,
    Name                NVARCHAR(100) NOT NULL,   -- شهریه ثابت / سرویس / کتاب / کلاس فوق‌برنامه
    IsRecurring         BIT NOT NULL DEFAULT 0
);

CREATE TABLE StudentInvoices (
    InvoiceId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    StudentId           INT NOT NULL FOREIGN KEY REFERENCES Students(StudentId),
    AcademicYearId      INT NOT NULL FOREIGN KEY REFERENCES AcademicYears(AcademicYearId),
    SchoolId            INT NOT NULL FOREIGN KEY REFERENCES Schools(SchoolId),
    FeeTypeId           INT NOT NULL FOREIGN KEY REFERENCES FeeTypes(FeeTypeId),
    InvoiceNumber       NVARCHAR(30) NOT NULL UNIQUE,
    Amount              DECIMAL(15,0) NOT NULL,
    Discount            DECIMAL(15,0) NOT NULL DEFAULT 0,
    NetAmount           AS (Amount - Discount) PERSISTED,
    DueDate             DATE NULL,
    Status              NVARCHAR(20) NOT NULL DEFAULT N'صادرشده', -- صادرشده / پرداخت‌شده / لغو
    Description         NVARCHAR(500) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Invoice_Student ON StudentInvoices(StudentId, AcademicYearId);

CREATE TABLE Payments (
    PaymentId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId           BIGINT NOT NULL FOREIGN KEY REFERENCES StudentInvoices(InvoiceId),
    PaymentDate         DATE NOT NULL,
    Amount              DECIMAL(15,0) NOT NULL,
    PaymentMethod       NVARCHAR(30) NOT NULL,    -- نقد / کارت / حواله / آنلاین
    ReferenceNumber     NVARCHAR(50) NULL,
    Description         NVARCHAR(300) NULL,
    RecordedByStaffId   INT NULL FOREIGN KEY REFERENCES Staff(StaffId),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

/* ============================================================================
   بخش ۱۳: اطلاعیه‌ها، پیام‌ها و SMS
============================================================================ */

CREATE TABLE Announcements (
    AnnouncementId      INT IDENTITY(1,1) PRIMARY KEY,
    SchoolId            INT NULL FOREIGN KEY REFERENCES Schools(SchoolId), -- NULL = سراسری
    ClassroomId         INT NULL FOREIGN KEY REFERENCES Classrooms(ClassroomId), -- اگر مخصوص یک کلاس
    Title               NVARCHAR(200) NOT NULL,
    Body                NVARCHAR(MAX) NOT NULL,
    TargetAudience      NVARCHAR(30) NOT NULL,    -- All / Students / Parents / Teachers
    PublishDate         DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ExpiryDate          DATETIME2 NULL,
    CreatedByUserId     INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    IsActive            BIT NOT NULL DEFAULT 1
);

CREATE TABLE Messages (
    MessageId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    FromUserId          INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    ToUserId            INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Subject             NVARCHAR(200) NULL,
    Body                NVARCHAR(MAX) NOT NULL,
    SentAt              DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ReadAt              DATETIME2 NULL,
    IsDeletedBySender   BIT NOT NULL DEFAULT 0,
    IsDeletedByReceiver BIT NOT NULL DEFAULT 0
);
CREATE INDEX IX_Msg_To ON Messages(ToUserId, ReadAt);

/* ============================================================================
   بخش ۱۴: لاگ سیستم و Audit
============================================================================ */

CREATE TABLE AuditLogs (
    AuditId             BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId              INT NULL FOREIGN KEY REFERENCES Users(UserId),
    Action              NVARCHAR(50) NOT NULL,    -- Insert / Update / Delete / Login
    EntityName          NVARCHAR(100) NOT NULL,
    EntityId            NVARCHAR(50) NULL,
    OldValues           NVARCHAR(MAX) NULL,       -- JSON
    NewValues           NVARCHAR(MAX) NULL,
    IpAddress           NVARCHAR(45) NULL,
    UserAgent           NVARCHAR(300) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Audit_Date ON AuditLogs(CreatedAt);
CREATE INDEX IX_Audit_User ON AuditLogs(UserId, CreatedAt);

/* ============================================================================
   بخش ۱۵: داده‌های اولیه (Seed Data)
============================================================================ */

INSERT INTO EducationLevels (Name, Code, IsDescriptive, MinGrade, MaxGrade) VALUES
(N'ابتدایی',          'ELEM',  1, 1, 6),
(N'متوسطه اول',       'MID',   0, 7, 9),
(N'متوسطه دوم',       'HIGH',  0, 10, 12),
(N'فنی و حرفه‌ای',     'TECH',  0, 10, 12),
(N'کاردانش',          'CARD',  0, 10, 12);

INSERT INTO AttendanceStatuses (StatusId, Name, Code, IsAbsent, IsTardy) VALUES
(1, N'حاضر',            'PRESENT',  0, 0),
(2, N'غایب غیرموجه',     'ABSENT',   1, 0),
(3, N'غایب موجه',       'EXCUSED',  1, 0),
(4, N'تاخیر',           'TARDY',    0, 1),
(5, N'مرخصی',           'LEAVE',    1, 0),
(6, N'فرار از مدرسه',    'TRUANT',   1, 0);

INSERT INTO Roles (Name, DisplayName) VALUES
('SuperAdmin',      N'مدیر ارشد سیستم'),
('SchoolAdmin',     N'مدیر سیستم مدرسه'),
('Principal',       N'مدیر مدرسه'),
('VicePrincipal',   N'معاون'),
('Teacher',         N'معلم'),
('Counselor',       N'مشاور'),
('Accountant',      N'حسابدار'),
('Student',         N'دانش‌آموز'),
('Parent',          N'ولی دانش‌آموز');

INSERT INTO ExamTypes (Name, DefaultWeight, IsFinal, CountsForGPA) VALUES
(N'آزمون کلاسی',     0.5, 0, 1),
(N'مستمر',          1.0, 0, 1),
(N'پرسش شفاهی',     0.5, 0, 1),
(N'میان‌ترم',        1.5, 0, 1),
(N'نوبت اول',        2.0, 1, 1),
(N'نوبت دوم',        2.0, 1, 1),
(N'پایانی',          3.0, 1, 1),
(N'پودمانی',         1.0, 0, 1),
(N'پروژه',          1.0, 0, 1);

INSERT INTO DisciplinaryTypes (Name, Category, Severity, DefaultScoreImpact) VALUES
(N'تشویق کلاسی',           'R', 1,  0.25),
(N'تشویق مدرسه',           'R', 2,  0.50),
(N'تقدیر کتبی',            'R', 3,  1.00),
(N'تذکر شفاهی',            'P', 1, -0.25),
(N'تذکر کتبی',             'P', 2, -0.50),
(N'احضار اولیا',           'P', 3, -1.00),
(N'تعلیق از تحصیل',         'P', 5, -2.00);
GO

/* ============================================================================
   بخش ۱۶: View های پرکاربرد
============================================================================ */

-- لیست کامل دانش‌آموزان فعلی هر کلاس
CREATE OR ALTER VIEW vw_ActiveStudentEnrollments AS
SELECT 
    e.EnrollmentId,
    s.StudentId, s.StudentCode,
    p.FirstName + N' ' + p.LastName AS FullName,
    p.NationalCode, p.Mobile,
    c.ClassroomId, c.Name AS ClassName,
    sch.SchoolId, sch.Name AS SchoolName,
    ay.AcademicYearId, ay.Title AS AcademicYear,
    g.Name AS GradeName
FROM StudentEnrollments e
JOIN Students s          ON s.StudentId = e.StudentId
JOIN Persons p           ON p.PersonId = s.PersonId
JOIN Classrooms c        ON c.ClassroomId = e.ClassroomId
JOIN Schools sch         ON sch.SchoolId = c.SchoolId
JOIN AcademicYears ay    ON ay.AcademicYearId = c.AcademicYearId
JOIN Grades g            ON g.GradeId = c.GradeId
WHERE e.Status = N'فعال' AND s.IsActive = 1;
GO

-- خلاصه حضور و غیاب هر دانش‌آموز در یک بازه
CREATE OR ALTER VIEW vw_StudentAttendanceSummary AS
SELECT
    a.StudentId,
    a.ClassroomId,
    YEAR(a.AttendanceDate) AS Y,
    MONTH(a.AttendanceDate) AS M,
    SUM(CASE WHEN s.IsAbsent = 1 THEN 1 ELSE 0 END) AS AbsenceCount,
    SUM(CASE WHEN s.IsTardy  = 1 THEN 1 ELSE 0 END) AS TardyCount,
    SUM(CASE WHEN s.Code = 'ABSENT' THEN 1 ELSE 0 END) AS UnexcusedAbsence
FROM Attendances a
JOIN AttendanceStatuses s ON s.StatusId = a.StatusId
GROUP BY a.StudentId, a.ClassroomId, YEAR(a.AttendanceDate), MONTH(a.AttendanceDate);
GO

PRINT N'✅ دیتابیس سیستم مدیریت مدارس با موفقیت ایجاد شد.';

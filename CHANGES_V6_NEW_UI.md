# 🎨 گزارش نسخه v6 — رابط کاربری مدرن

این نسخه شامل **بازطراحی کامل ظاهر** سیستم با ۵ پالت رنگ + Light/Dark Mode + آیکون‌های حرفه‌ای است.

---

## ✨ امکانات جدید

### 1️⃣ سیستم تم کاملاً قابل تنظیم
- **۵ پالت رنگ:** بنفش (پیش‌فرض) / سبز / صورتی / نارنجی / آبی
- **حالت Light و Dark**
- ذخیره در کوکی (با عمر ۱ سال)
- اعمال **قبل از رندر** (بدون فلش)

### 2️⃣ آیکون‌های حرفه‌ای Bootstrap Icons
- جایگزینی **۲۰۰+ emoji** با آیکون‌های وکتوری
- در منوها، دکمه‌ها، عناوین، dropdown ها
- نمایش زیباتر، شفاف‌تر و حرفه‌ای‌تر

### 3️⃣ CSS کاملاً جدید (640+ خط)
- استفاده از **CSS Variables** و **RGB color space** برای تم‌پذیری
- **انیمیشن نرم** برای hover ها و transitions
- **Card Hover Effects** با shadow و translateY
- **Sidebar Active State** با gradient
- **اسکرول‌بار سفارشی** (custom scrollbar)
- **Backdrop Filter** برای topbar
- **Skeleton Loading** برای جداول
- Responsive کامل (Mobile + Tablet + Desktop)

### 4️⃣ Stat Cards بهبود یافته
- خط رنگی کنار هر کارت
- Hover effect با scale و shadow
- آیکون‌های Bootstrap به جای emoji
- تایپوگرافی بهتر

### 5️⃣ سایدبار جدید
- پس‌زمینه روشن (مثل سبک Modern Admin)
- منوی فعال با **gradient + shadow** زیبا
- آیکون‌های وکتوری
- Section header های زیبا

### 6️⃣ Topbar مدرن‌تر
- دکمه‌های دایره‌ای برای bell و theme switcher
- User menu با background round
- دکمه‌های topbar با hover effect

### 7️⃣ DataTables استایل سفارشی
- دکمه‌های رنگی برای Excel/CSV/Print
- pagination زیبا و سازگار با تم
- search input زیبا

---

## 📋 فایل‌های تغییریافته/جدید

### جدید (3 فایل):
1. `wwwroot/css/site.css` — **بازنویسی کامل** (640+ خط)
2. `Controllers/ThemeController.cs` — ذخیره تم در کوکی
3. `Views/Shared/_ThemeSwitcher.cshtml` — UI انتخاب تم

### تغییریافته:
- **۵ Layout** (`_Layout`, `_TeacherLayout`, `_PrincipalLayout`, `_ParentLayout`, `_PortalLayout`)
  - افزودن Bootstrap Icons CDN
  - افزودن script اعمال تم قبل از رندر
  - جایگزینی emoji های منو با Bootstrap Icons
  - افزودن ThemeSwitcher
  - بهبود dropdown کاربر با آیکون
- `_NotificationBell.cshtml` — استفاده از کلاس topbar-btn + bi-bell-fill
- **۸۱ صفحه** Page Headers — `<h1>📊 متن</h1>` → `<h1><i class="bi bi-..."></i> متن</h1>`
- **۲۴ صفحه** Card Headers — `<h2>📚 متن</h2>` → آیکون
- **۸۳ صفحه** دکمه‌ها — `<button>📤 ارسال</button>` → `<button><i></i> ارسال</button>`
- **۱۶ صفحه** Stat Icons — `<div class="stat-icon">📚</div>` → آیکون

---

## 🎨 پالت رنگ‌ها

| تم | رنگ اصلی | کاربرد پیشنهادی |
|----|----------|------------------|
| 🟣 **Indigo** | #6366f1 | پیش‌فرض، مناسب همه مدارس |
| 🟢 **Emerald** | #10b981 | مدارس طبیعت‌گرا |
| 🌹 **Rose** | #f43f5e | مدارس دخترانه |
| 🟠 **Amber** | #f59e0b | مدارس فعال و انرژیک |
| 🌊 **Ocean** | #0ea5e9 | مدارس فنی و علمی |

---

## 🚀 برای استفاده

### نیازی به Migration نیست!

```powershell
dotnet run
```

**⚠️ خیلی مهم: حتماً Ctrl+F5 بزن** تا کش CSS قدیمی پاک بشه.

### نحوه تغییر تم:
1. روی آیکون 🎨 در topbar (کنار زنگوله) کلیک کن
2. یه رنگ انتخاب کن
3. حالت Light یا Dark انتخاب کن
4. به‌صورت خودکار **برای همه صفحات بعدی** اعمال میشه

---

## 🎯 نکات فنی

### CSS Variables استفاده شده:
- `rgb(var(--c-primary))` — رنگ اصلی تم
- `rgb(var(--surface))` — پس‌زمینه کارت‌ها
- `rgb(var(--text))` — رنگ متن
- `rgb(var(--border))` — رنگ خطوط
- و... (به `site.css` نگاه کن)

### اضافه کردن آیکون جدید به منو:
کافیه از وب‌سایت [Bootstrap Icons](https://icons.getbootstrap.com/) آیکون رو پیدا کنی و کلاسش رو بگذاری:
```html
<a class="menu-item">
    <i class="menu-icon bi bi-rocket-fill"></i>
    <span class="menu-text">صفحه جدید</span>
</a>
```

### اضافه کردن تم جدید:
به `site.css` بخش "Color Palettes" برو و یه `[data-theme="نام"]` جدید اضافه کن. بعد در `_ThemeSwitcher.cshtml` دکمه‌اش رو اضافه کن.

---

## 🧪 تست

1. `Ctrl+F5` بعد از اجرا
2. روی آیکون 🎨 در topbar کلیک کن
3. تم‌های مختلف رو امتحان کن
4. Dark Mode رو امتحان کن
5. به صفحات مختلف برو و ببین چقدر زیبا شده

---

## 💡 موارد جا مونده (اگر خواستی)

این موارد UI رو می‌تونم بعداً اضافه کنم:
- 🎨 افزودن انیمیشن‌های پیشرفته‌تر
- 📱 بهبود ظاهر برای موبایل (نسخه فعلی هم Responsive هست)
- 🔍 Search bar سراسری در topbar
- 🌐 پشتیبانی از RTL/LTR (الان فقط RTL)
- 📊 نمودارهای زیباتر با تم سازگار

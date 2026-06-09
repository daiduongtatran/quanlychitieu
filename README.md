# 💰 Quản Lý Chi Tiêu - Spending Management System

**Hệ thống quản lý chi tiêu cá nhân** được xây dựng bằng **ASP.NET Core MVC** với **SQL Server** backend.  
Cho phép người dùng quản lý ngân sách, ghi chép giao dịch, theo dõi chi tiêu và phân tích báo cáo tài chính.

---

## 📋 Nội Dung

1. [Tổng Quan](#tổng-quan)
2. [Architecture & Tech Stack](#architecture--tech-stack)
3. [Luồng Đăng Nhập Chi Tiết](#luồng-đăng-nhập-chi-tiết)
4. [Các Tính Năng Chính](#các-tính-năng-chính)
5. [Cấu Trúc Dự Án](#cấu-trúc-dự-án)
6. [Database Schema](#database-schema)
7. [Setup & Installation](#setup--installation)
8. [Chạy Project](#chạy-project)
9. [API Endpoints](#api-endpoints)
10. [Luồng Hoạt Động Chi Tiết](#luồng-hoạt-động-chi-tiết)

---

## 🎯 Tổng Quan

### Mục Đích
Giúp người dùng:
- 📝 **Ghi chép giao dịch** (Thu/Chi) theo danh mục
- 💰 **Quản lý ngân sách** (tháng/danh mục)
- 📊 **Xem báo cáo** chi tiêu qua biểu đồ
- 🔔 **Nhận thông báo** khi vượt ngân sách
- 📱 **Dashboard** tổng hợp tình hình tài chính

### Tính Năng Chính
✅ Đăng ký / Đăng nhập  
✅ Quản lý danh mục chi tiêu  
✅ Thêm giao dịch (Thu/Chi) với validation ngân sách  
✅ Thiết lập ngân sách (tháng/danh mục)  
✅ Xem lịch sử giao dịch với filter  
✅ Báo cáo chi tiêu (Pie chart + Trend)  
✅ Thông báo cảnh báo  
✅ Xem số dư tài khoản  

---

## 🏗️ Architecture & Tech Stack

### Frontend
- **Framework**: ASP.NET Core 10.0 MVC
- **View Engine**: Razor (`.cshtml`)
- **CSS**: Bootstrap 5 + Custom CSS (dashboard.css)
- **JavaScript**: Chart.js (biểu đồ), jQuery

### Backend
- **Runtime**: .NET 10.0
- **Language**: C#
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: Session-based (Distributed Memory Cache)

### Key Libraries
```xml
<!-- Program.cs dependencies -->
- DbContext (AppDbContext)
- IUserService (UserService)
- IExpenseTrackingService (ExpenseTrackingService)
- INotificationService (NotificationService)
- Session (30 phút timeout)
```

---

## 🔐 Luồng Đăng Nhập Chi Tiết

### 1️⃣ **Đăng Ký (Register)**

#### File Liên Quan:
- **View**: `Frontend/Views/Pages/Account/Register.cshtml`
- **Controller**: `Frontend/Controllers/AccountController.cs` → Action: `Register()`
- **Service**: `Backend/Services/UserService.cs` → Method: `RegisterUserAsync()`
- **Model**: `Frontend/Models/RegisterModel.cs`
- **Database**: `NguoiDung` table

#### Quy Trình Đăng Ký:
```
USER FORM
   ↓
Register.cshtml
   ↓ (POST)
AccountController.Register()
   ├─ Validate: Email, FullName, Password ≥ 6 ký tự
   ├─ Extract Username: từ email (phần trước @)
   ├─ Call: UserService.RegisterUserAsync()
   │   ├─ Check email đã tồn tại? (EmailExistsAsync)
   │   ├─ Check username đã tồn tại? (UsernameExistsAsync)
   │   ├─ Hash password: SHA256
   │   ├─ Create NguoiDung:
   │   │   - MaNguoiDung (auto increment)
   │   │   - TenDangNhap
   │   │   - Email (lowercase)
   │   │   - HoTen
   │   │   - MatKhauHash (SHA256)
   │   │   - SoDuTaiKhoan = 0
   │   │   - NgayTao = DateTime.Now
   │   ├─ Save to DB
   │   └─ Create Default Categories (ăn uống, xăng, quần áo, etc.)
   │
   └─ Success → Redirect to Login
```

#### Validation:
| Field | Rule |
|-------|------|
| Email | Không trống, email format, unique |
| FullName | Không trống |
| Password | ≥ 6 ký tự |
| Username | Unique (extract từ email) |

#### Default Categories Created:
```csharp
// Mỗi user mới tự động được tạo:
- Thu (LoaiDanhMuc = "Thu")
- Chi: Ăn Uống, Xăng, Quần Áo, etc.
```

---

### 2️⃣ **Đăng Nhập (Login)**

#### File Liên Quan:
- **View**: `Frontend/Views/Pages/Account/Login.cshtml`
- **Controller**: `Frontend/Controllers/AccountController.cs` → Action: `Login()`
- **Service**: `Backend/Services/UserService.cs` → Method: `LoginUserAsync()`
- **Model**: `Frontend/Models/LoginModel.cs`
- **Session**: ASP.NET Core Distributed Memory Cache (30 min timeout)

#### Quy Trình Đăng Nhập:
```
USER LOGIN FORM (Email + Password)
   ↓
Login.cshtml
   ↓ (POST + [ValidateAntiForgeryToken])
AccountController.Login(LoginModel)
   ├─ Validate ModelState
   ├─ Call: UserService.LoginUserAsync(email, password)
   │   ├─ Find user by email (case-insensitive)
   │   ├─ Verify password:
   │   │   - Hash input password with SHA256
   │   │   - Compare with MatKhauHash from DB
   │   ├─ If Match:
   │   │   └─ Return (success: true, user object)
   │   └─ If No Match:
   │       └─ Return (success: false, error message)
   │
   ├─ If Success:
   │   ├─ Set Session Variables:
   │   │   - UserId = user.MaNguoiDung (int)
   │   │   - UserEmail = user.Email (string)
   │   │   - UserName = user.HoTen (string)  [For Avatar Display]
   │   ├─ Set TempData["SuccessMessage"]
   │   └─ Redirect → Dashboard
   │
   └─ If Fail:
       ├─ Add ModelError
       └─ Return Login View
```

#### Password Hashing:
```csharp
// UserService.cs
private string HashPassword(string password)
{
    using (var sha256 = SHA256.Create())
    {
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

private bool VerifyPassword(string password, string hash)
{
    var hashOfInput = HashPassword(password);
    return hashOfInput == hash?.Trim();
}
```

#### Session Configuration:
```csharp
// Program.cs
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // 30 phút timeout
    options.Cookie.HttpOnly = true;                  // Chỉ server access
    options.Cookie.IsEssential = true;               // GDPR compliant
});

app.UseSession();  // Middleware
```

#### Truy Cập Session ở Controller:
```csharp
// Trong bất kỳ controller action:
int? userId = HttpContext.Session.GetInt32("UserId");
string userEmail = HttpContext.Session.GetString("UserEmail");
string userName = HttpContext.Session.GetString("UserName");

// Check if logged in:
if (userId == null)
    return RedirectToAction("Login", "Account");
```

---

### 3️⃣ **Đăng Xuất (Logout)**

#### File Liên Quan:
- **Controller**: `Frontend/Controllers/AccountController.cs` → Action: `Logout()`

#### Quy Trình:
```
Logout Button
   ↓
AccountController.Logout()
   ├─ HttpContext.Session.Clear()  // Xóa tất cả session vars
   ├─ Set TempData["SuccessMessage"] = "Đăng xuất thành công!"
   └─ Redirect → Login
```

---

## 🎨 Các Tính Năng Chính

### 1. Dashboard (Trang Chủ)

**File**: `Frontend/Views/Home/Dashboard.cshtml`  
**Controller**: `Frontend/Controllers/HomeController.cs` → Action: `Dashboard()`

**Hiển Thị**:
```
┌─────────────────────────────────────────┐
│  4 Stat Boxes (White-Gray Theme):       │
│  - Chi Tiêu Hôm Nay                     │
│  - Chi Tiêu Tháng Này                   │
│  - Ngân Sách Tháng Này                  │
│  - Tổng Giao Dịch                       │
└─────────────────────────────────────────┘
```

**Logic**:
```csharp
Dashboard()
  ├─ Check userId in session
  ├─ Get user info from DB
  ├─ Calculate:
  │   ├─ Today's expenses (GiaoDich.NgayGiaoDich = today, laThu = false)
  │   ├─ Month expenses (GiaoDich.NgayGiaoDich = current month)
  │   ├─ Budget remaining (NganSach where NgayBatDau ≤ today ≤ NgayKetThuc)
  │   └─ Total transactions count
  ├─ ViewBag.UserName (for avatar display)
  └─ Return Dashboard view
```

---

### 2. Quản Lý Ngân Sách

**File**: `Frontend/Views/Home/Dashboard.cshtml` (Form submit)  
**Controller**: `Frontend/Controllers/HomeController.cs` → Action: `ThemNganSach()`  
**Database**: `NganSach` table

#### Cách Hoạt Động:
```
Ngân Sách Chung (Tháng):
  - MaDanhMuc = NULL
  - SoTienNganSachThang = Tổng ngân sách tháng (GỐC - không đổi)
  - SoTienHanMuc = Ngân sách còn lại (THAY ĐỔI theo chi tiêu)

Ngân Sách Danh Mục:
  - MaDanhMuc = Category ID
  - SoTienNganSachThang = NULL (không dùng)
  - SoTienHanMuc = Hạn mức cho danh mục này

Ví Dụ:
  - User set: "Tháng 6 tôi có 20,000,000 VNĐ để tiêu"
    → NganSach: [MaDanhMuc=NULL, SoTienNganSachThang=20M, SoTienHanMuc=20M]
  
  - User chi: "Ăn uống 500k"
    → GiaoDich created, NganSach update
    → NganSach: [SoTienHanMuc = 20M - 500k = 19.5M]

  - User set: "Ăn uống tháng 6: max 3,000,000"
    → NganSach: [MaDanhMuc=DanhMucAnUong, SoTienHanMuc=3M]
```

#### Quy Trình ThemNganSach():
```csharp
ThemNganSach(
    decimal SoTienHanMuc,      // Giá trị ngân sách
    int? MaDanhMuc,             // NULL = tháng, ID = danh mục cụ thể
    DateTime NgayBatDau,
    DateTime NgayKetThuc
)
  ├─ Check userId exists in session
  ├─ If MaDanhMuc == NULL (Ngân sách tháng):
  │   ├─ Check if budget exists for this month
  │   ├─ If exists: Update SoTienNganSachThang, SoTienHanMuc (recalculate)
  │   └─ If not: Create new NganSach
  │
  └─ Else (Ngân sách danh mục):
      ├─ Check if budget exists for this category in date range
      ├─ If exists: Update SoTienHanMuc (reset/replace)
      └─ If not: Create new NganSach
```

---

### 3. Thêm Giao Dịch (Transaction)

**File**: `Frontend/Views/Home/Dashboard.cshtml` (Form)  
**Controller**: `Frontend/Controllers/HomeController.cs` → Action: `ThemGiaoDich()`  
**Database**: `GiaoDich` table

#### Quy Trình Thêm Giao Dịch:
```
ThemGiaoDich(
    decimal SoTien,           // Số tiền
    int MaDanhMuc,            // Danh mục (Thu/Chi)
    DateTime NgayGiaoDich,
    string GhiChu,            // Ghi chú
    bool IsDinhKy,            // Có định kỳ không
    string TanSuat,           // Nếu định kỳ: "Hàng ngày", "Hàng tuần", etc.
    DateTime? NgayKetThuc     // Ngày kết thúc của giao dịch định kỳ
)
  │
  ├─ STEP 1: Check userId in session
  ├─ STEP 2: Validate budget
  │   └─ Call: ExpenseTrackingService.CanAddTransactionAsync()
  │       └─ Check if amount exceeds budget (NganSach.SoTienHanMuc)
  │       └─ If exceed: Reject + Create ThongBao warning
  │
  ├─ STEP 3: Create GiaoDich record
  │   └─ Insert: {SoTien, MaDanhMuc, NgayGiaoDich, GhiChu, MaNguoiDung, IsDinhKy, TanSuat, NgayKetThuc}
  │
  ├─ STEP 4: Update NguoiDung.SoDuTaiKhoan
  │   ├─ If "Thu" (Income): SoDuTaiKhoan += SoTien
  │   └─ If "Chi" (Expense): SoDuTaiKhoan -= SoTien
  │
  ├─ STEP 5: Update NganSach.SoTienHanMuc
  │   ├─ Find budget for this date range
  │   ├─ If "Thu": SoTienHanMuc += SoTien (budget tăng)
  │   └─ If "Chi": SoTienHanMuc -= SoTien (budget giảm)
  │
  └─ STEP 6: Save to DB + Log + Redirect to Dashboard
```

#### Budget Validation Logic:
```csharp
// ExpenseTrackingService.cs
async Task<(bool isValid, string message)> CanAddTransactionAsync(...)
{
    // Check general budget:
    var generalBudget = GetBudgetForDate(userId, null, transactionDate);
    if (generalBudget.SoTienHanMuc < amount)
        return (false, "Vượt ngân sách chung!");
    
    // Check category budget:
    var categoryBudget = GetBudgetForDate(userId, categoryId, transactionDate);
    if (categoryBudget != null && categoryBudget.SoTienHanMuc < amount)
        return (false, "Vượt ngân sách danh mục!");
    
    return (true, "OK");
}
```

---

### 4. Xem Lịch Sử Giao Dịch

**File**: `Frontend/Views/Home/Transactions.cshtml`  
**Controller**: `Frontend/Controllers/HomeController.cs` → Action: `Transactions(string searchGhiChu, string searchNgay)`

#### Tính Năng:
- Filter by note (GhiChu)
- Filter by date (NgayGiaoDich)
- Display tất cả transactions + summary stats
- Removed: Excel export button ✅

#### SQL Query:
```sql
SELECT * FROM GiaoDich
WHERE MaNguoiDung = @userId
  AND (GhiChu LIKE @searchGhiChu OR NgayGiaoDich = @searchNgay)
ORDER BY NgayGiaoDich DESC
```

---

### 5. Báo Cáo Chi Tiêu (Reports)

**File**: `Frontend/Views/BaoCao/Index.cshtml`  
**Controller**: `Frontend/Controllers/BaoCaoController.cs` → Action: `Index()`

#### Hiển Thị:
```
┌─────────────────────────────────────────┐
│  Stats (White-Gray theme):              │
│  - Tổng Thu (Income)                    │
│  - Tổng Chi (Expense)                   │
│  - Số Dư (Balance)                      │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  Pie Chart: Chi tiêu theo danh mục      │
│  (Breakdown by category)                │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  Line Chart: Xu hướng chi tiêu          │
│  (Daily income vs expense trend)        │
└─────────────────────────────────────────┘
```

#### Logic:
```csharp
Index()
  ├─ Get all transactions for current month
  ├─ Group by DanhMuc:
  │   └─ Create pie chart data
  ├─ Group by date:
  │   └─ Create trend chart data
  ├─ Calculate totals:
  │   ├─ tongThu (sum of income)
  │   ├─ tongChi (sum of expenses)
  │   └─ soDu (balance)
  └─ ViewBag.UserName (avatar)
```

---

### 6. Danh Mục Chi Tiêu

**File**: `Frontend/Views/Pages/Danhmuc/Index.cshtml`  
**Controller**: `Frontend/Controllers/DanhmucController.cs`  
**Database**: `DanhMuc` table

#### Tính Năng:
- Create new category (Danh mục)
- Delete category
- Filter by user

#### Loại Danh Mục:
- `"Thu"` / `"Thu Nhập"` = Income
- Các loại Chi: Ăn Uống, Xăng, Quần Áo, etc.

---

## 📊 Database Schema

### 1. **NguoiDung** (Users)
```sql
CREATE TABLE NguoiDung (
    MaNguoiDung INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhauHash NVARCHAR(MAX) NOT NULL,            -- SHA256 hash
    Email NVARCHAR(100) NOT NULL UNIQUE,
    HoTen NVARCHAR(100),
    NgayTao DATETIME DEFAULT GETDATE(),
    SoDuTaiKhoan DECIMAL(18,2) DEFAULT 0           -- Account balance
);
```

### 2. **DanhMuc** (Categories)
```sql
CREATE TABLE DanhMuc (
    MaDanhMuc INT PRIMARY KEY IDENTITY(1,1),
    TenDanhMuc NVARCHAR(100) NOT NULL,
    LoaiDanhMuc NVARCHAR(50),                      -- "Thu" or "Chi"
    BieuTuong NVARCHAR(50),                        -- Icon name
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);
```

### 3. **GiaoDich** (Transactions)
```sql
CREATE TABLE GiaoDich (
    MaGiaoDich INT PRIMARY KEY IDENTITY(1,1),
    SoTien DECIMAL(18,2) NOT NULL,
    GhiChu NVARCHAR(MAX),
    NgayGiaoDich DATETIME DEFAULT GETDATE(),
    MaDanhMuc INT NOT NULL FOREIGN KEY REFERENCES DanhMuc(MaDanhMuc),
    MaNguoiDung INT NOT NULL FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung),
    IsDinhKy BIT DEFAULT 0,                        -- Recurring transaction?
    TanSuat NVARCHAR(50),                          -- "Hàng ngày", "Hàng tuần", etc.
    NgayKetThuc DATETIME                           -- End date if recurring
);
```

### 4. **NganSach** (Budgets)
```sql
CREATE TABLE NganSach (
    MaNganSach INT PRIMARY KEY IDENTITY(1,1),
    SoTienHanMuc DECIMAL(18,2) NOT NULL,           -- Remaining amount
    SoTienNganSachThang DECIMAL(18,2),             -- Original budget (for general only)
    NgayBatDau DATETIME NOT NULL,
    NgayKetThuc DATETIME NOT NULL,
    MaDanhMuc INT,                                 -- NULL = general, ID = category
    MaNguoiDung INT NOT NULL FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);
```

### 5. **ThongBao** (Notifications)
```sql
CREATE TABLE ThongBao (
    MaThongBao INT PRIMARY KEY IDENTITY(1,1),
    TieuDe NVARCHAR(200) NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    LoaiThongBao NVARCHAR(50),                     -- "GiaoDich", "SapHetTien", "NganSach"
    BieuTuong NVARCHAR(10),                        -- Emoji
    DaDoc BIT DEFAULT 0,
    NgayTao DATETIME DEFAULT GETDATE(),
    MaNguoiDung INT NOT NULL FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung)
);
```

### Relationships Diagram:
```
NguoiDung (1) ──────────────┐
                            │
                    ┌───────┴────────┬─────────────┬──────────────┐
                    │                │             │              │
                 (N) DanhMuc      (N) GiaoDich   (N) NganSach   (N) ThongBao
```

---

## 🚀 Setup & Installation

### Điều Kiện Tiên Quyết:
- **.NET SDK 10.0** hoặc cao hơn
- **SQL Server** (Express hoặc Full)
- **Visual Studio 2022** hoặc VS Code

### Bước 1: Clone Repository
```bash
git clone https://github.com/your-repo/spending-management.git
cd spending-management
```

### Bước 2: Cấu Hình Connection String

**File**: `Frontend/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=QuanLyChiTieu;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

Thay `YOUR_SERVER`:
- **Local**: `(local)` hoặc `.`
- **SQL Express**: `.\SQLEXPRESS`
- **Remote**: `server.domain.com` hoặc IP address

### Bước 3: Restore NuGet Packages
```bash
dotnet restore
```

### Bước 4: Database Setup

**Option A - Automatic (Recommended)**:
```csharp
// Program.cs already has:
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();  // Auto-create if not exists
}
```

**Option B - Manual SQL Script**:
```bash
sqlcmd -S YOUR_SERVER -U sa -P YOUR_PASSWORD -d QuanLyChiTieu -i ResetDatabase.sql
```

### Bước 5: Build Project
```bash
dotnet build
```

---

## 🎮 Chạy Project

### Option 1: Visual Studio 2022
1. Open `SpendingManagement.slnx` in Visual Studio
2. Set **Frontend** as Startup Project (right-click → Set as Startup Project)
3. Press **F5** or click **Run**
4. Browser automatically opens at `https://localhost:7001` (or assigned port)

### Option 2: Command Line
```bash
cd Frontend
dotnet run
```

Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
      Now listening on: http://localhost:5001
```

### Option 3: VS Code
```bash
# Terminal
cd Frontend
dotnet watch run
```
(Auto-reload on file changes)

---

## 🔗 API Endpoints

### Authentication
| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/Account/Register` | GET/POST | Register new user | ❌ |
| `/Account/Login` | GET/POST | Login | ❌ |
| `/Account/Logout` | GET | Logout | ✅ |
| `/Account/ForgotPassword` | GET | Forgot password (Incomplete) | ❌ |

### Dashboard & Transactions
| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/Home/Dashboard` | GET | Main dashboard | ✅ |
| `/Home/ThemGiaoDich` | POST | Add transaction | ✅ |
| `/Home/Transactions` | GET | View transaction history | ✅ |
| `/Home/ThemNganSach` | POST | Set budget | ✅ |

### Reports
| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/BaoCao/Index` | GET | Financial report | ✅ |

### Categories
| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/Danhmuc/Index` | GET | List categories | ✅ |
| `/Danhmuc/Create` | POST | Create category | ✅ |
| `/Danhmuc/Delete/{id}` | POST | Delete category | ✅ |

### Notifications
| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/Notification/GetNotifications` | GET | Get all notifications | ✅ |
| `/Notification/MarkAsRead` | POST | Mark notification as read | ✅ |

---

## 📂 Luồng Hoạt Động Chi Tiết

### Scenario: User đăng nhập & tạo giao dịch

```
┌─────────────────────────────────────────────────────────┐
│ 1. USER VISITS WEBSITE                                  │
└─────────────────────────────────────────────────────────┘
        ↓
    app.MapControllerRoute(...) 
    default controller = "Home"
    default action = "Index"
        ↓
    HomeController.Index()
    ├─ if userId in session → Dashboard
    └─ else → Redirect to AccountController.Login()

┌─────────────────────────────────────────────────────────┐
│ 2. LOGIN PAGE LOADS                                     │
└─────────────────────────────────────────────────────────┘
    GET /Account/Login
        ↓
    AccountController.Login() [HttpGet]
        ↓
    Views/Pages/Account/Login.cshtml
    ├─ Email input field
    ├─ Password input field
    └─ Submit button (POST)

┌─────────────────────────────────────────────────────────┐
│ 3. USER SUBMITS LOGIN FORM                              │
└─────────────────────────────────────────────────────────┘
    POST /Account/Login
        ↓ [ValidateAntiForgeryToken]
    AccountController.Login(LoginModel)
        ├─ Validate ModelState
        ├─ Call UserService.LoginUserAsync(email, password)
        │   ├─ Query: NguoiDung where Email = (case-insensitive)
        │   ├─ Hash input password with SHA256
        │   ├─ Compare with MatKhauHash
        │   └─ Return (success, message, user object)
        │
        ├─ If success:
        │   ├─ Session["UserId"] = user.MaNguoiDung
        │   ├─ Session["UserEmail"] = user.Email
        │   ├─ Session["UserName"] = user.HoTen
        │   ├─ TempData["SuccessMessage"] = "Đăng nhập thành công!"
        │   └─ Redirect → /Home/Dashboard
        │
        └─ If fail:
            ├─ ModelState.AddModelError
            └─ Return Login view with errors

┌─────────────────────────────────────────────────────────┐
│ 4. DASHBOARD LOADS                                      │
└─────────────────────────────────────────────────────────┘
    Redirect: /Home/Dashboard
        ↓
    HomeController.Dashboard()
        ├─ userId = Session["UserId"]  (not null ✓)
        ├─ Query NguoiDung by MaNguoiDung
        ├─ ViewBag.UserName = user.HoTen  (for avatar)
        ├─ Calculate metrics:
        │   ├─ Today's expenses (WHERE NgayGiaoDich = today AND LoaiDanhMuc != "Thu")
        │   ├─ Month expenses (WHERE MONTH(NgayGiaoDich) = current month)
        │   ├─ Budget remaining (NganSach where date range includes today)
        │   └─ Total transaction count
        │
        └─ Return Views/Home/Dashboard.cshtml with data

    Views/Home/Dashboard.cshtml Renders:
        ├─ Navigation bar with avatar (first letter of UserName)
        ├─ 4 stat boxes (white-gray theme)
        ├─ Form to add transaction
        ├─ Form to set budget
        └─ Charts/widgets

┌─────────────────────────────────────────────────────────┐
│ 5. USER CREATES TRANSACTION                             │
└─────────────────────────────────────────────────────────┘
    Dashboard Form Submit:
        - SoTien: 500000 (500k VNĐ)
        - MaDanhMuc: 2 (Ăn Uống)
        - NgayGiaoDich: Today
        - GhiChu: "Ăn cơm trưa"
        ↓ (POST)
    /Home/ThemGiaoDich
        ↓
    HomeController.ThemGiaoDich(500000, 2, today, "Ăn cơm trưa", false, null, null)
        │
        ├─ STEP 1: Get userId from Session
        ├─ STEP 2: Validate Budget
        │   └─ ExpenseTrackingService.CanAddTransactionAsync()
        │       ├─ Get general budget for today
        │       ├─ Get category budget for today
        │       ├─ Check: general.SoTienHanMuc >= 500k? ✓
        │       ├─ Check: category.SoTienHanMuc >= 500k? ✓
        │       └─ Return (true, "OK")
        │
        ├─ STEP 3: Create GiaoDich
        │   └─ DB INSERT:
        │       {MaGiaoDich: auto, SoTien: 500k, MaDanhMuc: 2, 
        │        NgayGiaoDich: today, GhiChu: "...", 
        │        MaNguoiDung: userId, IsDinhKy: false}
        │
        ├─ STEP 4: Update Account Balance
        │   ├─ Query NguoiDung by MaNguoiDung = userId
        │   ├─ danhMuc.LoaiDanhMuc = "Chi"? (not "Thu")
        │   ├─ Subtract: SoDuTaiKhoan -= 500k
        │   └─ DB UPDATE NguoiDung
        │
        ├─ STEP 5: Update Budget
        │   ├─ Query NganSach where MaNguoiDung = userId, 
        │   │            MaDanhMuc = null, NgayBatDau ≤ today ≤ NgayKetThuc
        │   ├─ Subtract: SoTienHanMuc -= 500k
        │   │   (SoTienNganSachThang remains unchanged)
        │   └─ DB UPDATE NganSach
        │
        ├─ STEP 6: Save to DB
        │   └─ _context.SaveChangesAsync()
        │
        └─ Redirect: /Home/Dashboard
            ↓
        Success message displays on dashboard

┌─────────────────────────────────────────────────────────┐
│ 6. VIEW TRANSACTION HISTORY                             │
└─────────────────────────────────────────────────────────┘
    /Home/Transactions
        ↓
    HomeController.Transactions(searchGhiChu, searchNgay)
        ├─ userId from Session
        ├─ Query all GiaoDich where MaNguoiDung = userId
        ├─ Apply filters if provided:
        │   ├─ WHERE GhiChu LIKE searchGhiChu
        │   └─ WHERE NgayGiaoDich = searchNgay
        │
        └─ Return Views/Home/Transactions.cshtml

    Views/Home/Transactions.cshtml Shows:
        ├─ Transaction list (table)
        ├─ Summary stats (total income, expense, balance)
        ├─ Filter form (search by note, date)
        └─ No Excel export button ✓

┌─────────────────────────────────────────────────────────┐
│ 7. VIEW REPORT                                          │
└─────────────────────────────────────────────────────────┘
    /BaoCao/Index
        ↓
    BaoCaoController.Index()
        ├─ userId from Session
        ├─ Query GiaoDich for current month
        ├─ Group by DanhMuc:
        │   └─ Create pie chart data
        ├─ Group by date:
        │   └─ Create trend chart data
        ├─ ViewBag.UserName (avatar)
        │
        └─ Return Views/BaoCao/Index.cshtml

    Views/BaoCao/Index.cshtml Renders:
        ├─ 3 stat boxes (Revenue, Expense, Balance)
        ├─ Pie chart: Expense breakdown by category
        ├─ Line chart: Daily income/expense trend
        └─ Chart.js visualization

┌─────────────────────────────────────────────────────────┐
│ 8. LOGOUT                                               │
└─────────────────────────────────────────────────────────┘
    Click "Logout"
        ↓
    /Account/Logout
        ↓
    AccountController.Logout()
        ├─ HttpContext.Session.Clear()
        │   └─ Delete all session variables
        ├─ TempData["SuccessMessage"] = "Đăng xuất thành công!"
        │
        └─ Redirect: /Account/Login
            ↓
        User returns to login page
```

---

## 🔒 Security Considerations

### ✅ Implemented:
- ✅ Password hashing (SHA256)
- ✅ Session-based authentication
- ✅ CSRF protection ([ValidateAntiForgeryToken])
- ✅ HTTPS redirect
- ✅ Session timeout (30 minutes)
- ✅ HttpOnly cookies

### ⚠️ TODO (Production Improvements):
- [ ] Add [Authorize] attributes to all sensitive actions
- [ ] Implement rate limiting for login attempts
- [ ] Add soft delete support (recovery)
- [ ] Implement audit logging
- [ ] Add password reset (email verification)
- [ ] Implement 2FA (Two-Factor Authentication)

---

## 📝 Notes

### File Organization (Latest):
```
Frontend/
  ├─ Controllers/
  │   ├─ AccountController.cs      (Login/Register/Logout)
  │   ├─ HomeController.cs         (Dashboard/Transactions/Budget)
  │   ├─ BaoCaoController.cs       (Reports)
  │   ├─ DanhmucController.cs      (Categories)
  │   └─ NotificationController.cs (Notifications)
  │
  ├─ Views/
  │   ├─ Home/
  │   │   ├─ Dashboard.cshtml      (Main dashboard)
  │   │   └─ Transactions.cshtml   (Transaction history)
  │   │
  │   ├─ Pages/
  │   │   ├─ Account/
  │   │   │   ├─ Login.cshtml
  │   │   │   ├─ Register.cshtml
  │   │   │   └─ ForgotPassword.cshtml (incomplete)
  │   │   ├─ BaoCao/
  │   │   │   └─ Index.cshtml
  │   │   └─ Danhmuc/
  │   │       └─ Index.cshtml
  │   │
  │   └─ Shared/
  │       ├─ _Layout.cshtml        (Main layout)
  │       ├─ _DashboardLayout.cshtml
  │       └─ Error.cshtml
  │
  ├─ Services/
  │   ├─ BudgetService.cs          (ExpenseTrackingService)
  │   └─ NotificationService.cs
  │
  ├─ wwwroot/
  │   ├─ css/
  │   │   ├─ site.css
  │   │   ├─ dashboard.css         (White-gray theme)
  │   │   └─ notification.css
  │   └─ js/
  │       ├─ site.js
  │       └─ notification.js
  │
  └─ Program.cs                    (App configuration)

Backend/
  ├─ Models/
  │   ├─ NguoiDung.cs             (User)
  │   ├─ GiaoDich.cs              (Transaction)
  │   ├─ DanhMuc.cs               (Category)
  │   ├─ NganSach.cs              (Budget)
  │   └─ ThongBao.cs              (Notification)
  │
  ├─ Services/
  │   ├─ IUserService.cs
  │   └─ UserService.cs           (Authentication logic)
  │
  └─ Data/
      └─ AppDbContext.cs          (EF Core DbContext)
```

### Key Configuration Files:
- `Frontend/appsettings.json` - Connection string
- `Frontend/Properties/launchSettings.json` - Port configuration
- `Frontend/Program.cs` - Dependency injection & middleware setup

---

## 🐛 Known Issues & TODO

| Issue | Priority | Status |
|-------|----------|--------|
| ForgotPassword not implemented | HIGH | ❌ TODO |
| Recurring transactions not auto-created | HIGH | ❌ TODO |
| No soft delete support | MEDIUM | ❌ TODO |
| No rate limiting on login | MEDIUM | ❌ TODO |
| Missing audit trail | MEDIUM | ❌ TODO |
| Session timeout not handled gracefully | LOW | ⚠️ PARTIAL |

---

## 📚 Resources

- [ASP.NET Core MVC Documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Session & Auth in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)
- [Chart.js Documentation](https://www.chartjs.org/)

---

## 📧 Contact & Support

For issues or questions, please check the code documentation in files or create an issue.

**Last Updated**: June 10, 2026

---

**Made with ❤️ for Personal Finance Management**

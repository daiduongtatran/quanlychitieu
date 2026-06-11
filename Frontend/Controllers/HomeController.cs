using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Frontend.Services;

namespace Frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        private readonly IExpenseTrackingService _expenseTrackingService;
        private readonly INotificationService _notificationService;

        public HomeController(
            AppDbContext context, 
            ILogger<HomeController> logger, 
            IUserService userService,
            IExpenseTrackingService expenseTrackingService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
            _expenseTrackingService = expenseTrackingService;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            bool isConnected = _context.Database.CanConnect();
            ViewBag.Status = isConnected ? "Kết nối Database thành công!" : "Kết nối Database thất bại!";
            
            var chiTieuTheoDanhMuc = _context.GiaoDich
                .Where(g => g.MaDanhMuc > 0) 
                .Include(g => g.DanhMuc)         
                .GroupBy(g => new { 
                    g.MaDanhMuc, 
                    TenDM = g.DanhMuc.TenDanhMuc,  
                    Icon = g.DanhMuc.BieuTuong    
                })
                .Select(group => new {
                    MaDanhMuc = group.Key.MaDanhMuc,
                    TenDanhMuc = group.Key.TenDM,
                    BieuTuong = group.Key.Icon,
                    TongTien = group.Sum(g => g.SoTien)
                })
                .ToList();

            ViewBag.ChiTieuTheoDanhMuc = chiTieuTheoDanhMuc;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ThemNganSach(
            string LoaiNganSach, 
            int? MaDanhMuc, 
            decimal SoTienHanMuc, 
            DateTime NgayBatDau, 
            DateTime NgayKetThuc)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (LoaiNganSach == "Thang")
                {
                    var currentDate = DateTime.Now;
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                    
                    var nganSachCu = await _context.NganSach
                        .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                               n.MaDanhMuc == null &&
                               n.NgayBatDau <= monthEnd && 
                               n.NgayKetThuc >= monthStart);
                    
                    if (nganSachCu != null)
                    {
                        decimal soTienDaChiTieu = (nganSachCu.SoTienNganSachThang ?? 0) - nganSachCu.SoTienHanMuc;
                        
                        nganSachCu.SoTienNganSachThang = SoTienHanMuc;
                        nganSachCu.SoTienHanMuc = SoTienHanMuc - soTienDaChiTieu;
                        nganSachCu.NgayBatDau = NgayBatDau;
                        nganSachCu.NgayKetThuc = NgayKetThuc;
                        _logger.LogInformation($"[User {userId}] Cập nhật ngân sách tháng: {SoTienHanMuc:N0}đ (đã chi: {soTienDaChiTieu:N0}đ, còn: {nganSachCu.SoTienHanMuc:N0}đ)");
                    }
                    else
                    {
                        var nganSachThangMoi = new NganSach
                        {
                            MaNguoiDung = userId.Value,
                            MaDanhMuc = null,
                            SoTienHanMuc = SoTienHanMuc,
                            SoTienNganSachThang = SoTienHanMuc,
                            NgayBatDau = NgayBatDau,
                            NgayKetThuc = NgayKetThuc
                        };
                        _context.NganSach.Add(nganSachThangMoi);
                        _logger.LogInformation($"[User {userId}] Thiết lập ngân sách tháng mới: {SoTienHanMuc:N0}đ");
                    }
                }
                else 
                {
                    var nganSachDanhMucCu = await _context.NganSach
                        .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                               n.MaDanhMuc == MaDanhMuc && 
                               n.NgayBatDau.Date >= NgayBatDau.Date && 
                               n.NgayKetThuc.Date <= NgayKetThuc.Date);
                    
                    if (nganSachDanhMucCu != null)
                    {
                        nganSachDanhMucCu.SoTienNganSachThang = SoTienHanMuc;
                        nganSachDanhMucCu.SoTienHanMuc = SoTienHanMuc;
                        nganSachDanhMucCu.NgayBatDau = NgayBatDau;
                        nganSachDanhMucCu.NgayKetThuc = NgayKetThuc;
                        _logger.LogInformation($"[User {userId}] Cập nhật ngân sách danh mục {MaDanhMuc}: {SoTienHanMuc:N0}đ");
                    }
                    else
                    {
                        var nganSachDanhMucMoi = new NganSach
                        {
                            MaNguoiDung = userId.Value,
                            MaDanhMuc = MaDanhMuc,
                            SoTienNganSachThang = SoTienHanMuc,
                            SoTienHanMuc = SoTienHanMuc,
                            NgayBatDau = NgayBatDau,
                            NgayKetThuc = NgayKetThuc
                        };
                        _context.NganSach.Add(nganSachDanhMucMoi);
                        _logger.LogInformation($"[User {userId}] Tạo ngân sách danh mục {MaDanhMuc}: {SoTienHanMuc:N0}đ");
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi lưu ngân sách mới.");
            }

            return RedirectToAction("Dashboard"); 
        }
        [HttpPost] 
        public async Task<IActionResult> ThemGiaoDich(
            decimal SoTien, 
            int MaDanhMuc, 
            DateTime NgayGiaoDich, 
            string GhiChu,
            bool IsDinhKy,         
            string TanSuat,        
            DateTime? NgayKetThuc  
        )
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); 
            }

            try
            {
                var (isValid, validationMessage) = await _expenseTrackingService.CanAddTransactionAsync(
                    SoTien,
                    MaDanhMuc,
                    userId.Value,
                    NgayGiaoDich);

                if (!isValid)
                {
                    _logger.LogWarning($"[User {userId}] Giao dịch bị từ chối: {validationMessage}");
                    await _notificationService.TaoThongBaoAsync(
                        userId.Value,
                        "⛔ Giao dịch bị từ chối",
                        validationMessage,
                        "CanhBaoGiaoDich",
                        "🚫"
                    );
                    
                    TempData["Error"] = validationMessage;
                    return RedirectToAction("Dashboard");
                }
                var giaoDichMoi = new Backend.Models.GiaoDich
                {
                    SoTien = SoTien,
                    MaDanhMuc = MaDanhMuc,
                    NgayGiaoDich = NgayGiaoDich,
                    GhiChu = GhiChu,
                    MaNguoiDung = userId.Value,
                    IsDinhKy = IsDinhKy,
                    TanSuat = IsDinhKy ? TanSuat : null, 
                    NgayKetThuc = IsDinhKy ? NgayKetThuc : null
                };

                _context.GiaoDich.Add(giaoDichMoi);
                var danhMuc = await _context.DanhMuc.FindAsync(MaDanhMuc);
                var nguoiDung = await _context.NguoiDung.FindAsync(userId.Value);

                if (nguoiDung != null && danhMuc != null)
                {
                    bool laThu = danhMuc.LoaiDanhMuc == "Thu" || danhMuc.LoaiDanhMuc == "Thu Nhập";
                    
                    if (laThu)
                    {
                        nguoiDung.SoDuTaiKhoan += SoTien;
                        
                        var nganSachChung = await _context.NganSach
                            .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                                   n.MaDanhMuc == null && 
                                   n.NgayBatDau <= NgayGiaoDich && 
                                   n.NgayKetThuc >= NgayGiaoDich);
                        
                        if (nganSachChung != null)
                        {
                            nganSachChung.SoTienNganSachThang = (nganSachChung.SoTienNganSachThang ?? 0) + SoTien;
                            nganSachChung.SoTienHanMuc += SoTien;
                        }
                        
                        _logger.LogInformation($"[User {userId}] Thu tiền: {SoTien:N0}đ từ {danhMuc.TenDanhMuc}");
                    }
                    else
                    {
                        nguoiDung.SoDuTaiKhoan -= SoTien;
                        
                        var nganSachChung = await _context.NganSach
                            .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                                   n.MaDanhMuc == null && 
                                   n.NgayBatDau <= NgayGiaoDich && 
                                   n.NgayKetThuc >= NgayGiaoDich);
                        
                        if (nganSachChung != null)
                        {
                            nganSachChung.SoTienHanMuc -= SoTien;
                        }
                        var nganSachDanhMuc = await _context.NganSach
                            .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                                   n.MaDanhMuc == MaDanhMuc && 
                                   n.NgayBatDau <= NgayGiaoDich && 
                                   n.NgayKetThuc >= NgayGiaoDich);
                        
                        if (nganSachDanhMuc != null)
                        {
                            nganSachDanhMuc.SoTienHanMuc -= SoTien;
                        }
                        
                        _logger.LogInformation($"[User {userId}] Chi tiền: {SoTien:N0}đ cho {danhMuc.TenDanhMuc} | Trừ cả ngân sách chung & danh mục");
                    }
                }

                await _context.SaveChangesAsync();
                if (nguoiDung != null && danhMuc != null)
                {
                    decimal soDuHienTai = nguoiDung.SoDuTaiKhoan ?? 0;
                    bool laThu = danhMuc.LoaiDanhMuc == "Thu" || danhMuc.LoaiDanhMuc == "Thu Nhập";
                    await _notificationService.TaoThongBaoGiaoDichAsync(
                        userId.Value, SoTien, danhMuc.TenDanhMuc ?? "Không rõ",
                        laThu, soDuHienTai, GhiChu);

                    if (!laThu)
                    {
                        await _notificationService.KiemTraCanhBaoSapHetTienAsync(userId.Value, soDuHienTai);

                        var (monthBudget, _, _, _) = await _expenseTrackingService.GetMonthlyBudgetInfoAsync(userId.Value);
                        await _notificationService.KiemTraCanhBaoChiTieuLonAsync(
                            userId.Value, SoTien, danhMuc.TenDanhMuc ?? "Không rõ",
                            monthBudget > 0 ? monthBudget : null);
                    }
                }

                TempData["Success"] = $"✅ Thêm giao dịch thành công!";
                _logger.LogInformation($"[User {userId}] Giao dịch thêm: {SoTien:N0}đ danh mục {MaDanhMuc}");
                
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm giao dịch mới");
                TempData["Error"] = "Lỗi khi thêm giao dịch. Vui lòng thử lại.";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> Transactions(string searchGhiChu, string searchNgay)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user != null)
                {
                    ViewBag.UserName = user.HoTen;
                }

                var query = _context.GiaoDich
                    .Include(g => g.DanhMuc) 
                    .Where(g => g.MaNguoiDung == userId.Value);

                if (!string.IsNullOrEmpty(searchGhiChu))
                {
                    query = query.Where(g => g.GhiChu != null && g.GhiChu.ToLower().Contains(searchGhiChu.ToLower()));
                }

                if (!string.IsNullOrEmpty(searchNgay))
                {
                    if (DateTime.TryParse(searchNgay, out DateTime parsedDate))
                    {
                        query = query.Where(g => g.NgayGiaoDich.Date == parsedDate.Date);
                    }
                }

                var danhSachGiaoDich = await query
                    .OrderByDescending(g => g.NgayGiaoDich)
                    .ToListAsync();

                var tongThu = danhSachGiaoDich.Where(g => g.DanhMuc != null && g.DanhMuc.LoaiDanhMuc == "Thu").Sum(g => g.SoTien);
                var tongChi = danhSachGiaoDich.Where(g => g.DanhMuc != null && g.DanhMuc.LoaiDanhMuc == "Chi").Sum(g => g.SoTien);
                var soDu = tongThu - tongChi;

                ViewBag.TongThu = tongThu;
                ViewBag.TongChi = tongChi;
                ViewBag.SoDu = soDu;

                return View(danhSachGiaoDich);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải lịch sử giao dịch");
                ViewBag.TongThu = 0;
                ViewBag.TongChi = 0;
                ViewBag.SoDu = 0;
                return View(new List<Backend.Models.GiaoDich>());
            }
        }
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.UserName = user.HoTen;
                ViewBag.UserEmail = user.Email;
                ViewBag.AccountBalance = user.SoDuTaiKhoan;
                var todayExpense = await _expenseTrackingService.GetTodayExpenseAsync(userId.Value);
                ViewBag.TodayExpense = todayExpense;

                var (monthlyBudget, monthlySpent, monthlyRemaining, percentUsed) = 
                    await _expenseTrackingService.GetMonthlyBudgetInfoAsync(userId.Value);

                ViewBag.MonthBudget = monthlyBudget;
                ViewBag.MonthExpense = monthlySpent;
                ViewBag.MonthRemaining = monthlyRemaining;
                ViewBag.PercentUsed = percentUsed;

                ViewBag.BudgetStatus = percentUsed > 100 ? "danger" :
                                       percentUsed > 80 ? "warning" :
                                       "normal";

                var transactionCount = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value)
                    .CountAsync();
                ViewBag.TransactionCount = transactionCount;

                var currentMonth = DateTime.Now;
                var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var monthEnd = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month));

                var recentTransactions = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value)
                    .OrderByDescending(g => g.NgayGiaoDich)
                    .Take(10)
                    .Select(g => new
                    {
                        g.MaGiaoDich,
                        g.SoTien,
                        g.GhiChu,
                        g.NgayGiaoDich,
                        TenDanhMuc = g.DanhMuc != null ? g.DanhMuc.TenDanhMuc : "N/A",
                        BieuTuong = g.DanhMuc != null ? g.DanhMuc.BieuTuong : "",
                        LoaiDanhMuc = g.DanhMuc != null ? g.DanhMuc.LoaiDanhMuc : "Chi"
                    })
                    .ToListAsync();
                ViewBag.RecentTransactions = recentTransactions;

                var expenseByCategory = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && 
                           g.NgayGiaoDich >= monthStart && 
                           g.NgayGiaoDich <= monthEnd &&
                           g.DanhMuc != null &&
                           g.DanhMuc.LoaiDanhMuc == "Chi")
                    .GroupBy(g => new { g.DanhMuc.TenDanhMuc, g.DanhMuc.BieuTuong, g.MaDanhMuc })
                    .Select(g => new
                    {
                        Category = g.Key.TenDanhMuc,
                        Icon = g.Key.BieuTuong,
                        Total = g.Sum(x => x.SoTien),
                        Count = g.Count(),
                        MaDanhMuc = g.Key.MaDanhMuc
                    })
                    .OrderByDescending(x => x.Total)
                    .ToListAsync();
                ViewBag.ExpenseByCategory = expenseByCategory;

                var incomeByCategory = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && 
                           g.NgayGiaoDich >= monthStart && 
                           g.NgayGiaoDich <= monthEnd &&
                           g.DanhMuc != null &&
                           (g.DanhMuc.LoaiDanhMuc == "Thu" || g.DanhMuc.LoaiDanhMuc == "Thu Nhập"))
                    .GroupBy(g => new { g.DanhMuc.TenDanhMuc, g.DanhMuc.BieuTuong, g.MaDanhMuc })
                    .Select(g => new
                    {
                        Category = g.Key.TenDanhMuc,
                        Icon = g.Key.BieuTuong,
                        Total = g.Sum(x => x.SoTien),
                        Count = g.Count(),
                        MaDanhMuc = g.Key.MaDanhMuc
                    })
                    .OrderByDescending(x => x.Total)
                    .ToListAsync();
                ViewBag.IncomeByCategory = incomeByCategory;

                var danhSachDanhMuc = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .ToListAsync();

                ViewBag.DanhSachDanhMuc = danhSachDanhMuc;
                ViewBag.DanhMucList = danhSachDanhMuc;

                var budgetByCategory = await _context.NganSach
                    .Where(b => b.MaNguoiDung == userId.Value && 
                           b.NgayBatDau <= monthEnd && 
                           b.NgayKetThuc >= monthStart &&
                           b.MaDanhMuc != null)
                    .Include(b => b.DanhMuc)
                    .ToListAsync();

                foreach (var budget in budgetByCategory)
                {
                    if (!budget.SoTienNganSachThang.HasValue || budget.SoTienNganSachThang <= 0)
                    {
                        budget.SoTienNganSachThang = Math.Max(budget.SoTienHanMuc, 0);
                    }
                }

                var categoryBudgetInfo = new Dictionary<int, dynamic>();
                foreach (var budget in budgetByCategory)
                {
                    if (budget.MaDanhMuc.HasValue)
                    {
                        categoryBudgetInfo[budget.MaDanhMuc.Value] = new
                        {
                            SoTienHanMuc = budget.SoTienHanMuc,
                            NgayBatDau = budget.NgayBatDau,
                            NgayKetThuc = budget.NgayKetThuc,
                            MaNganSach = budget.MaNganSach,
                            ChiTieu = 0,
                            ConLai = budget.SoTienHanMuc
                        };
                    }
                }

                ViewBag.CategoryBudgetInfo = categoryBudgetInfo;
                var budgetByCategoryList = new List<dynamic>();
                foreach (var budget in budgetByCategory)
                {
                    if (budget.MaDanhMuc.HasValue && budget.DanhMuc != null)
                    {
                        decimal original = budget.SoTienNganSachThang ?? 0m;
                        if (original <= 0)
                            continue;
                        
                        decimal remaining = budget.SoTienHanMuc;
                        decimal spent = original - remaining;

                        budgetByCategoryList.Add(new
                        {
                            Category = budget.DanhMuc.TenDanhMuc,
                            Icon = budget.DanhMuc.BieuTuong,
                            BudgetLimit = original,
                            Spent = Math.Max(0, spent),
                            Remaining = remaining,
                            MaNganSach = budget.MaNganSach
                        });
                    }
                }

                ViewBag.BudgetByCategory = budgetByCategoryList;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu Dashboard");
                ViewBag.RecentTransactions = new List<object>();
                ViewBag.ExpenseByCategory = new List<object>();
                ViewBag.IncomeByCategory = new List<object>();
                ViewBag.DanhSachDanhMuc = new List<Backend.Models.DanhMuc>();
                ViewBag.DanhMucList = new List<Backend.Models.DanhMuc>();
                ViewBag.MonthBudget = 0;
                ViewBag.MonthExpense = 0;
                ViewBag.MonthRemaining = 0;
                ViewBag.PercentUsed = 0;
                return View(new Backend.Models.NguoiDung());
            }
        }
    }
}

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

        /// <summary>
        /// Thiết lập ngân sách tháng cho người dùng
        /// Lưu cả SoTienHanMuc (hạn mức) và SoTienNganSachThang (tổng tiền nhập vào)
        /// </summary>
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
                // ─────────────────────────────────────────────────────────────
                // Trường hợp: Thiết lập NGÂN SÁCH CHUNG (tháng)
                // ─────────────────────────────────────────────────────────────
                if (LoaiNganSach == "Thang")
                {
                    // Xóa ngân sách chung cũ trong khoảng thời gian đó
                    var nganSachCu = await _context.NganSach
                        .Where(n => n.MaNguoiDung == userId.Value && 
                               n.MaDanhMuc == null &&
                               n.NgayBatDau.Date >= NgayBatDau.Date && 
                               n.NgayKetThuc.Date <= NgayKetThuc.Date)
                        .ToListAsync();
                        
                    if (nganSachCu.Any())
                    {
                        _context.NganSach.RemoveRange(nganSachCu);
                    }

                    // Tạo ngân sách tháng mới
                    var nganSachThangMoi = new NganSach
                    {
                        MaNguoiDung = userId.Value,
                        MaDanhMuc = null,                    // NULL = Ngân sách chung
                        SoTienHanMuc = SoTienHanMuc,         // Hạn mức (20 triệu)
                        SoTienNganSachThang = SoTienHanMuc,  // Tổng ngân sách tháng nhập vào
                        NgayBatDau = NgayBatDau,
                        NgayKetThuc = NgayKetThuc
                    };
                    _context.NganSach.Add(nganSachThangMoi);
                    
                    _logger.LogInformation($"[User {userId}] Thiết lập ngân sách chung tháng: {SoTienHanMuc:N0}đ");
                }
                // ─────────────────────────────────────────────────────────────
                // Trường hợp: Thiết lập NGÂN SÁCH DANH MỤC
                // ─────────────────────────────────────────────────────────────
                else 
                {
                    // Kiểm tra xem danh mục này trong tháng này đã có ngân sách chưa
                    var nganSachDanhMucCu = await _context.NganSach
                        .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                               n.MaDanhMuc == MaDanhMuc && 
                               n.NgayBatDau.Date >= NgayBatDau.Date && 
                               n.NgayKetThuc.Date <= NgayKetThuc.Date);
                    
                    if (nganSachDanhMucCu != null)
                    {
                        // Nếu đã tồn tại → Cập nhật
                        nganSachDanhMucCu.SoTienHanMuc = SoTienHanMuc;
                        _logger.LogInformation($"[User {userId}] Cập nhật ngân sách danh mục {MaDanhMuc}: {SoTienHanMuc:N0}đ");
                    }
                    else
                    {
                        // Nếu chưa tồn tại → Thêm mới
                        var nganSachDanhMucMoi = new NganSach
                        {
                            MaNguoiDung = userId.Value,
                            MaDanhMuc = MaDanhMuc,
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

        /// <summary>
        /// Thêm giao dịch mới với VALIDATION ngân sách
        /// </summary>
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
                // ═══════════════════════════════════════════════════════════════
                // STEP 1: VALIDATE NGÂN SÁCH
                // ═══════════════════════════════════════════════════════════════
                var (isValid, validationMessage) = await _expenseTrackingService.CanAddTransactionAsync(
                    SoTien,
                    MaDanhMuc,
                    userId.Value,
                    NgayGiaoDich);

                if (!isValid)
                {
                    _logger.LogWarning($"[User {userId}] Giao dịch bị từ chối: {validationMessage}");
                    TempData["Error"] = validationMessage;
                    return RedirectToAction("Dashboard");
                }

                // ═══════════════════════════════════════════════════════════════
                // STEP 2: THÊM GIAO DỊCH
                // ═══════════════════════════════════════════════════════════════
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

                // ═══════════════════════════════════════════════════════════════
                // STEP 3: CẬP NHẬT NGÂN SÁCH VÀ SỐ DƯ TÀI KHOẢN
                // ═══════════════════════════════════════════════════════════════
                var danhMuc = await _context.DanhMuc.FindAsync(MaDanhMuc);
                var nguoiDung = await _context.NguoiDung.FindAsync(userId.Value);

                if (nguoiDung != null && danhMuc != null)
                {
                    if (danhMuc.LoaiDanhMuc == "Thu" || danhMuc.LoaiDanhMuc == "Thu Nhập")
                    {
                        nguoiDung.SoDuTaiKhoan += SoTien;
                    }
                    else if (danhMuc.LoaiDanhMuc == "Chi" || danhMuc.LoaiDanhMuc == "Chi Tiêu")
                    {
                        nguoiDung.SoDuTaiKhoan -= SoTien;
                        
                        // GHI CHÚ: SoTienNganSachThang là ngân sách GỐC (không trừ)
                        // Dashboard tính: Còn lại = SoTienNganSachThang - SUM(GiaoDich tháng)
                        // (Service GetMonthlyBudgetInfoAsync sẽ xử lý tính toán)
                        
                        // TRỪ NGÂN SÁCH DANH MỤC (SoTienHanMuc)
                        var nganSachDanhMuc = await _context.NganSach
                            .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && 
                                   n.MaDanhMuc == MaDanhMuc && 
                                   n.NgayBatDau <= NgayGiaoDich && 
                                   n.NgayKetThuc >= NgayGiaoDich);
                        
                        if (nganSachDanhMuc != null)
                        {
                            nganSachDanhMuc.SoTienHanMuc -= SoTien;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                
                // ═══════════════════════════════════════════════════════════════
                // STEP 4: TẠO THÔNG BÁO (biến động số dư, cảnh báo)
                // ═══════════════════════════════════════════════════════════════
                if (nguoiDung != null && danhMuc != null)
                {
                    decimal soDuHienTai = nguoiDung.SoDuTaiKhoan ?? 0;
                    bool laThu = danhMuc.LoaiDanhMuc == "Thu" || danhMuc.LoaiDanhMuc == "Thu Nhập";

                    // 4a. Thông báo biến động số dư (luôn tạo)
                    await _notificationService.TaoThongBaoGiaoDichAsync(
                        userId.Value, SoTien, danhMuc.TenDanhMuc ?? "Không rõ",
                        laThu, soDuHienTai, GhiChu);

                    if (!laThu)
                    {
                        // 4b. Cảnh báo số dư sắp hết (dưới 100.000đ)
                        await _notificationService.KiemTraCanhBaoSapHetTienAsync(userId.Value, soDuHienTai);

                        // 4c. Cảnh báo chi tiêu lớn
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

        /// <summary>
        /// Dashboard - Hiển thị tổng quan chi tiêu & ngân sách tháng
        /// Sử dụng ExpenseTrackingService để tính chính xác
        /// </summary>
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

                // Lấy thông tin chi tiêu HÔM NAY
                var todayExpense = await _expenseTrackingService.GetTodayExpenseAsync(userId.Value);
                ViewBag.TodayExpense = todayExpense;

                // Lấy thông tin NGÂN SÁCH & CHI TIÊU THÁNG dùng ExpenseTrackingService
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
                        BieuTuong = g.DanhMuc != null ? g.DanhMuc.BieuTuong : ""
                    })
                    .ToListAsync();
                ViewBag.RecentTransactions = recentTransactions;

                var expenseByCategory = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && 
                           g.NgayGiaoDich >= monthStart && 
                           g.NgayGiaoDich <= monthEnd &&
                           g.DanhMuc != null)
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

                var categoryBudgetInfo = new Dictionary<int, dynamic>();
                foreach (var budget in budgetByCategory)
                {
                    if (budget.MaDanhMuc.HasValue)
                    {
                        var categoryExpense = await _context.GiaoDich
                            .Where(g => g.MaNguoiDung == userId.Value && 
                                   g.MaDanhMuc == budget.MaDanhMuc &&
                                   g.NgayGiaoDich >= monthStart && 
                                   g.NgayGiaoDich <= monthEnd)
                            .SumAsync(g => g.SoTien);

                        categoryBudgetInfo[budget.MaDanhMuc.Value] = new
                        {
                            SoTienHanMuc = budget.SoTienHanMuc,
                            NgayBatDau = budget.NgayBatDau,
                            NgayKetThuc = budget.NgayKetThuc,
                            MaNganSach = budget.MaNganSach,
                            ChiTieu = categoryExpense,
                            ConLai = budget.SoTienHanMuc - categoryExpense
                        };
                    }
                }

                ViewBag.CategoryBudgetInfo = categoryBudgetInfo;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu Dashboard");
                ViewBag.RecentTransactions = new List<object>();
                ViewBag.ExpenseByCategory = new List<object>();
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

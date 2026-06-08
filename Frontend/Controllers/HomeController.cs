using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;

        public HomeController(AppDbContext context, ILogger<HomeController> logger, IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
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
        public async Task<IActionResult> ThemNganSach(string LoaiNganSach, int? MaDanhMuc, decimal SoTienHanMuc, DateTime NgayBatDau, DateTime NgayKetThuc)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // TRƯỜNG HỢP 1: THIẾT LẬP HẠN MỨC CHO TỔNG CHI TIÊU THÁNG
                if (LoaiNganSach == "Thang")
                {
                    // Xóa TOÀN BỘ ngân sách cũ của người dùng này trong khoảng thời gian đã chọn
                    var nganSachCu = await _context.NganSach
                        .Where(n => n.MaNguoiDung == userId.Value && n.NgayBatDau >= NgayBatDau && n.NgayKetThuc <= NgayKetThuc)
                        .ToListAsync();
                        
                    if (nganSachCu.Any())
                    {
                        _context.NganSach.RemoveRange(nganSachCu);
                    }

                    // Tạo 1 ngân sách duy nhất cho cả tháng (MaDanhMuc = null)
                    var nganSachThangMoi = new NganSach
                    {
                        MaNguoiDung = userId.Value,
                        MaDanhMuc = null, // null đại diện cho cả tháng
                        SoTienHanMuc = SoTienHanMuc,
                        NgayBatDau = NgayBatDau,
                        NgayKetThuc = NgayKetThuc
                    };
                    _context.NganSach.Add(nganSachThangMoi);
                }
                // TRƯỜNG HỢP 2: THIẾT LẬP HẠN MỨC THEO DANH MỤC
                else 
                {
                    // Xóa ngân sách "Tổng chi tiêu tháng" (MaDanhMuc == null) nếu có để tránh xung đột tính toán
                    var nganSachTongCu = await _context.NganSach
                        .Where(n => n.MaNguoiDung == userId.Value && n.MaDanhMuc == null && n.NgayBatDau >= NgayBatDau && n.NgayKetThuc <= NgayKetThuc)
                        .ToListAsync();
                    if (nganSachTongCu.Any())
                    {
                        _context.NganSach.RemoveRange(nganSachTongCu);
                    }

                    // Kiểm tra xem danh mục này trong tháng này đã được thiết lập ngân sách chưa
                    var nganSachDanhMucCu = await _context.NganSach
                        .FirstOrDefaultAsync(n => n.MaNguoiDung == userId.Value && n.MaDanhMuc == MaDanhMuc && n.NgayBatDau >= NgayBatDau && n.NgayKetThuc <= NgayKetThuc);
                    
                    if (nganSachDanhMucCu != null)
                    {
                        // Nếu đã tồn tại thì cập nhật đè số tiền hạn mức mới
                        nganSachDanhMucCu.SoTienHanMuc = SoTienHanMuc;
                    }
                    else
                    {
                        // Nếu chưa có thì thêm mới (Hệ thống cho phép tạo nhiều ngân sách cho nhiều danh mục khác nhau)
                        var nganSachDanhMucMoi = new NganSach
                        {
                            MaNguoiDung = userId.Value,
                            MaDanhMuc = MaDanhMuc,
                            SoTienHanMuc = SoTienHanMuc,
                            NgayBatDau = NgayBatDau,
                            NgayKetThuc = NgayKetThuc
                        };
                        _context.NganSach.Add(nganSachDanhMucMoi);
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
                    if (danhMuc.LoaiDanhMuc == "Thu" || danhMuc.LoaiDanhMuc == "Thu Nhập")
                    {
                        nguoiDung.SoDuTaiKhoan += SoTien;
                    }
                    else if (danhMuc.LoaiDanhMuc == "Chi" || danhMuc.LoaiDanhMuc == "Chi Tiêu")
                    {
                        nguoiDung.SoDuTaiKhoan -= SoTien; 
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm giao dịch mới");
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

                var today = DateTime.Now.Date;
                var todayExpense = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && g.NgayGiaoDich.Date == today)
                    .SumAsync(g => g.SoTien);
                ViewBag.TodayExpense = todayExpense;

                var currentMonth = DateTime.Now;
                var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var monthEnd = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month));

                var monthExpense = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && 
                           g.NgayGiaoDich >= monthStart && 
                           g.NgayGiaoDich <= monthEnd)
                    .SumAsync(g => g.SoTien);
                ViewBag.MonthExpense = monthExpense;

                var monthBudget = await _context.NganSach
                    .Where(b => b.MaNguoiDung == userId.Value && 
                           b.NgayBatDau <= monthEnd && 
                           b.NgayKetThuc >= monthStart)
                    .SumAsync(b => b.SoTienHanMuc);
                ViewBag.MonthBudget = monthBudget;

                var categoryCount = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .CountAsync();
                ViewBag.CategoryCount = categoryCount;

                var transactionCount = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value)
                    .CountAsync();
                ViewBag.TransactionCount = transactionCount;

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
                    .GroupBy(g => new { g.DanhMuc.TenDanhMuc, g.DanhMuc.BieuTuong })
                    .Select(g => new
                    {
                        Category = g.Key.TenDanhMuc,
                        Icon = g.Key.BieuTuong,
                        Total = g.Sum(x => x.SoTien),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Total)
                    .ToListAsync();
                ViewBag.ExpenseByCategory = expenseByCategory;

                var budgetByCategory = await _context.NganSach
                    .Where(b => b.MaNguoiDung == userId.Value && 
                           b.NgayBatDau <= monthEnd && 
                           b.NgayKetThuc >= monthStart &&
                           b.DanhMuc != null)
                    .GroupBy(b => new { b.DanhMuc.TenDanhMuc, b.DanhMuc.BieuTuong, b.MaDanhMuc })
                    .Select(b => new
                    {
                        Category = b.Key.TenDanhMuc,
                        Icon = b.Key.BieuTuong,
                        BudgetLimit = b.Sum(x => x.SoTienHanMuc),
                        Spent = _context.GiaoDich
                             .Where(g => g.MaNguoiDung == userId.Value && 
                                    g.MaDanhMuc == b.Key.MaDanhMuc &&
                                    g.NgayGiaoDich >= monthStart && 
                                    g.NgayGiaoDich <= monthEnd)
                             .Sum(g => g.SoTien)
                    })
                    .ToListAsync();
                ViewBag.BudgetByCategory = budgetByCategory;

                // FIX LỖI TÊN ĐỒNG BỘ: Sinh ra cả 2 tên ViewBag để cả 2 modal (thêm giao dịch & hạn mức) đều nhận được dữ liệu danh mục
                var danhSachDanhMuc = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .ToListAsync();

                // Lấy thông tin ngân sách cho từng danh mục
                var categoryBudgetInfo = new Dictionary<int, dynamic>();
                foreach (var category in danhSachDanhMuc)
                {
                    if (category.MaDanhMuc.HasValue)
                    {
                        var budget = await _context.NganSach
                            .Where(n => n.MaNguoiDung == userId.Value && 
                                   n.MaDanhMuc == category.MaDanhMuc &&
                                   n.NgayBatDau <= monthEnd && 
                                   n.NgayKetThuc >= monthStart)
                            .FirstOrDefaultAsync();
                        
                        if (budget != null)
                        {
                            categoryBudgetInfo[category.MaDanhMuc.Value] = new
                            {
                                SoTienHanMuc = budget.SoTienHanMuc,
                                NgayBatDau = budget.NgayBatDau,
                                NgayKetThuc = budget.NgayKetThuc,
                                MaNganSach = budget.MaNganSach
                            };
                        }
                        else
                        {
                            categoryBudgetInfo[category.MaDanhMuc.Value] = null;
                        }
                    }
                }

                ViewBag.DanhSachDanhMuc = danhSachDanhMuc;
                ViewBag.DanhMucList = danhSachDanhMuc; // <-- Thêm dòng này để giải quyết triệt để lỗi dropdown rỗng ngoài view ngân sách
                ViewBag.CategoryBudgetInfo = categoryBudgetInfo;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu Dashboard");
                ViewBag.RecentTransactions = new List<object>();
                ViewBag.ExpenseByCategory = new List<object>();
                ViewBag.BudgetByCategory = new List<object>();
                ViewBag.DanhSachDanhMuc = new List<Backend.Models.DanhMuc>();
                ViewBag.DanhMucList = new List<Backend.Models.DanhMuc>();
                return View(new Backend.Models.NguoiDung());
            }
        }
    }
}
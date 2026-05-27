using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

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

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ThemGiaoDich(decimal SoTien, int MaDanhMuc, DateTime NgayGiaoDich, string GhiChu)
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
                    MaNguoiDung = userId.Value
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
        public async Task<IActionResult> Transactions()
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    try
    {
        var danhSachGiaoDich = await _context.GiaoDich
            .Include(g => g.DanhMuc) 
            .Where(g => g.MaNguoiDung == userId.Value)
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

                // Chi tiêu hôm nay (Tối ưu sang SumAsync)
                var today = DateTime.Now.Date;
                var todayExpense = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && g.NgayGiaoDich.Date == today)
                    .SumAsync(g => g.SoTien);
                ViewBag.TodayExpense = todayExpense;

                // Chi tiêu tháng này
                var currentMonth = DateTime.Now;
                var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var monthEnd = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month));

                var monthExpense = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && 
                           g.NgayGiaoDich >= monthStart && 
                           g.NgayGiaoDich <= monthEnd)
                    .SumAsync(g => g.SoTien);
                ViewBag.MonthExpense = monthExpense;

                // Ngân sách tháng
                var monthBudget = await _context.NganSach
                    .Where(b => b.MaNguoiDung == userId.Value && 
                           b.NgayBatDau <= monthEnd && 
                           b.NgayKetThuc >= monthStart)
                    .SumAsync(b => b.SoTienHanMuc);
                ViewBag.MonthBudget = monthBudget;

                // Tổng danh mục
                var categoryCount = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .CountAsync();
                ViewBag.CategoryCount = categoryCount;

                // Tổng giao dịch
                var transactionCount = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value)
                    .CountAsync();
                ViewBag.TransactionCount = transactionCount;

                // Giao dịch gần đây (10 giao dịch mới nhất)
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

                // Chi tiêu theo danh mục (tháng này)
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

                // Ngân sách theo danh mục
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


                ViewBag.DanhSachDanhMuc = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .ToListAsync();

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu Dashboard");
                // Đảm bảo không bị crash giao diện nếu có lỗi xảy ra
                ViewBag.RecentTransactions = new List<object>();
                ViewBag.ExpenseByCategory = new List<object>();
                ViewBag.BudgetByCategory = new List<object>();
                ViewBag.DanhSachDanhMuc = new List<Backend.Models.DanhMuc>();
                return View(new Backend.Models.NguoiDung());
            }
        }
    }
}
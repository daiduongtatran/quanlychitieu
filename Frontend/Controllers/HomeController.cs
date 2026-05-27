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

                // Chi tiêu hôm nay
                var today = DateTime.Now.Date;
                var todayExpense = _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && g.NgayGiaoDich.Date == today)
                    .Sum(g => g.SoTien);
                ViewBag.TodayExpense = todayExpense;

                // Chi tiêu tháng này
                var currentMonth = DateTime.Now;
                var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var monthEnd = new DateTime(currentMonth.Year, currentMonth.Month, DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month));
                
                var monthExpense = _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value && 
                           g.NgayGiaoDich >= monthStart && 
                           g.NgayGiaoDich <= monthEnd)
                    .Sum(g => g.SoTien);
                ViewBag.MonthExpense = monthExpense;

                // Ngân sách tháng
                var monthBudget = _context.NganSach
                    .Where(b => b.MaNguoiDung == userId.Value && 
                           b.NgayBatDau <= monthEnd && 
                           b.NgayKetThuc >= monthStart)
                    .Sum(b => b.SoTienHanMuc);
                ViewBag.MonthBudget = monthBudget;

                // Tổng danh mục
                var categoryCount = _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .Count();
                ViewBag.CategoryCount = categoryCount;

                // Tổng giao dịch
                var transactionCount = _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value)
                    .Count();
                ViewBag.TransactionCount = transactionCount;

                // Giao dịch gần đây (10 giao dịch mới nhất)
                var recentTransactions = _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId.Value)
                    .OrderByDescending(g => g.NgayGiaoDich)
                    .Take(10)
                    .Select(g => new
                    {
                        g.MaGiaoDich,
                        g.SoTien,
                        g.GhiChu,
                        g.NgayGiaoDich,
                        CategoryName = g.DanhMuc != null ? g.DanhMuc.TenDanhMuc : "N/A",
                        Icon = g.DanhMuc != null ? g.DanhMuc.BieuTuong : ""
                    })
                    .ToList();
                ViewBag.RecentTransactions = recentTransactions;

                // Chi tiêu theo danh mục (tháng này)
                var expenseByCategory = _context.GiaoDich
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
                    .ToList();
                ViewBag.ExpenseByCategory = expenseByCategory;

                // Ngân sách theo danh mục
                var budgetByCategory = _context.NganSach
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
                    .ToList();
                ViewBag.BudgetByCategory = budgetByCategory;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard: {Message}", ex.Message);
                // Nếu có lỗi, vẫn load trang nhưng với dữ liệu mặc định
                return View();
            }
        }
    }
}
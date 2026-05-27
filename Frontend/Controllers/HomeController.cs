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
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return RedirectToAction("Login", "Account");
            }
        }
    }
}
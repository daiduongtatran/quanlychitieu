using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Frontend.Controllers
{
    public class BaoCaoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserService _userService;

        public BaoCaoController(AppDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user != null)
            {
                ViewBag.UserName = user.HoTen;
            }

            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            var startDate = new DateTime(selectedYear, selectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var giaoDichThang = await _context.GiaoDich
                .Include(g => g.DanhMuc)
                .Where(g => g.MaNguoiDung == userId.Value 
                         && g.NgayGiaoDich >= startDate 
                         && g.NgayGiaoDich <= endDate)
                .ToListAsync();

            decimal tongThu = giaoDichThang.Where(g => g.DanhMuc?.LoaiDanhMuc == "Thu").Sum(g => g.SoTien);
            decimal tongChi = giaoDichThang.Where(g => g.DanhMuc?.LoaiDanhMuc == "Chi").Sum(g => g.SoTien);
            
            ViewBag.TongThu = tongThu;
            ViewBag.TongChi = tongChi;
            ViewBag.SoDu = tongThu - tongChi;

            var chiTieuTheoDanhMuc = giaoDichThang
                .Where(g => g.DanhMuc?.LoaiDanhMuc == "Chi")
                .GroupBy(g => new { g.DanhMuc.TenDanhMuc, g.DanhMuc.BieuTuong })
                .Select(g => new {
                    TenDanhMuc = g.Key.TenDanhMuc,
                    BieuTuong = g.Key.BieuTuong,
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(x => x.TongTien)
                .ToList();

            ViewBag.ChiTieuList = chiTieuTheoDanhMuc;
            
            ViewBag.PieLabels = JsonSerializer.Serialize(chiTieuTheoDanhMuc.Select(x => x.TenDanhMuc));
            ViewBag.PieData = JsonSerializer.Serialize(chiTieuTheoDanhMuc.Select(x => x.TongTien));

            var xuHuongNgay = Enumerable.Range(1, DateTime.DaysInMonth(selectedYear, selectedMonth))
                .Select(day => {
                    var date = new DateTime(selectedYear, selectedMonth, day);
                    var gdNgay = giaoDichThang.Where(g => g.NgayGiaoDich.Date == date);
                    return new {
                        Ngay = day.ToString(),
                        Thu = gdNgay.Where(g => g.DanhMuc?.LoaiDanhMuc == "Thu").Sum(g => g.SoTien),
                        Chi = gdNgay.Where(g => g.DanhMuc?.LoaiDanhMuc == "Chi").Sum(g => g.SoTien)
                    };
                }).ToList();

            ViewBag.LineLabels = JsonSerializer.Serialize(xuHuongNgay.Select(x => x.Ngay));
            ViewBag.LineThuData = JsonSerializer.Serialize(xuHuongNgay.Select(x => x.Thu));
            ViewBag.LineChiData = JsonSerializer.Serialize(xuHuongNgay.Select(x => x.Chi));

            return View();
        }
    }
}
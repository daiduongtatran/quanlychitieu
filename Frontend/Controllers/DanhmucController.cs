using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Frontend.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserService _userService;

        public DanhMucController(AppDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
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

            var danhMucs = await _context.DanhMuc
                .Where(d => d.MaNguoiDung == userId.Value)
                .ToListAsync();

            if (!danhMucs.Any())
            {
                await CreateDefaultCategoriesAsync(userId.Value);
                danhMucs = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .ToListAsync();
            }

            return View(danhMucs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc danhMuc)
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

            danhMuc.MaNguoiDung = userId.Value;

            ModelState.Remove("NguoiDung");
            ModelState.Remove("GiaoDich");

            if (ModelState.IsValid)
            {
                _context.Add(danhMuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var danhMucs = await _context.DanhMuc
                .Where(d => d.MaNguoiDung == userId.Value)
                .ToListAsync();
            return View(nameof(Index), danhMucs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var danhMuc = await _context.DanhMuc
                .FirstOrDefaultAsync(d => d.MaDanhMuc == id && d.MaNguoiDung == userId.Value);

            if (danhMuc != null)
            {
                _context.DanhMuc.Remove(danhMuc);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CreateDefaultCategoriesAsync(int userId)
        {
            try
            {
                var defaultCategories = new List<DanhMuc>
                {

                    new DanhMuc { TenDanhMuc = "Ăn uống", LoaiDanhMuc = "Chi", BieuTuong = "☕", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Di chuyển", LoaiDanhMuc = "Chi", BieuTuong = "🚗", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Mua sắm", LoaiDanhMuc = "Chi", BieuTuong = "🛒", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Sức khỏe", LoaiDanhMuc = "Chi", BieuTuong = "❤️", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Giáo dục", LoaiDanhMuc = "Chi", BieuTuong = "📚", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Nhà cửa", LoaiDanhMuc = "Chi", BieuTuong = "🏠", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Giải trí", LoaiDanhMuc = "Chi", BieuTuong = "🎬", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Khác", LoaiDanhMuc = "Chi", BieuTuong = "❓", MaNguoiDung = userId },

                    new DanhMuc { TenDanhMuc = "Lương", LoaiDanhMuc = "Thu", BieuTuong = "💼", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Tiền thưởng", LoaiDanhMuc = "Thu", BieuTuong = "🎁", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Đầu tư", LoaiDanhMuc = "Thu", BieuTuong = "📈", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Thu nhập khác", LoaiDanhMuc = "Thu", BieuTuong = "❓", MaNguoiDung = userId },
                };

                _context.DanhMuc.AddRange(defaultCategories);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"Error creating default categories: {ex.Message}");
            }
        }
    }
}
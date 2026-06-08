using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Frontend.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly AppDbContext _context;

        public DanhMucController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách danh mục của User
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var danhMucs = await _context.DanhMuc
                .Where(d => d.MaNguoiDung == userId.Value)
                .ToListAsync();

            // Nếu người dùng chưa có danh mục nào, tạo danh mục mặc định
            if (!danhMucs.Any())
            {
                await CreateDefaultCategoriesAsync(userId.Value);
                danhMucs = await _context.DanhMuc
                    .Where(d => d.MaNguoiDung == userId.Value)
                    .ToListAsync();
            }

            return View(danhMucs);
        }

        // Xử lý thêm danh mục mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc danhMuc)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            danhMuc.MaNguoiDung = userId.Value;

            // Loại bỏ kiểm tra ràng buộc tự động của NguoiDung và GiaoDich khi gửi từ Form
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

        // Xử lý xóa danh mục
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

        // Tạo danh mục mặc định cho người dùng
        private async Task CreateDefaultCategoriesAsync(int userId)
        {
            try
            {
                var defaultCategories = new List<DanhMuc>
                {
                    // Danh mục Chi tiêu
                    new DanhMuc { TenDanhMuc = "Ăn uống", LoaiDanhMuc = "Chi", BieuTuong = "☕", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Di chuyển", LoaiDanhMuc = "Chi", BieuTuong = "🚗", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Mua sắm", LoaiDanhMuc = "Chi", BieuTuong = "🛒", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Sức khỏe", LoaiDanhMuc = "Chi", BieuTuong = "❤️", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Giáo dục", LoaiDanhMuc = "Chi", BieuTuong = "📚", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Nhà cửa", LoaiDanhMuc = "Chi", BieuTuong = "🏠", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Giải trí", LoaiDanhMuc = "Chi", BieuTuong = "🎬", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Khác", LoaiDanhMuc = "Chi", BieuTuong = "❓", MaNguoiDung = userId },
                    
                    // Danh mục Thu nhập
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
                // Log lỗi nếu cần, nhưng không ném exception
                System.Diagnostics.Debug.WriteLine($"Error creating default categories: {ex.Message}");
            }
        }
    }
}
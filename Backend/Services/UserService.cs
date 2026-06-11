using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

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

        public async Task<(bool Success, string Message, int? UserId)> RegisterUserAsync(string tenDangNhap, string email, string hoTen, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenDangNhap))
                    return (false, "Tên đăng nhập không được để trống", null);

                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email không được để trống", null);

                if (string.IsNullOrWhiteSpace(hoTen))
                    return (false, "Họ tên không được để trống", null);

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "Mật khẩu phải có ít nhất 6 ký tự", null);

                if (await EmailExistsAsync(email.Trim()))
                    return (false, "Email này đã được sử dụng", null);

                if (await UsernameExistsAsync(tenDangNhap.Trim()))
                    return (false, "Tên đăng nhập này đã được sử dụng", null);

                var newUser = new NguoiDung
                {
                    TenDangNhap = tenDangNhap.Trim(),
                    Email = email.Trim().ToLower(), 
                    HoTen = hoTen.Trim(),
                    MatKhauHash = HashPassword(password),
                    NgayTao = DateTime.Now
                };

                _context.NguoiDung.Add(newUser);
                await _context.SaveChangesAsync();

                await CreateDefaultCategoriesAsync(newUser.MaNguoiDung);

                _logger.LogInformation($"User registered successfully: {email}");
                return (true, "Đăng ký thành công!", newUser.MaNguoiDung);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return (false, "Lỗi khi đăng ký. Vui lòng thử lại.", null);
            }
        }

        public async Task<(bool Success, string Message, NguoiDung? User)> LoginUserAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email không được để trống", null);

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Mật khẩu không được để trống", null);


                Console.WriteLine($"\n[DIAGNOSTIC] === TIẾN TRÌNH ĐĂNG NHẬP ===");
                Console.WriteLine($"[DIAGNOSTIC] Email nhập vào form: '{email}'");
                Console.WriteLine($"[DIAGNOSTIC] Mật khẩu nhập vào form: '{password}'");

                var user = await _context.NguoiDung
                    .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == email.Trim().ToLower());

                if (user == null)
                {
                    Console.WriteLine("[DIAGNOSTIC] KẾT QUẢ: Không tìm thấy Email này trong Database!");
                    return (false, "Email hoặc mật khẩu không chính xác", null);
                }

                Console.WriteLine($"[DIAGNOSTIC] Kết quả DB: Tìm thấy tài khoản '{user.TenDangNhap}'");
                Console.WriteLine($"[DIAGNOSTIC] Chuỗi Hash trong DB: '{user.MatKhauHash}'");
                Console.WriteLine($"[DIAGNOSTIC] Chuỗi Hash từ form sinh ra: '{HashPassword(password)}'");

                if (!VerifyPassword(password, user.MatKhauHash))
                {
                    Console.WriteLine("[DIAGNOSTIC] KẾT QUẢ: Mật khẩu không khớp! Xác thực thất bại.");
                    return (false, "Email hoặc mật khẩu không chính xác", null);
                }

                Console.WriteLine("[DIAGNOSTIC] KẾT QUẢ: Xác thực thành công! Đang chuyển hướng sang Dashboard.");
                _logger.LogInformation($"User logged in successfully: {email}");
                return (true, "Đăng nhập thành công!", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return (false, "Lỗi khi đăng nhập. Vui lòng thử lại.", null);
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.NguoiDung.AnyAsync(u => u.Email.Trim().ToLower() == email.Trim().ToLower());
        }

        public async Task<bool> UsernameExistsAsync(string tenDangNhap)
        {
            return await _context.NguoiDung.AnyAsync(u => u.TenDangNhap.Trim().ToLower() == tenDangNhap.Trim().ToLower());
        }

        public async Task<NguoiDung?> GetUserByEmailAsync(string email)
        {
            return await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == email.Trim().ToLower());
        }

        public async Task<NguoiDung?> GetUserByIdAsync(int id)
        {
            return await _context.NguoiDung.FirstOrDefaultAsync(u => u.MaNguoiDung == id);
        }

        private async Task CreateDefaultCategoriesAsync(int userId)
        {
            try
            {
                var defaultCategories = new List<DanhMuc>
                {
                    // Danh mục Chi tiêu
                    new DanhMuc { TenDanhMuc = "Ăn uống", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-cup-hot", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Di chuyển", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-car-front", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Mua sắm", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-cart-check", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Sức khỏe", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-heart-pulse", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Giáo dục", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-book", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Nhà cửa", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-house", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Giải trí", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-film", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Khác", LoaiDanhMuc = "Chi", BieuTuong = "bi bi-question-circle", MaNguoiDung = userId },
                    
                    // Danh mục Thu nhập
                    new DanhMuc { TenDanhMuc = "Lương", LoaiDanhMuc = "Thu", BieuTuong = "bi bi-briefcase", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Tiền thưởng", LoaiDanhMuc = "Thu", BieuTuong = "bi bi-gift", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Đầu tư", LoaiDanhMuc = "Thu", BieuTuong = "bi bi-graph-up", MaNguoiDung = userId },
                    new DanhMuc { TenDanhMuc = "Thu nhập khác", LoaiDanhMuc = "Thu", BieuTuong = "bi bi-question-circle", MaNguoiDung = userId },
                };

                _context.DanhMuc.AddRange(defaultCategories);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Default categories created for user: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating default categories for user {userId}");
                
            }
        }
    }
}
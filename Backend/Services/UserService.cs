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
            return hashOfInput == hash;
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
// check xem email da ton tai ch
                if (await EmailExistsAsync(email))
                    return (false, "Email này đã được sử dụng", null);

                if (await UsernameExistsAsync(tenDangNhap))
                    return (false, "Tên đăng nhập này đã được sử dụng", null);

                var newUser = new NguoiDung
                {
                    TenDangNhap = tenDangNhap,
                    Email = email,
                    HoTen = hoTen,
                    MatKhauHash = HashPassword(password),
                    NgayTao = DateTime.Now
                };

                _context.NguoiDung.Add(newUser);
                await _context.SaveChangesAsync();

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

                var user = await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                    return (false, "Email hoặc mật khẩu không chính xác", null);

                if (!VerifyPassword(password, user.MatKhauHash))
                    return (false, "Email hoặc mật khẩu không chính xác", null);

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
            return await _context.NguoiDung.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> UsernameExistsAsync(string tenDangNhap)
        {
            return await _context.NguoiDung.AnyAsync(u => u.TenDangNhap == tenDangNhap);
        }

        public async Task<NguoiDung?> GetUserByEmailAsync(string email)
        {
            return await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<NguoiDung?> GetUserByIdAsync(int id)
        {
            return await _context.NguoiDung.FirstOrDefaultAsync(u => u.MaNguoiDung == id);
        }
    }
}

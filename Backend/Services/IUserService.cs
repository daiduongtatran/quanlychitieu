using Backend.Models;

namespace Backend.Services
{
    public interface IUserService
    {
        Task<(bool Success, string Message, int? UserId)> RegisterUserAsync(string tenDangNhap, string email, string hoTen, string password);
        Task<(bool Success, string Message, NguoiDung? User)> LoginUserAsync(string email, string password);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string tenDangNhap);
        Task<NguoiDung?> GetUserByEmailAsync(string email);
        Task<NguoiDung?> GetUserByIdAsync(int id);
    }
}

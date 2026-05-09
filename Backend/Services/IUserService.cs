using Backend.Models;

namespace Backend.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Register a new user
        /// </summary>
        Task<(bool Success, string Message, int? UserId)> RegisterUserAsync(string tenDangNhap, string email, string hoTen, string password);

        /// <summary>
        /// Login user
        /// </summary>
        Task<(bool Success, string Message, NguoiDung? User)> LoginUserAsync(string email, string password);

        /// <summary>
        /// Check if email exists
        /// </summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// Check if username exists
        /// </summary>
        Task<bool> UsernameExistsAsync(string tenDangNhap);

        /// <summary>
        /// Get user by email
        /// </summary>
        Task<NguoiDung?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Get user by id
        /// </summary>
        Task<NguoiDung?> GetUserByIdAsync(int id);
    }
}

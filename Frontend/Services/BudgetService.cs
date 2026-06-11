using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend.Services
{

    public interface IExpenseTrackingService
    {        Task<decimal> GetMonthlyTotalExpenseAsync(int userId, DateTime monthDate);

        Task<decimal> GetCategoryMonthlyExpenseAsync(int userId, int categoryId, DateTime monthDate);

        Task<NganSach?> GetCurrentMonthlyBudgetAsync(int userId);

        Task<NganSach?> GetCategoryBudgetAsync(int userId, int categoryId);
        Task<decimal> GetMonthlyBudgetRemainingAsync(int userId);

        Task<decimal> GetCategoryBudgetRemainingAsync(int userId, int categoryId);

        Task<(bool isValid, string message)> CanAddTransactionAsync(
            decimal amount,
            int? categoryId,
            int userId,
            DateTime transactionDate);

        Task<decimal> GetTodayExpenseAsync(int userId);

        Task<(decimal budget, decimal spent, decimal remaining, decimal percentUsed)> GetMonthlyBudgetInfoAsync(int userId);
    }

    public class ExpenseTrackingService : IExpenseTrackingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExpenseTrackingService> _logger;

        public ExpenseTrackingService(AppDbContext context, ILogger<ExpenseTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> GetMonthlyTotalExpenseAsync(int userId, DateTime monthDate)
        {
            try
            {
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var totalExpense = await _context.GiaoDich
                    .Include(g => g.DanhMuc)
                    .Where(g => g.MaNguoiDung == userId &&
                               g.NgayGiaoDich >= monthStart &&
                               g.NgayGiaoDich <= monthEnd &&
                               g.DanhMuc != null &&
                               (g.DanhMuc.LoaiDanhMuc == "Chi" || g.DanhMuc.LoaiDanhMuc == "Chi Tiêu"))
                    .SumAsync(g => g.SoTien);

                return totalExpense;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating monthly total expense: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetCategoryMonthlyExpenseAsync(int userId, int categoryId, DateTime monthDate)
        {
            try
            {
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var categoryExpense = await _context.GiaoDich
                    .Include(g => g.DanhMuc)
                    .Where(g => g.MaNguoiDung == userId &&
                               g.MaDanhMuc == categoryId &&
                               g.NgayGiaoDich >= monthStart &&
                               g.NgayGiaoDich <= monthEnd &&
                               g.DanhMuc != null &&
                               (g.DanhMuc.LoaiDanhMuc == "Chi" || g.DanhMuc.LoaiDanhMuc == "Chi Tiêu"))
                    .SumAsync(g => g.SoTien);

                return categoryExpense;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating category monthly expense: {ex.Message}");
                return 0;
            }
        }

        public async Task<NganSach?> GetCurrentMonthlyBudgetAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthlyBudget = await _context.NganSach
                    .FirstOrDefaultAsync(n =>
                        n.MaNguoiDung == userId &&
                        n.MaDanhMuc == null &&
                        n.NgayBatDau <= today &&
                        n.NgayKetThuc >= today);

                return monthlyBudget;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting current monthly budget: {ex.Message}");
                return null;
            }
        }

        public async Task<NganSach?> GetCategoryBudgetAsync(int userId, int categoryId)
        {
            try
            {
                var today = DateTime.Today;

                var categoryBudget = await _context.NganSach
                    .FirstOrDefaultAsync(n =>
                        n.MaNguoiDung == userId &&
                        n.MaDanhMuc == categoryId &&
                        n.NgayBatDau <= today &&
                        n.NgayKetThuc >= today);

                return categoryBudget;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting category budget: {ex.Message}");
                return null;
            }
        }

        public async Task<decimal> GetMonthlyBudgetRemainingAsync(int userId)
        {
            try
            {
                var monthlyBudget = await GetCurrentMonthlyBudgetAsync(userId);
                if (monthlyBudget == null)
                    return 0;

                var remaining = monthlyBudget.SoTienHanMuc;
                return Math.Max(0, remaining);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating monthly budget remaining: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetCategoryBudgetRemainingAsync(int userId, int categoryId)
        {
            try
            {
                var categoryBudget = await GetCategoryBudgetAsync(userId, categoryId);
                if (categoryBudget == null)
                    return 0;

                var remaining = categoryBudget.SoTienHanMuc;
                return Math.Max(0, remaining);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating category budget remaining: {ex.Message}");
                return 0;
            }
        }

        public async Task<(bool isValid, string message)> CanAddTransactionAsync(
            decimal amount,
            int? categoryId,
            int userId,
            DateTime transactionDate)
        {
            try
            {
                if (categoryId.HasValue)
                {
                    var category = await _context.DanhMuc.FindAsync(categoryId.Value);
                    if (category != null && (category.LoaiDanhMuc == "Thu" || category.LoaiDanhMuc == "Thu Nhập"))
                    {
                    
                        return (true, "OK");
                    }
                }

                var errors = new List<string>();

                var monthlyBudget = await GetCurrentMonthlyBudgetAsync(userId);
                if (monthlyBudget != null)
                {
                    var monthlyRemaining = monthlyBudget.SoTienHanMuc;

                    if (amount > monthlyRemaining)
                    {
                        errors.Add($"⚠️ Bạn không thể thêm giao dịch này vì vượt quá số tiền trong ngân sách! Ngân sách tháng còn lại: {monthlyRemaining:N0}đ");
                    }
                }

                if (errors.Any())
                {
                    return (false, string.Join(" | ", errors));
                }

                return (true, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating transaction: {ex.Message}");
                return (false, "Lỗi kiểm tra ngân sách");
            }
        }

        public async Task<decimal> GetTodayExpenseAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;

                var todayExpense = await _context.GiaoDich
                    .Where(g => g.MaNguoiDung == userId && g.NgayGiaoDich.Date == today)
                    .SumAsync(g => g.SoTien);

                return todayExpense;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating today expense: {ex.Message}");
                return 0;
            }
        }

        public async Task<(decimal budget, decimal spent, decimal remaining, decimal percentUsed)> GetMonthlyBudgetInfoAsync(int userId)
        {
            try
            {
                var monthlyBudget = await GetCurrentMonthlyBudgetAsync(userId);
                if (monthlyBudget == null || !monthlyBudget.SoTienNganSachThang.HasValue)
                    return (0, 0, 0, 0);

                var budgetOriginal = monthlyBudget.SoTienNganSachThang.Value;
                var remaining = monthlyBudget.SoTienHanMuc;
                var spent = budgetOriginal - remaining;
                
                var percentUsed = budgetOriginal > 0 ? Math.Round((spent / budgetOriginal) * 100, 2) : 0;

                return (budgetOriginal, spent, Math.Max(0, remaining), percentUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting monthly budget info: {ex.Message}");
                return (0, 0, 0, 0);
            }
        }
    }
}


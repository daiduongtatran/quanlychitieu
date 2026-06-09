using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend.Services
{
    /// <summary>
    /// Dịch vụ quản lý chi tiêu - Tracking ngân sách tháng & danh mục
    /// 
    /// UX/Logic thực tế như Momo, Wallet app:
    /// 1. User nhập: "Tháng này tôi có 20 triệu để tiêu" (SoTienNganSachThang)
    /// 2. User tiêu: "Hôm nay ăn uống 500k" (tạo giao dịch)
    /// 3. App tự động: "Ngân sách còn 19.5 triệu" (20tr - 0.5tr)
    /// 4. Nếu tiêu vượt: "Vượt ngân sách, còn x triệu" (từ chối hoặc cảnh báo)
    /// 
    /// Hai loại ngân sách:
    /// - Ngân sách CHUNG (MaDanhMuc=NULL): Giới hạn tổng chi tiêu tháng (SoTienNganSachThang)
    /// - Ngân sách DANH MỤC (MaDanhMuc=ID): Giới hạn chi tiêu từng loại (SoTienHanMuc)
    /// 
    /// Logic trừ tiền:
    /// - Chi tiêu tất cả danh mục → TRỪ từ ngân sách chung
    /// - Chi tiêu danh mục cụ thể → TRỪ từ ngân sách danh mục ĐÓ
    /// </summary>
    public interface IExpenseTrackingService
    {
        /// <summary>
        /// Lấy tổng chi tiêu tháng này (tất cả danh mục)
        /// Dùng để tính: Ngân sách chung còn lại = SoTienNganSachThang - TongChiTieuThang
        /// </summary>
        Task<decimal> GetMonthlyTotalExpenseAsync(int userId, DateTime monthDate);

        /// <summary>
        /// Lấy chi tiêu của 1 danh mục trong tháng
        /// Dùng để tính: Ngân sách danh mục còn lại = SoTienHanMuc - TongChiTieuDanhMuc
        /// </summary>
        Task<decimal> GetCategoryMonthlyExpenseAsync(int userId, int categoryId, DateTime monthDate);

        /// <summary>
        /// Lấy ngân sách chung của tháng hiện tại
        /// </summary>
        Task<NganSach?> GetCurrentMonthlyBudgetAsync(int userId);

        /// <summary>
        /// Lấy ngân sách danh mục cho tháng hiện tại
        /// </summary>
        Task<NganSach?> GetCategoryBudgetAsync(int userId, int categoryId);

        /// <summary>
        /// Tính ngân sách chung còn lại tháng này
        /// Công thức: SoTienNganSachThang - TongChiTieuThang
        /// </summary>
        Task<decimal> GetMonthlyBudgetRemainingAsync(int userId);

        /// <summary>
        /// Tính ngân sách danh mục còn lại
        /// Công thức: SoTienHanMuc - TongChiTieuDanhMuc
        /// </summary>
        Task<decimal> GetCategoryBudgetRemainingAsync(int userId, int categoryId);

        /// <summary>
        /// Kiểm tra xem giao dịch có vượt ngân sách không
        /// Kiểm tra cả ngân sách chung + danh mục
        /// 
        /// Returns: (isValid, errorMessage)
        /// - isValid=true: OK, được thêm giao dịch
        /// - isValid=false: Vượt ngân sách, không được thêm
        /// </summary>
        Task<(bool isValid, string message)> CanAddTransactionAsync(
            decimal amount,
            int? categoryId,
            int userId,
            DateTime transactionDate);

        /// <summary>
        /// Lấy chi tiêu hôm nay
        /// </summary>
        Task<decimal> GetTodayExpenseAsync(int userId);

        /// <summary>
        /// Lấy ngân sách info cho dashboard
        /// Trả về: (NgânSáchChung, TongChiTieuThang, ConLai, %)
        /// </summary>
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

        /// <summary>
        /// Lấy tổng chi tiêu trong tháng (tất cả danh mục)
        /// </summary>
        public async Task<decimal> GetMonthlyTotalExpenseAsync(int userId, DateTime monthDate)
        {
            try
            {
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Chỉ tính CHI (loại danh mục "Chi"), không tính THU
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

        /// <summary>
        /// Lấy chi tiêu của 1 danh mục trong tháng
        /// </summary>
        public async Task<decimal> GetCategoryMonthlyExpenseAsync(int userId, int categoryId, DateTime monthDate)
        {
            try
            {
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Chỉ tính CHI (loại danh mục "Chi"), không tính THU
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

        /// <summary>
        /// Lấy ngân sách chung tháng hiện tại
        /// </summary>
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
                        n.MaDanhMuc == null &&  // Ngân sách chung (không phải danh mục)
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

        /// <summary>
        /// Lấy ngân sách danh mục cho tháng hiện tại
        /// </summary>
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

        /// <summary>
        /// Tính ngân sách chung CÒN LẠI
        /// Công thức: SoTienHanMuc (giá trị còn lại - được trừ trực tiếp)
        /// </summary>
        public async Task<decimal> GetMonthlyBudgetRemainingAsync(int userId)
        {
            try
            {
                var monthlyBudget = await GetCurrentMonthlyBudgetAsync(userId);
                if (monthlyBudget == null)
                    return 0;

                // ✅ SoTienHanMuc = Giá trị còn lại (đã được trừ)
                var remaining = monthlyBudget.SoTienHanMuc;
                return Math.Max(0, remaining);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating monthly budget remaining: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Tính ngân sách danh mục CÒN LẠI
        /// Công thức: SoTienHanMuc (giá trị hiện tại sau khi trừ)
        /// </summary>
        public async Task<decimal> GetCategoryBudgetRemainingAsync(int userId, int categoryId)
        {
            try
            {
                var categoryBudget = await GetCategoryBudgetAsync(userId, categoryId);
                if (categoryBudget == null)
                    return 0;

                // ✅ Trừ trực tiếp → SoTienHanMuc chính là giá trị còn lại
                var remaining = categoryBudget.SoTienHanMuc;
                return Math.Max(0, remaining);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating category budget remaining: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem ĐƯỢC thêm giao dịch không
        /// 
        /// Logic:
        /// 1. Nếu có ngân sách chung → Kiểm tra không vượt quá
        /// 2. Nếu có ngân sách danh mục → Kiểm tra không vượt quá
        /// 3. Nếu vượt 1 trong 2 → Từ chối + thông báo
        /// </summary>
        public async Task<(bool isValid, string message)> CanAddTransactionAsync(
            decimal amount,
            int? categoryId,
            int userId,
            DateTime transactionDate)
        {
            try
            {
                // Kiểm tra xem danh mục có phải "Chi" không
                // Nếu là "Thu", không cần kiểm tra ngân sách
                if (categoryId.HasValue)
                {
                    var category = await _context.DanhMuc.FindAsync(categoryId.Value);
                    if (category != null && (category.LoaiDanhMuc == "Thu" || category.LoaiDanhMuc == "Thu Nhập"))
                    {
                        // "Thu" không cần kiểm tra ngân sách
                        return (true, "OK");
                    }
                }

                var errors = new List<string>();

                // ─────────────────────────────────────────────────────────────
                // Kiểm tra 1: Ngân sách CHUNG (tháng) - Chỉ cho "Chi"
                // ─────────────────────────────────────────────────────────────
                var monthlyBudget = await GetCurrentMonthlyBudgetAsync(userId);
                if (monthlyBudget != null)
                {
                    // ✅ SoTienHanMuc = Giá trị còn lại (đã được trừ)
                    var monthlyRemaining = monthlyBudget.SoTienHanMuc;

                    if (amount > monthlyRemaining)
                    {
                        errors.Add($"⚠️ Bạn không thể thêm giao dịch này vì vượt quá số tiền trong ngân sách! Ngân sách tháng còn lại: {monthlyRemaining:N0}đ");
                    }
                }

                // ─────────────────────────────────────────────────────────────
                // ✅ BỎ: Kiểm tra ngân sách danh mục
                // Cho phép ngân sách danh mục đi âm (hiển thị UI thôi)
                // ─────────────────────────────────────────────────────────────
                // Ngân sách danh mục không còn được validate, chỉ để tracking

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

        /// <summary>
        /// Lấy chi tiêu HÔM NAY
        /// </summary>
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

        /// <summary>
        /// Lấy thông tin ngân sách cho DASHBOARD
        /// Trả về: (NgânSáchGốc, TongChiTieuThang, ConLai, %)
        /// 
        /// Logic:
        /// - SoTienNganSachThang = Giá trị GỐC (không đổi)
        /// - SoTienHanMuc = Giá trị CÒN LẠI (được trừ trực tiếp)
        /// - Spent = Gốc - ConLai
        /// </summary>
        public async Task<(decimal budget, decimal spent, decimal remaining, decimal percentUsed)> GetMonthlyBudgetInfoAsync(int userId)
        {
            try
            {
                var monthlyBudget = await GetCurrentMonthlyBudgetAsync(userId);
                if (monthlyBudget == null || !monthlyBudget.SoTienNganSachThang.HasValue)
                    return (0, 0, 0, 0);

                var budgetOriginal = monthlyBudget.SoTienNganSachThang.Value; // Gốc
                var remaining = monthlyBudget.SoTienHanMuc; // Còn lại (đã trừ)
                var spent = budgetOriginal - remaining; // Chi tiêu = Gốc - Còn lại
                
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


using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Tạo thông báo biến động số dư (giống Momo/ngân hàng)
        /// </summary>
        Task TaoThongBaoGiaoDichAsync(int userId, decimal soTien, string tenDanhMuc, bool laThu, decimal soDuSauGiaoDich, string? ghiChu);

        /// <summary>
        /// Kiểm tra và tạo cảnh báo nếu số dư dưới 100.000đ
        /// </summary>
        Task KiemTraCanhBaoSapHetTienAsync(int userId, decimal soDuHienTai);

        /// <summary>
        /// Kiểm tra và tạo cảnh báo nếu giao dịch chi tiêu quá lớn
        /// Ngưỡng: > 5.000.000đ hoặc > 30% ngân sách tháng
        /// </summary>
        Task KiemTraCanhBaoChiTieuLonAsync(int userId, decimal soTien, string tenDanhMuc, decimal? nganSachThang);

        /// <summary>
        /// Lấy danh sách thông báo (mới nhất trước), giới hạn 50 cái
        /// </summary>
        Task<List<ThongBao>> LayDanhSachThongBaoAsync(int userId, int take = 50);

        /// <summary>
        /// Lấy số thông báo chưa đọc
        /// </summary>
        Task<int> LaySoThongBaoChuaDocAsync(int userId);

        /// <summary>
        /// Đánh dấu tất cả thông báo là đã đọc
        /// </summary>
        Task DanhDauTatCaDaDocAsync(int userId);

        /// <summary>
        /// Đánh dấu 1 thông báo là đã đọc
        /// </summary>
        Task DanhDauDaDocAsync(int userId, int maThongBao);

        /// <summary>
        /// Tạo thông báo chung
        /// </summary>
        Task TaoThongBaoAsync(int userId, string tieuDe, string noiDung, string loaiThongBao, string? bieuTuong);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        // ─── Ngưỡng cảnh báo ────────────────────────────────────────────────
        private const decimal NGUONG_SAP_HET_TIEN = 100_000m;          // 100.000đ
        private const decimal NGUONG_CHI_TIEU_LON_TUYET_DOI = 5_000_000m; // 5 triệu đồng
        private const decimal NGUONG_CHI_TIEU_LON_PHAN_TRAM = 0.30m;   // 30% ngân sách tháng

        public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task TaoThongBaoGiaoDichAsync(
            int userId, decimal soTien, string tenDanhMuc,
            bool laThu, decimal soDuSauGiaoDich, string? ghiChu)
        {
            try
            {
                string dau = laThu ? "+" : "-";
                string bieuTuong = laThu ? "💰" : "💸";

                string tieuDe = laThu
                    ? $"Số dư tài khoản tăng {soTien:N0}đ"
                    : $"Số dư tài khoản giảm {soTien:N0}đ";

                string noiDung = $"{dau}{soTien:N0}đ • {tenDanhMuc}";
                if (!string.IsNullOrWhiteSpace(ghiChu))
                    noiDung += $" • {ghiChu}";

                // ✅ Lấy ngân sách tháng còn lại từ SoTienHanMuc (giá trị CÒN LẠI - đã trừ)
                var thangHienTai = DateTime.Now;
                var nganSachThang = await _context.NganSach
                    .FirstOrDefaultAsync(ns => ns.MaNguoiDung == userId
                                           && ns.MaDanhMuc == null  // Ngân sách chung tháng
                                           && ns.NgayBatDau.Month == thangHienTai.Month
                                           && ns.NgayBatDau.Year == thangHienTai.Year);

                decimal soDuNganSachConLai = soDuSauGiaoDich;  // Giá trị mặc định
                if (nganSachThang != null && nganSachThang.SoTienHanMuc >= 0)
                {
                    // ✅ SoTienHanMuc = giá trị CÒN LẠI (đã được trừ)
                    soDuNganSachConLai = nganSachThang.SoTienHanMuc;
                }

                noiDung += $"\nSố dư khả dụng: {soDuNganSachConLai:N0}đ";

                await _LuuThongBaoAsync(userId, tieuDe, noiDung, "GiaoDich", bieuTuong);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo giao dịch");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cảnh báo sắp hết tiền
        // ─────────────────────────────────────────────────────────────────────
        public async Task KiemTraCanhBaoSapHetTienAsync(int userId, decimal soDuHienTai)
        {
            try
            {
                // ✅ Chỉ tạo cảnh báo nếu ngân sách tháng CÒN LẠI < 100k
                // Bỏ check "số dư hiện tại < 100k"
                var thangHienTai = DateTime.Now;
                var nganSachThang = await _context.NganSach
                    .FirstOrDefaultAsync(ns => ns.MaNguoiDung == userId
                                           && ns.MaDanhMuc == null  // Ngân sách chung tháng
                                           && ns.NgayBatDau.Month == thangHienTai.Month
                                           && ns.NgayBatDau.Year == thangHienTai.Year);

                // Nếu không có ngân sách tháng, không cảnh báo
                if (nganSachThang == null)
                    return;

                // ✅ Lấy ngân sách còn lại từ SoTienHanMuc (giá trị CÒN LẠI)
                decimal soDuNganSachConLai = nganSachThang.SoTienHanMuc;

                // Chỉ tạo cảnh báo nếu ngân sách tháng còn < 100k
                if (soDuNganSachConLai >= NGUONG_SAP_HET_TIEN)
                    return;

                // Tránh spam: không tạo nếu trong 30 phút vừa rồi đã có cảnh báo tương tự
                var thoiGianGanNhat = DateTime.Now.AddMinutes(-30);
                bool daCoCanHBao = await _context.ThongBao
                    .AnyAsync(t => t.MaNguoiDung == userId
                                && t.LoaiThongBao == "SapHetNganSach"
                                && t.NgayTao >= thoiGianGanNhat);
                if (daCoCanHBao) return;

                // ✅ Nội dung cảnh báo: "Số dư tài khoản của bạn còn (...) hãy chú ý ngân sách chi tiêu"
                string tieuDe = "⚠️ Cảnh báo ngân sách!";
                string noiDung = $"Số dư tài khoản của bạn còn ({soDuNganSachConLai:N0}đ) hãy chú ý ngân sách chi tiêu";

                await _LuuThongBaoAsync(userId, tieuDe, noiDung, "SapHetNganSach", "🔴");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra cảnh báo sắp hết tiền");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cảnh báo chi tiêu lớn
        // ─────────────────────────────────────────────────────────────────────
        public async Task KiemTraCanhBaoChiTieuLonAsync(
            int userId, decimal soTien, string tenDanhMuc, decimal? nganSachThang)
        {
            try
            {
                bool vuotNguongTuyetDoi = soTien >= NGUONG_CHI_TIEU_LON_TUYET_DOI;
                bool vuotNguongPhanTram = nganSachThang.HasValue
                    && nganSachThang.Value > 0
                    && (soTien / nganSachThang.Value) >= NGUONG_CHI_TIEU_LON_PHAN_TRAM;

                if (!vuotNguongTuyetDoi && !vuotNguongPhanTram) return;

                string tieuDe = $"⚠️ Chi tiêu lớn: {soTien:N0}đ";
                string noiDung;

                if (vuotNguongTuyetDoi && vuotNguongPhanTram)
                {
                    decimal pct = Math.Round((soTien / nganSachThang!.Value) * 100, 0);
                    noiDung = $"Bạn vừa chi {soTien:N0}đ cho \"{tenDanhMuc}\", " +
                              $"chiếm {pct}% ngân sách tháng. Hãy kiểm tra lại chi tiêu của bạn.";
                }
                else if (vuotNguongPhanTram)
                {
                    decimal pct = Math.Round((soTien / nganSachThang!.Value) * 100, 0);
                    noiDung = $"Bạn vừa chi {soTien:N0}đ cho \"{tenDanhMuc}\", " +
                              $"chiếm đến {pct}% ngân sách tháng.";
                }
                else
                {
                    noiDung = $"Bạn vừa thực hiện 1 khoản chi lớn {soTien:N0}đ cho \"{tenDanhMuc}\". " +
                              $"Đây là khoản chi vượt 5 triệu đồng.";
                }

                await _LuuThongBaoAsync(userId, tieuDe, noiDung, "ChiTieuLon", "⚠️");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra cảnh báo chi tiêu lớn");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Truy vấn
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<ThongBao>> LayDanhSachThongBaoAsync(int userId, int take = 50)
        {
            return await _context.ThongBao
                .Where(t => t.MaNguoiDung == userId)
                .OrderByDescending(t => t.NgayTao)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> LaySoThongBaoChuaDocAsync(int userId)
        {
            return await _context.ThongBao
                .CountAsync(t => t.MaNguoiDung == userId && !t.DaDoc);
        }

        public async Task DanhDauTatCaDaDocAsync(int userId)
        {
            var chuaDoc = await _context.ThongBao
                .Where(t => t.MaNguoiDung == userId && !t.DaDoc)
                .ToListAsync();

            foreach (var tb in chuaDoc)
                tb.DaDoc = true;

            await _context.SaveChangesAsync();
        }

        public async Task DanhDauDaDocAsync(int userId, int maThongBao)
        {
            var tb = await _context.ThongBao
                .FirstOrDefaultAsync(t => t.MaThongBao == maThongBao && t.MaNguoiDung == userId);
            if (tb != null)
            {
                tb.DaDoc = true;
                await _context.SaveChangesAsync();
            }
        }

        // ✅ Tạo thông báo chung (generic)
        public async Task TaoThongBaoAsync(
            int userId, string tieuDe, string noiDung, string loaiThongBao, string? bieuTuong)
        {
            try
            {
                await _LuuThongBaoAsync(userId, tieuDe, noiDung, loaiThongBao, bieuTuong);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal helper
        // ─────────────────────────────────────────────────────────────────────
        private async Task _LuuThongBaoAsync(
            int userId, string tieuDe, string noiDung, string loai, string? bieuTuong)
        {
            var thongBao = new ThongBao
            {
                MaNguoiDung = userId,
                TieuDe      = tieuDe,
                NoiDung     = noiDung,
                LoaiThongBao = loai,
                BieuTuong   = bieuTuong,
                DaDoc       = false,
                NgayTao     = DateTime.Now
            };
            _context.ThongBao.Add(thongBao);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[Thông báo] User={userId} [{loai}] {tieuDe}");
        }
    }
}

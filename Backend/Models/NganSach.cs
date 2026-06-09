using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
    {
        public class NganSach
        {
            [Key]
            public int MaNganSach { get; set; }

            [Required]
            [Column(TypeName = "decimal(18,2)")]
            public decimal SoTienHanMuc { get; set; }
            
            // ═══════════════════════════════════════════════════════════════
            // SoTienNganSachThang: Tổng ngân sách tháng nhập vào
            // ═══════════════════════════════════════════════════════════════
            // Chỉ dùng khi MaDanhMuc = NULL (ngân sách chung/tháng)
            // 
            // Ví dụ:
            //   - Nhập tháng: 20,000,000 VNĐ
            //   - Tiêu ăn uống: 1,000,000 VNĐ (TRỪ từ SoTienNganSachThang)
            //   - Tiêu xăng: 2,000,000 VNĐ (TRỪ từ SoTienNganSachThang)
            //   - Còn lại: 17,000,000 VNĐ
            // 
            // Khi MaDanhMuc ≠ NULL: Cột này được IGNORE, dùng SoTienHanMuc thay
            [Column(TypeName = "decimal(18,2)")]
            public decimal? SoTienNganSachThang { get; set; }
            
            public DateTime NgayBatDau { get; set; }
            public DateTime NgayKetThuc { get; set; }

            // MaDanhMuc = NULL → Ngân sách chung (tháng)
            // MaDanhMuc = ID → Ngân sách danh mục (loại chi tiêu cụ thể)
            public int? MaDanhMuc { get; set; }
            [ForeignKey("MaDanhMuc")]
            public DanhMuc? DanhMuc { get; set; }

            public int MaNguoiDung { get; set; }
            [ForeignKey("MaNguoiDung")]
            public NguoiDung? NguoiDung { get; set; }
        }
    }
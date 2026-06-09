using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class ThongBao
    {
        [Key]
        public int MaThongBao { get; set; }

        [Required]
        [StringLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        [Required]
        public string NoiDung { get; set; } = string.Empty;

        /// <summary>
        /// Loại thông báo:
        ///   'GiaoDich'   - Biến động số dư (thu/chi)
        ///   'SapHetTien' - Cảnh báo sắp hết tiền (dưới 100.000đ)
        ///   'ChiTieuLon' - Cảnh báo chi tiêu lớn trong 1 lần
        ///   'NganSach'   - Cảnh báo vượt/gần vượt ngân sách
        /// </summary>
        [Required]
        [StringLength(50)]
        public string LoaiThongBao { get; set; } = string.Empty;

        /// <summary>
        /// Icon hiển thị kèm thông báo (emoji)
        /// </summary>
        [StringLength(10)]
        public string? BieuTuong { get; set; }

        public bool DaDoc { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Required]
        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiDung { get; set; }
    }
}

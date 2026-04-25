using System.ComponentModel.DataAnnotations; // Quan trọng: Phải có dòng này!

namespace Backend.Models
{
    public class DanhMuc
    {
        [Key] // Dòng này là bắt buộc để EF nhận MaDanhMuc là khóa chính
        public int MaDanhMuc { get; set; }

        [Required, StringLength(100)]
        public string TenDanhMuc { get; set; }

        [Required, StringLength(20)]
        public string LoaiDanhMuc { get; set; }

        public string BieuTuong { get; set; }

        public int? MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }
    }
}
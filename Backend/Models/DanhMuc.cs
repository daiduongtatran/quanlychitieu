using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class DanhMuc
    {
        [Key]
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
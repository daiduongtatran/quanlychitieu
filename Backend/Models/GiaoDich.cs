using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class GiaoDich
    {
        [Key] // Thêm dòng này để EF nhận diện khóa chính
        public int MaGiaoDich { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        public string GhiChu { get; set; }

        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        [Required]
        public int MaDanhMuc { get; set; }
        public DanhMuc DanhMuc { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }
    }
}
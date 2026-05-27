using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required]
        public string MatKhauHash { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string HoTen { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SoDuTaiKhoan { get; set; } = 0;

        public ICollection<GiaoDich> GiaoDichs { get; set; }
    }
}
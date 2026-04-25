using System.ComponentModel.DataAnnotations;

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
        public string Email { get; set; } // Đã bổ sung trường Email

        [StringLength(100)]
        public string HoTen { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Quan hệ 1-n: Một người dùng có nhiều giao dịch
        public ICollection<GiaoDich> GiaoDichs { get; set; }
    }
}
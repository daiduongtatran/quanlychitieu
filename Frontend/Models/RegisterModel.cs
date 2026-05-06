using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Họ tên không được bỏ trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu không được bỏ trống")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản sử dụng")]
        [Display(Name = "Tôi đã đọc và đồng ý với Điều khoản Dịch vụ")]
        public bool AgreeToTerms { get; set; }
    }
}

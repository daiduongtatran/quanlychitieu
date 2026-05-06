using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email không được bỏ trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string? Password { get; set; }

        [Display(Name = "Nhớ tôi")]
        public bool RememberMe { get; set; }
    }
}

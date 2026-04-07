using System.ComponentModel.DataAnnotations;

namespace SV22T1020555.Shop.Models
{
    /// <summary>Form đăng ký tài khoản khách (email + mật khẩu).</summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email đăng nhập")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Nhập lại mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = "";
    }
}

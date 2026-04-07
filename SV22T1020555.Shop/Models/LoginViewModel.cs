using System.ComponentModel.DataAnnotations;

namespace SV22T1020555.Shop.Models
{
    /// <summary>Form đăng nhập khách.</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Nhập email")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}

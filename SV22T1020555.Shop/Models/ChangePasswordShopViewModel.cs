using System.ComponentModel.DataAnnotations;

namespace SV22T1020555.Shop.Models
{
    /// <summary>Form đổi mật khẩu trên shop (cũ / mới / xác nhận).</summary>
    public class ChangePasswordShopViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string OldPassword { get; set; } = "";

        [Required]
        [MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required]
        [Compare(nameof(NewPassword))]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = "";
    }
}

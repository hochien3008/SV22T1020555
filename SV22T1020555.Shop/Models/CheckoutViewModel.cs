using System.ComponentModel.DataAnnotations;

namespace SV22T1020555.Shop.Models
{
    /// <summary>Thông tin giao hàng khi đặt hàng (tỉnh, địa chỉ, SĐT nhận).</summary>
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Chọn tỉnh/thành")]
        [Display(Name = "Tỉnh/Thành giao hàng")]
        public string DeliveryProvince { get; set; } = "";

        [Required(ErrorMessage = "Nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string DeliveryAddress { get; set; } = "";

        [Required(ErrorMessage = "Nhập số điện thoại nhận hàng")]
        [StringLength(30, MinimumLength = 8, ErrorMessage = "Số điện thoại từ 8–30 ký tự")]
        [Display(Name = "Số điện thoại nhận hàng")]
        [RegularExpression(@"^[-+() \d]{8,30}$", ErrorMessage = "Chỉ dùng chữ số và ký tự + - ( ) khoảng trắng")]
        public string DeliveryPhone { get; set; } = "";
    }
}

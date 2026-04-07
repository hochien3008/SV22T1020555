using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Sales;
using SV22T1020555.Shop;
using SV22T1020555.Shop.Models;

namespace SV22T1020555.Shop.Controllers
{
    /// <summary>
    /// Thanh toán đặt hàng: kiểm tra hồ sơ khách, hiển thị form giao hàng và tạo đơn.
    /// </summary>
    [Authorize(Roles = ShopRoles.Customer)]
    public class CheckoutController : Controller
    {
        /// <summary>
        /// Hiển thị bước thanh toán với giỏ thường hoặc giỏ Mua ngay, tiền điền địa chỉ giao hàng từ hồ sơ khách.
        /// </summary>
        /// <param name="source">"buyNow" để thanh toán giỏ Mua ngay; bỏ trống để dùng giỏ thường.</param>
        /// <returns>
        /// View form thanh toán với thông tin giao hàng;
        /// chuyển về giỏ hàng nếu giỏ rỗng hoặc hồ sơ khách chưa đầy đủ.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Index(string? source = null)
        {
            var isBuyNow = string.Equals(source, "buyNow", StringComparison.OrdinalIgnoreCase);
            var cart = isBuyNow ? ShopCartHelper.GetBuyNowCart() : ShopCartHelper.GetCartForCheckout();
            if (cart.Count == 0)
            {
                TempData["Error"] = isBuyNow ? "Không có sản phẩm Mua ngay để thanh toán." : "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            var customerId = User.GetCustomerId();
            if (customerId == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Checkout") });

            var customer = await PartnerDataService.GetCustomerAsync(customerId.Value);
            if (customer == null || !CustomerProfileHelper.IsCompleteForCheckout(customer))
            {
                TempData["ProfileRequired"] =
                    "Vui lòng bổ sung họ tên đầy đủ, số điện thoại, tỉnh/thành và địa chỉ trong Hồ sơ trước khi đặt hàng.";
                return RedirectToAction("Profile", "Account", new { returnUrl = Url.Action("Index", "Checkout") });
            }

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            ViewBag.CheckoutCart = cart;
            ViewBag.CheckoutSource = isBuyNow ? "buyNow" : "";
            var vm = new CheckoutViewModel
            {
                DeliveryProvince = customer.Province?.Trim() ?? "",
                DeliveryAddress = customer.Address?.Trim() ?? "",
                DeliveryPhone = customer.Phone?.Trim() ?? ""
            };
            return View(vm);
        }

        /// <summary>
        /// Ghi nhận đơn hàng từ giỏ và xóa giỏ tương ứng sau khi tạo đơn thành công.
        /// </summary>
        /// <param name="model">Dữ liệu thanh toán (tỉnh/thành, địa chỉ, sô điện thoại nhận hàng).</param>
        /// <param name="source">"buyNow" nếu thanh toán giỏ Mua ngay; bỏ trống nếu dùng giỏ thường.</param>
        /// <returns>
        /// Chuyển sang trang chi tiết đơn vừa tạo nếu thành công;
        /// hiển thị lại form kèm lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model, string? source = null)
        {
            var isBuyNow = string.Equals(source, "buyNow", StringComparison.OrdinalIgnoreCase);
            var cart = isBuyNow ? ShopCartHelper.GetBuyNowCart() : ShopCartHelper.GetCartForCheckout();
            if (cart.Count == 0)
            {
                TempData["Error"] = isBuyNow ? "Không có sản phẩm Mua ngay để thanh toán." : "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                ViewBag.CheckoutCart = cart;
                ViewBag.CheckoutSource = isBuyNow ? "buyNow" : "";
                return View(model);
            }

            var customerId = User.GetCustomerId();
            if (customerId == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Checkout") });

            var customer = await PartnerDataService.GetCustomerAsync(customerId.Value);
            if (!CustomerProfileHelper.IsCompleteForCheckout(customer))
            {
                TempData["ProfileRequired"] =
                    "Vui lòng bổ sung họ tên đầy đủ, số điện thoại, tỉnh/thành và địa chỉ trong Hồ sơ trước khi đặt hàng.";
                return RedirectToAction("Profile", "Account", new { returnUrl = Url.Action("Index", "Checkout") });
            }

            var lines = cart.Select(x => new OrderDetail
            {
                ProductID = x.ProductID,
                Quantity = x.Quantity,
                SalePrice = x.SalePrice
            }).ToList();

            var orderId = await SalesDataService.CreateOrderWithDetailsAsync(
                customerId.Value,
                model.DeliveryProvince.Trim(),
                model.DeliveryAddress.Trim(),
                lines,
                model.DeliveryPhone.Trim());

            if (orderId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Không tạo được đơn hàng. Kiểm tra sản phẩm còn bán.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(model);
            }

            ShopCartHelper.ClearCartForCheckout();
            TempData["Message"] = "Đặt hàng thành công.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Sales;
using SV22T1020555.Shop;

namespace SV22T1020555.Shop.Controllers
{
    /// <summary>
    /// Giỏ hàng session: xem, thêm, cập nhật số lượng, xóa dòng hoặc xóa cả giỏ; hỗ trợ Mua ngay (giỏ tách).
    /// </summary>
    public class CartController : Controller
    {
        /// <summary>
        /// Hiển thị trang giỏ hàng thường của phiên làm việc hiện tại.
        /// </summary>
        /// <returns>
        /// View giỏ hàng kèm danh sách sản phẩm đã thêm.
        /// </returns>
        public IActionResult Index()
        {
            return View(ShopCartHelper.GetCart());
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hoặc chuyển sang thanh toán Mua ngay (một dòng, không trộn giỏ thường).
        /// </summary>
        /// <param name="productId">Mã sản phẩm.</param>
        /// <param name="quantity">Số lượng (mặc định 1).</param>
        /// <param name="buyNow">True: lưu vào giỏ Mua ngay và redirect Checkout.</param>
        /// <param name="returnUrl">Sau khi thêm (không Mua ngay), quay lại URL nội bộ nếu có.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1, bool buyNow = false, string? returnUrl = null)
        {
            if (productId <= 0 || quantity <= 0)
            {
                TempData["Error"] = "Sản phẩm hoặc số lượng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var p = await CatalogDataService.GetProductAsync(productId);
            if (p == null || !p.IsSelling)
            {
                TempData["Error"] = "Mặt hàng không còn bán.";
                return RedirectToAction("Index", "Products");
            }

            if (buyNow)
            {
                ShopCartHelper.SetBuyNowCart(new OrderDetailViewInfo
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Unit = p.Unit,
                    Photo = p.Photo ?? "",
                    Quantity = quantity,
                    SalePrice = p.Price
                });
                return RedirectToAction("Index", "Checkout", new { source = "buyNow" });
            }

            ShopCartHelper.Add(new OrderDetailViewInfo
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Photo = p.Photo ?? "",
                Quantity = quantity,
                SalePrice = p.Price
            });

            TempData["Message"] = $"Đã thêm «{p.ProductName}» vào giỏ.";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Details", "Products", new { id = productId });
        }

        /// <summary>
        /// Cập nhật số lượng của một sản phẩm trong giỏ hàng thường.
        /// </summary>
        /// <param name="productId">Mã sản phẩm cần cập nhật.</param>
        /// <param name="quantity">Số lượng mới (phải lớn hơn 0).</param>
        /// <returns>
        /// Chuyển về trang giỏ hàng sau khi cập nhật hoặc nếu dữ liệu không hợp lệ.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            var p = await CatalogDataService.GetProductAsync(productId);
            if (p == null || !p.IsSelling || quantity <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Cập nhật không hợp lệ." });
                
                TempData["Error"] = "Cập nhật không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            ShopCartHelper.Update(productId, quantity, p.Price);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cart = ShopCartHelper.GetCart();
                var line = cart.FirstOrDefault(x => x.ProductID == productId);
                return Json(new
                {
                    success = true,
                    lineTotal = line != null ? (line.Quantity * line.SalePrice).ToString("N0") + " đ" : "0 đ",
                    cartTotal = cart.Sum(x => x.Quantity * x.SalePrice).ToString("N0") + " đ",
                    cartCount = cart.Sum(x => x.Quantity)
                });
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa một sản phẩm khỏi giỏ hàng thường.
        /// </summary>
        /// <param name="productId">Mã sản phẩm cần xóa khỏi giỏ.</param>
        /// <returns>
        /// Chuyển về trang giỏ hàng sau khi xóa.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            ShopCartHelper.Remove(productId);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cart = ShopCartHelper.GetCart();
                return Json(new
                {
                    success = true,
                    cartTotal = cart.Sum(x => x.Quantity * x.SalePrice).ToString("N0") + " đ",
                    cartCount = cart.Sum(x => x.Quantity),
                    isEmpty = cart.Count == 0
                });
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa toàn bộ sản phẩm trong giỏ hàng thường.
        /// </summary>
        /// <returns>
        /// Chuyển về trang giỏ hàng sau khi làm rỗng giỏ.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            ShopCartHelper.Clear();
            return RedirectToAction(nameof(Index));
        }
    }
}

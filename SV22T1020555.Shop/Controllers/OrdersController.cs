using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.DataDictionary;
using SV22T1020555.Models.Sales;
using SV22T1020555.Shop;
using System.Text.RegularExpressions;

namespace SV22T1020555.Shop.Controllers
{
    /// <summary>
    /// Đơn hàng của khách: danh sách, chi tiết, cập nhật giao hàng (đơn mới/chấp nhận), hủy đơn.
    /// </summary>
    [Authorize(Roles = ShopRoles.Customer)]
    public class OrdersController : Controller
    {
        private static readonly Regex DeliveryPhoneRegex = new(@"^[0-9+\-\s().]{8,20}$", RegexOptions.Compiled);
        private const string DeliveryPhonePrefix = "SĐT nhận hàng:";

        /// <summary>Hiển thị danh sách đơn hàng của khách đang đăng nhập, có phân trang.</summary>
        /// <param name="page">Số trang hiện tại (mặc định 1).</param>
        /// <returns>View danh sách đơn của khách; chuyển sang Login nếu chưa đăng nhập.</returns>
        public async Task<IActionResult> Index(int page = 1)
        {
            var customerId = User.GetCustomerId();
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var input = new OrderSearchInput
            {
                Page = page < 1 ? 1 : page,
                PageSize = 10,
                SearchValue = "",
                Status = 0
            };

            var result = await SalesDataService.ListCustomerOrdersAsync(customerId.Value, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị chi tiết một đơn hàng; chỉ cho phép nếu đơn thuộc khách đang đăng nhập.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xem chi tiết.</param>
        /// <returns>
        /// View chi tiết đơn; 404 nếu đơn không tồn tại hoặc không thuộc khách này.
        /// </returns>
        public async Task<IActionResult> Details(int id)
        {
            var customerId = User.GetCustomerId();
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderForCustomerAsync(id, customerId.Value);
            if (order == null)
                return NotFound();

            ViewBag.Details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync() ?? new List<Province>();

            var (phone, address) = SplitDeliveryAddress(order.DeliveryAddress);
            ViewBag.DeliveryPhone = phone;
            ViewBag.DeliveryStreet = address;
            return View(order);
        }

        /// <summary>
        /// Cập nhật tỉnh/thành, địa chỉ và số điện thoại nhận hàng của đơn;
        /// chỉ thực hiện được khi đơn đang ở trạng thái Mới hoặc Đã duyệt.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần cập nhật.</param>
        /// <param name="deliveryProvince">Tỉnh/thành giao hàng mới.</param>
        /// <param name="deliveryAddress">Địa chỉ đường phố giao hàng mới.</param>
        /// <param name="deliveryPhone">Số điện thoại nhận hàng mới.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo thành công hoặc lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDelivery(int id, string deliveryProvince, string deliveryAddress, string deliveryPhone)
        {
            var customerId = User.GetCustomerId();
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderForCustomerAsync(id, customerId.Value);
            if (order == null)
                return NotFound();

            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                TempData["Error"] = "Không thể sửa thông tin giao hàng khi đơn đã chuyển trạng thái khác.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var p = (deliveryProvince ?? "").Trim();
            var a = (deliveryAddress ?? "").Trim();
            var ph = (deliveryPhone ?? "").Trim();

            if (string.IsNullOrWhiteSpace(p) || string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(ph))
            {
                TempData["Error"] = "Vui lòng nhập đủ: tỉnh/thành, địa chỉ và số điện thoại nhận hàng.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (!DeliveryPhoneRegex.IsMatch(ph))
            {
                TempData["Error"] = "Số điện thoại nhận hàng không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Giữ format thống nhất với lúc tạo đơn (Checkout).
            var combinedAddress = $"{DeliveryPhonePrefix} {ph}\n{a}";

            var updated = new Order
            {
                OrderID = id,
                CustomerID = customerId.Value,
                DeliveryProvince = p,
                DeliveryAddress = combinedAddress
            };

            var ok = await SalesDataService.UpdateOrderAsync(updated);
            TempData[ok ? "Message" : "Error"] = ok ? "Đã cập nhật thông tin giao hàng cho đơn này." : "Không thể cập nhật thông tin giao hàng (chỉ áp dụng khi chưa giao).";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Hủy đơn hàng của khách; chỉ được hủy khi đơn đang ở trạng thái Mới hoặc Đã duyệt.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hủy.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo thành công hoặc lỗi.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var customerId = User.GetCustomerId();
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderForCustomerAsync(id, customerId.Value);
            if (order == null)
                return NotFound();

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
            {
                TempData["Error"] = "Không thể hủy đơn ở trạng thái hiện tại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var ok = await SalesDataService.CancelOrderAsync(id);
            TempData[ok ? "Message" : "Error"] = ok ? "Đã hủy đơn hàng." : "Không thể hủy đơn hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Tách số điện thoại và địa chỉ đường từ chuỗi <c>DeliveryAddress</c> được lưu khi đặt hàng.
        /// </summary>
        /// <param name="raw">Chuỗi DeliveryAddress gốc lưu trong DB (có thể null hoặc rỗng).</param>
        /// <returns>
        /// Tuple (phone, address): phone là số điện thoại, address là địa chỉ đường; rỗng nếu không phân tích được.
        /// </returns>
        private static (string phone, string address) SplitDeliveryAddress(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ("", "");

            var text = raw.Trim();
            if (text.StartsWith(DeliveryPhonePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var rest = text.Substring(DeliveryPhonePrefix.Length).Trim();
                var idx = rest.IndexOf('\n');
                if (idx >= 0)
                {
                    var phone = rest.Substring(0, idx).Trim().TrimStart(':').Trim();
                    var addr = rest.Substring(idx + 1).Trim();
                    return (phone, addr);
                }
                return (rest.Trim().TrimStart(':').Trim(), "");
            }
            return ("", text);
        }
    }
}

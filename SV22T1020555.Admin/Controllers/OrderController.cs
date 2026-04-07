using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.Admin;
using SV22T1020555.Admin.Models;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Catalog;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;
using SV22T1020555.Models.Sales;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý nghệp vụ bán hàng: tìm kiếm đơn, lập đơn mới, duyệt, giao hàng, hủy và hoàn tất.
    /// </summary>
    public class OrderController : Controller
    {
        private const int PAGESIZE = 10;
        private const string ORDER_SEARCH = "OrderSearchInput";

        /// <summary>
        /// Giao diện nhập đầu vào tìm kiếm đơn hàng và hiển thị kết quả tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý đơn hàng";

            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE, // Đã sửa: dùng hằng số khai báo ở đầu class
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null, // Nếu trong Model khai báo là string thì bạn có thể để "" nhé
                    DateTo = null
                };
            }

            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm đơn hàng và trả về partial view kết quả phân trang.
        /// </summary>
        /// <param name="input">Tiêu chí tìm kiếm (từ khóa, trạng thái, khoảng ngày, trang).</param>
        /// <returns>
        /// Partial view danh sách đơn hàng.
        /// </returns>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            // Đã thêm: Đảm bảo PageSize luôn có giá trị để không bị lỗi lúc phân trang
            if (input.PageSize == 0)
                input.PageSize = PAGESIZE;

            // Flatpickr đang gửi ngày theo dạng dd/MM/yyyy, cần parse thủ công
            // để tránh phụ thuộc culture mặc định của server.
            var rawDateFrom = Request.Query["DateFrom"].ToString();
            if (!string.IsNullOrWhiteSpace(rawDateFrom) &&
                DateTime.TryParseExact(rawDateFrom, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            {
                input.DateFrom = parsedFrom;
            }

            var rawDateTo = Request.Query["DateTo"].ToString();
            if (!string.IsNullOrWhiteSpace(rawDateTo) &&
                DateTime.TryParseExact(rawDateTo, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            {
                input.DateTo = parsedTo;
            }

            var result = await SalesDataService.ListOrdersAsync(input);

            ApplicationContext.SetSessionData(ORDER_SEARCH, input);

            return View(result);
        }

        /// <summary>
        /// Hiển thị chi tiết một đơn hàng cùng các mặt hàng trong đơn.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xem.</param>
        /// <returns>
        /// View chi tiết đơn; chuyển về Index nếu không tìm thấy.
        /// </returns>
        public async Task<IActionResult> Detail(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            // Lấy thêm danh sách chi tiết mặt hàng trong đơn để hiển thị trên View
            ViewBag.OrderDetails = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Title = "Chi tiết đơn hàng";

            return View(data);
        }

        private const string SEARCH_PRODUCT = "SearchProductToSale";
        /// <summary>
        /// Giao diện cung cấp các chức năng nghiệp vụ lập đơn hàng mới.
        /// </summary>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            return View(input);
        }


        /// <summary>
        /// Tim hang de ban
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }
        /// <summary>
        /// Hiển thị giỏ hàng phân tách (shopping cart cho Admin tạo đơn mới).
        /// </summary>
        /// <returns>
        /// Partial/Full view giỏ hàng hiện tại.
        /// </returns>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }
        /// <summary>
        /// Thêm một mặt hàng vào giỏ hàng tạo đơn của Admin.
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần thêm.</param>
        /// <param name="quantity">Số lượng (phải > 0).</param>
        /// <param name="price">Giá bán (phải ≥ 0).</param>
        /// <returns>
        /// JSON ApiResult: 1 nếu thành công, 0 kèm thông báo lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId =0, int quantity =0, decimal price =0)
        {
            //Kiểm tra dữ liệu hợp lệ 
            if (productId <= 0)
                return Json(new ApiResult(0, "Mã mặt hàng không hợp lệ"));
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (price < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng này đã ngưng bán"));

            //Thêm hnagf vào giỏ
            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "",
                Quantity = quantity,
                SalePrice = price
            };
            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa đơn hàng (GET).
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xóa.</param>
        /// <returns>
        /// View xác nhận xóa; chuyển về Index nếu đơn không tồn tại.
        /// </returns>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Xóa đơn hàng";
            return View(data);
        }

        /// <summary>
        /// Thực hiện xóa đơn hàng khỏi CSDL (POST).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa.</param>
        /// <param name="returnUrl">URL để quay lại sau khi xóa (hiện đang không sử dụng).</param>
        /// <returns>
        /// Chuyển về Index sau khi xóa.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int orderID, string returnUrl = "")
        {
            await SalesDataService.DeleteOrderAsync(orderID);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa số lượng và giá của một mặt hàng trong giỏ (partial view).
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần chỉnh sửa.</param>
        /// <returns>
        /// Partial view form chỉnh sửa.
        /// </returns>
        public IActionResult EditCartItem( int productId)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            return PartialView(item);
        }
        /// <summary>
        /// Cập nhật số lượng và giá bán của mặt hàng trong giỏ qua AJAX.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần cập nhật.</param>
        /// <param name="quantity">Số lượng mới (phải > 0).</param>
        /// <param name="salePrice">Giá bán mới (phải ≥ 0).</param>
        /// <returns>
        /// JSON ApiResult: 1 nếu thành công, 0 kèm thông báo lỗi.
        /// </returns>
        [HttpPost]
        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            //Kiểm tra dữ liệu
            if (!ModelState.IsValid)
                return Json(new ApiResult(0, "Dữ liệu gửi lên không hợp lệ"));
            if (productID <= 0)
                return Json(new ApiResult(0, "Mã mặt hàng không hợp lệ"));
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var item = ShoppingCartHelper.GetCartItem(productID);
            if (item == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại trong giỏ"));

            ShoppingCartHelper.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }
        /// <summary>
        /// Tạo đơn hàng từ giỏ hiện tại của Admin; hỗ trợ cả giao hàng và bán tại cửa hàng.
        /// </summary>
        /// <param name="customerID">Mã khách hàng (bắt buộc khi giao hàng, tùy chọn khi bán tại cửa).</param>
        /// <param name="province">Tỉnh/thành giao hàng (bắt buộc khi giao).</param>
        /// <param name="address">Địa chỉ giao hàng (bắt buộc khi giao).</param>
        /// <param name="deliveryMode">"ship" để giao hàng; "pickup" để bán tại cửa.</param>
        /// <returns>
        /// JSON ApiResult: orderID nếu thành công, 0 kèm thông báo lỗi.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            int? customerID = null,
            string province = "",
            string address = "",
            string deliveryMode = "ship")
        {
            if (customerID.HasValue && customerID.Value < 0)
                return Json(new ApiResult(0, "Khách hàng không hợp lệ"));

            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart.Count == 0)
            {
                return Json(new ApiResult(0, "Giỏ hàng đang trống"));
            }

            var isPickup = string.Equals(deliveryMode?.Trim(), "pickup", StringComparison.OrdinalIgnoreCase);
            if (isPickup)
            {
                province = "";
                address = "Bán tại cửa hàng — không giao";
            }
            else
            {
                if (!customerID.HasValue || customerID.Value <= 0)
                    return Json(new ApiResult(0, "Giao hàng: vui lòng chọn khách hàng trong danh bạ."));
                if (string.IsNullOrWhiteSpace(province))
                    return Json(new ApiResult(0, "Giao hàng: vui lòng chọn tỉnh/thành."));
                if (string.IsNullOrWhiteSpace(address))
                    return Json(new ApiResult(0, "Giao hàng: vui lòng nhập địa chỉ giao."));
            }

            var lines = cart.Select(item => new OrderDetail
            {
                OrderID = 0,
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.SalePrice
            }).ToList();

            int orderID = await SalesDataService.CreateOrderWithDetailsAsync(customerID ?? 0, province, address, lines);
            if (orderID <= 0)
                return Json(new ApiResult(0, "Không lập được đơn hàng (giỏ hàng, khách hàng hoặc mặt hàng không hợp lệ)."));

            //Lập đơn thành công -> xóa giỏ hàng hiện tại
            ShoppingCartHelper.ClearCart();
            return Json(new ApiResult(orderID, ""));
        }


        /// <summary>
        /// Xóa một sản phẩm khỏi giỏ hàng hoặc đơn hàng.
        /// </summary>
        public IActionResult DeleteCartItem(int productId)
        {
            //POST: xóa hàng khỏi giỏ
            if(Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
                return Json(new ApiResult(1, ""));
            }
            //GET: Hiển thị giao diện
            ViewBag.ProductID = productId;
            return PartialView();
        }




        /// <summary>
        /// Xóa toàn bộ sản phẩm trong giỏ hàng hiện tại.
        /// </summary>
        public IActionResult ClearCart()
        {
            //POST: Xóa giở hàng
            if(Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }
            //GET: Hiển thị giao diện
            return PartialView();
        }




        /// <summary>
        /// Duyệt đơn hàng (chuyển trạng thái thành Đã duyệt); chỉ khi đơn đang Chờ duyệt.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần duyệt.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        public async Task<IActionResult> Accept(int id)
        {
            if (!TryGetEmployeeId(out int employeeID))
                return RedirectToAction("Login", "Account");

            var ok = await SalesDataService.AcceptOrderAsync(id, employeeID);
            return RedirectToOrderDetail(id, ok ? null : "Không thể duyệt đơn (chỉ đơn chờ duyệt mới được duyệt).");
        }

        /// <summary>
        /// Hiển thị modal chọn người giao hàng cho đơn đã duyệt (GET).
        /// </summary>
        /// <param name="id">Mã đơn hàng cần chọn shipper.</param>
        /// <returns>
        /// Partial view modal; 404 nếu không tìm thấy, 400 nếu đơn không ở trạng thái phù hợp.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.Accepted)
                return BadRequest();

            var shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 500,
                SearchValue = ""
            });

            var model = new OrderShippingFormModel
            {
                OrderID = id,
                Shippers = shippers.DataItems ?? new List<Shipper>()
            };
            return PartialView(model);
        }

        /// <summary>
        /// Gán shipper và chuyển đơn sang trạng thái Đang giao (POST).
        /// </summary>
        /// <param name="id">Mã đơn hàng cần giao.</param>
        /// <param name="shipperID">Mã shipper được chọn.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            var ok = await SalesDataService.ShipOrderAsync(id, shipperID);
            return RedirectToOrderDetail(id, ok ? null : "Không thể giao hàng (đơn phải đã duyệt và shipper hợp lệ).");
        }

        /// <summary>
        /// Hiển thị modal sửa thông tin giao hàng của đơn (chỉ New/Đã duyệt) — GET.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần sửa.</param>
        /// <returns>
        /// Partial view modal; 404 hoặc 400 nếu đơn không hợp lệ.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> EditInfo(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                return BadRequest();

            var provinces = await DictionaryDataService.ListProvincesAsync();
            var model = new OrderEditInfoFormModel
            {
                OrderID = id,
                CustomerID = order.CustomerID,
                DeliveryProvince = order.DeliveryProvince,
                DeliveryAddress = order.DeliveryAddress,
                Provinces = provinces ?? new List<SV22T1020555.Models.DataDictionary.Province>()
            };
            return PartialView(model);
        }

        /// <summary>
        /// Lưu thông tin giao hàng mới (tỉnh/thành, địa chỉ) cho đơn (POST).
        /// </summary>
        /// <param name="id">Mã đơn hàng cần cập nhật.</param>
        /// <param name="deliveryProvince">Tỉnh/thành giao hàng mới.</param>
        /// <param name="deliveryAddress">Địa chỉ giao hàng mới.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInfoConfirm(int id, string deliveryProvince, string deliveryAddress)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                return RedirectToOrderDetail(id, "Không thể sửa thông tin khi đơn đã chuyển trạng thái khác.");

            var updated = new Order
            {
                OrderID = id,
                CustomerID = order.CustomerID,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress
            };

            var ok = await SalesDataService.UpdateOrderAsync(updated);
            return RedirectToOrderDetail(id, ok ? null : "Không thể cập nhật thông tin giao hàng (chỉ New/Đã duyệt và dữ liệu hợp lệ).");
        }

        /// <summary>
        /// Hiển thị modal xác nhận từ chối đơn (chỉ đơn Chờ duyệt) — GET.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần từ chối.</param>
        /// <returns>
        /// Partial view modal; 404 hoặc 400 nếu không hợp lệ.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.New)
                return BadRequest();
            return PartialView(id);
        }

        /// <summary>
        /// Thực hiện từ chối đơn hàng (chuyển trạng thái sang Bị từ chối) — POST.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần từ chối.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectConfirm(int id)
        {
            if (!TryGetEmployeeId(out int employeeID))
                return RedirectToAction("Login", "Account");

            var ok = await SalesDataService.RejectOrderAsync(id, employeeID);
            return RedirectToOrderDetail(id, ok ? null : "Không thể từ chối đơn (chỉ đơn chờ duyệt).");
        }

        /// <summary>
        /// Hiển thị modal xác nhận hủy đơn (New/Đã duyệt/Đang giao) — GET.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hủy.</param>
        /// <returns>
        /// Partial view modal; 400 nếu đơn không hợp lệ.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted &&
                order.Status != OrderStatusEnum.Shipping)
                return BadRequest();
            return PartialView(id);
        }

        /// <summary>
        /// Thực hiện hủy đơn hàng — POST.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hủy.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirm(int id)
        {
            var ok = await SalesDataService.CancelOrderAsync(id);
            return RedirectToOrderDetail(id, ok ? null : "Không thể hủy đơn với trạng thái hiện tại.");
        }

        /// <summary>
        /// Hiển thị modal xác nhận thu hồi đơn đang giao về Đã duyệt — GET.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần thu hồi.</param>
        /// <returns>
        /// Partial view modal; 400 nếu đơn không phải Đang giao.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> RecallFromShipping(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.Shipping &&
                order.Status != OrderStatusEnum.Accepted)
                return BadRequest();
            return PartialView(id);
        }

        /// <summary>
        /// Thực hiện thu hồi đơn từ trạng thái Đang giao về Đã duyệt — POST.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần thu hồi.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecallFromShippingConfirm(int id)
        {
            var ok = await SalesDataService.RevertShippingToAcceptedAsync(id);
            return RedirectToOrderDetail(id, ok ? null : "Không thể thu hồi (chỉ áp dụng đơn đang giao).");
        }

        /// <summary>
        /// Hiển thị modal xác nhận hoàn tất giao hàng (đơn đang giao) — GET.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hoàn tất.</param>
        /// <returns>
        /// Partial view modal; 400 nếu đơn không ở trạng thái Đang giao.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Finish(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatusEnum.Shipping)
                return BadRequest();
            return PartialView(id);
        }

        /// <summary>
        /// Thực hiện hoàn tất giao hàng, chuyển đơn sang Hoàn thành — POST.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hoàn tất.</param>
        /// <returns>
        /// Chuyển về trang chi tiết đơn kèm thông báo.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishConfirm(int id)
        {
            var ok = await SalesDataService.CompleteOrderAsync(id);
            return RedirectToOrderDetail(id, ok ? null : "Không thể hoàn tất (chỉ đơn đã duyệt hoặc đang giao).");
        }

        /// <summary>
        /// Chuyển hướng về trang chi tiết đơn kèm thông báo lỗi nếu có (helper nội bộ).</summary>
        /// <param name="id">Mã đơn hàng đích.</param>
        /// <param name="errorMessage">Thông báo lỗi; null hoặc rỗng nếu không có lỗi.</param>
        /// <returns>
        /// RedirectToAction sang action Detail.
        /// </returns>
        private IActionResult RedirectToOrderDetail(int id, string? errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
                TempData["OrderError"] = errorMessage;
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Lấy EmployeeID từ Claims của người dùng đang đăng nhập (helper nội bộ).
        /// </summary>
        /// <param name="employeeID">Giá trị EmployeeID nếu thành công; 0 nếu thất bại.</param>
        /// <returns>
        /// True nếu lấy EmployeeID hợp lệ thành công, ngược lại False.
        /// </returns>
        private bool TryGetEmployeeId(out int employeeID)
        {
            employeeID = 0;
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserId))
                return false;
            return int.TryParse(userData.UserId, out employeeID) && employeeID > 0;
        }
    }
}

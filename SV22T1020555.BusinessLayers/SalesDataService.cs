using System.Collections.Generic;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.DataLayers.SQLServer;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Sales;

namespace SV22T1020555.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        /// <summary>Giới hạn độ dài chuỗi gửi từ client (không tin tưởng input).</summary>
        private const int MaxDeliveryProvinceLength = 255;
        private const int MaxDeliveryAddressLength = 500;
        /// <summary>Số dòng tối đa trong một đơn (chống payload quá lớn / lạm dụng).</summary>
        private const int MaxOrderLines = 500;
        /// <summary>Số lượng tối đa mỗi dòng (int hợp lệ nghiệp vụ).</summary>
        private const int MaxQuantityPerLine = 1_000_000;
        /// <summary>Trần đơn giá một dòng (tránh số quá lớn / sai sót nhập liệu).</summary>
        private const decimal MaxSalePricePerUnit = 999_999_999_999.99m;

        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang.
        /// </summary>
        /// <param name="input">
        /// Thông tin tìm kiếm và phân trang (từ khóa, trạng thái, trang hiển thị, số dòng mỗi trang).
        /// Nếu null, mặc định lấy trang 1 với 10 dòng.
        /// </param>
        /// <returns>
        /// Kết quả tìm kiếm dưới dạng danh sách đơn hàng có phân trang.
        /// </returns>
        public static async Task<PagedResult<OrderSearchInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = 10,
                    SearchValue = ""
                };
            }
            else
            {
                var st = (int)input.Status;
                if (st != 0 && !Enum.IsDefined(typeof(OrderStatusEnum), st))
                    input.Status = (OrderStatusEnum)0;
            }

            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng của một khách hàng dưới dạng phân trang.
        /// </summary>
        /// <param name="customerID">Mã khách hàng cần lấy đơn hàng.</param>
        /// <param name="input">
        /// Thông tin phân trang và lọc (trạng thái, trang hiển thị, số dòng mỗi trang).
        /// Trường SearchValue bị bỏ qua (chỉ lọc theo khách).
        /// </param>
        /// <returns>
        /// Kết quả dưới dạng danh sách đơn hàng của khách có phân trang;
        /// trả về danh sách rỗng nếu <paramref name="customerID"/> không hợp lệ.
        /// </returns>
        public static async Task<PagedResult<OrderSearchInfo>> ListCustomerOrdersAsync(int customerID, OrderSearchInput input)
        {
            if (customerID <= 0)
            {
                return new PagedResult<OrderSearchInfo>
                {
                    Page = 1,
                    PageSize = input?.PageSize > 0 ? input.PageSize : 10,
                    RowCount = 0,
                    DataItems = new List<OrderSearchInfo>()
                };
            }

            if (input == null)
            {
                input = new OrderSearchInput { Page = 1, PageSize = 10, SearchValue = "" };
            }
            else
            {
                var st = (int)input.Status;
                if (st != 0 && !Enum.IsDefined(typeof(OrderStatusEnum), st))
                    input.Status = (OrderStatusEnum)0;
            }

            input.CustomerID = customerID;
            input.SearchValue = "";
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng dựa vào mã đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần tìm.</param>
        /// <returns>
        /// Đối tượng OrderViewInfo nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            if (orderID <= 0)
                return null;
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng, chỉ trả về kết quả nếu đơn thuộc đúng khách đang đăng nhập.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần tìm.</param>
        /// <param name="customerID">Mã khách hàng xác thực quyền sở hữu đơn.</param>
        /// <returns>
        /// Đối tượng OrderViewInfo nếu tìm thấy và thuộc đúng khách, ngược lại trả về null.
        /// </returns>
        public static async Task<OrderViewInfo?> GetOrderForCustomerAsync(int orderID, int customerID)
        {
            if (orderID <= 0 || customerID <= 0)
                return null;
            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.CustomerID != customerID)
                return null;
            return order;
        }

        /// <summary>
        /// Tạo một đơn hàng mới với trạng thái Mới (không kèm chi tiết dòng hàng).
        /// </summary>
        /// <param name="customerID">Mã khách hàng đặt hàng.</param>
        /// <param name="deliveryProcince">Tỉnh/thành giao hàng.</param>
        /// <param name="deliverAddress">Địa chỉ giao hàng chi tiết.</param>
        /// <returns>
        /// Mã đơn hàng được tạo mới; trả về 0 nếu không tạo được.
        /// </returns>
        public static async Task<int> AddOrderAsync(int customerID, string deliveryProcince, string deliverAddress)
        {
            if (customerID < 0)
                return 0;

            if (customerID > 0)
            {
                var customer = await PartnerDataService.GetCustomerAsync(customerID);
                if (customer == null)
                    return 0;
            }

            var province = (deliveryProcince ?? "").Trim();
            var address = (deliverAddress ?? "").Trim();
            if (province.Length > MaxDeliveryProvinceLength || address.Length > MaxDeliveryAddressLength)
                return 0;

            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address,
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now
            };
            return await orderDB.AddAsync(order);
        }

        /// <summary>
        /// Tạo đơn hàng mới kèm toàn bộ dòng chi tiết trong một transaction (dùng khi đặt hàng từ giỏ).
        /// </summary>
        /// <param name="customerID">Mã khách hàng đặt hàng.</param>
        /// <param name="deliveryProvince">Tỉnh/thành giao hàng.</param>
        /// <param name="deliveryAddress">Địa chỉ giao hàng chi tiết.</param>
        /// <param name="lines">Danh sách dòng hàng (mã sản phẩm, số lượng, đơn giá).</param>
        /// <param name="deliveryContactPhone">
        /// SĐT nhận hàng riêng cho đơn này; lưu kèm trong DeliveryAddress, không cập nhật hồ sơ khách.
        /// </param>
        /// <returns>
        /// Mã đơn hàng được tạo mới; trả về 0 nếu không tạo được (dữ liệu không hợp lệ, sản phẩm ngừng bán, v.v.).
        /// </returns>
        public static async Task<int> CreateOrderWithDetailsAsync(int customerID, string deliveryProvince,
            string deliveryAddress, IReadOnlyList<OrderDetail> lines, string? deliveryContactPhone = null)
        {
            if (lines == null || lines.Count == 0 || lines.Count > MaxOrderLines)
                return 0;

            if (customerID < 0)
                return 0;

            if (customerID > 0)
            {
                var customer = await PartnerDataService.GetCustomerAsync(customerID);
                if (customer == null)
                    return 0;
            }

            var province = (deliveryProvince ?? "").Trim();
            var addr = (deliveryAddress ?? "").Trim();
            var phone = (deliveryContactPhone ?? "").Trim();
            if (phone.Length > 0)
                addr = "SĐT nhận hàng: " + phone + (addr.Length > 0 ? "\n" : "") + addr;
            if (province.Length > MaxDeliveryProvinceLength || addr.Length > MaxDeliveryAddressLength)
                return 0;

            var seen = new HashSet<int>();
            foreach (var line in lines)
            {
                if (line == null)
                    return 0;
                if (line.ProductID <= 0 || line.Quantity <= 0 || line.Quantity > MaxQuantityPerLine)
                    return 0;
                if (line.SalePrice < 0 || line.SalePrice > MaxSalePricePerUnit)
                    return 0;
                if (!seen.Add(line.ProductID))
                    return 0;

                var product = await CatalogDataService.GetProductAsync(line.ProductID);
                if (product == null || !product.IsSelling)
                    return 0;
            }

            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = addr,
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now
            };

            var details = lines.Select(l => new OrderDetail
            {
                OrderID = 0,
                ProductID = l.ProductID,
                Quantity = l.Quantity,
                SalePrice = l.SalePrice
            }).ToList();

            return await orderDB.AddOrderWithDetailsAsync(order, details);
        }

        /// <summary>
        /// Cập nhật thông tin giao hàng của một đơn hàng (tỉnh/thành, địa chỉ).
        /// Chỉ cho phép cập nhật khi đơn đang ở trạng thái Mới hoặc Đã duyệt.
        /// </summary>
        /// <param name="data">Thông tin đơn hàng cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            if (data == null || data.OrderID <= 0)
                return false;

            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                return false;

            // Chỉ cho phép cập nhật thông tin đơn hàng khi đơn chưa giao.
            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            if (data.CustomerID.HasValue && data.CustomerID.Value < 0)
                return false;
            if (data.CustomerID == 0)
                data.CustomerID = null;
            if (data.CustomerID is > 0)
            {
                var cust = await PartnerDataService.GetCustomerAsync(data.CustomerID.Value);
                if (cust == null)
                    return false;
            }

            var p = (data.DeliveryProvince ?? "").Trim();
            var a = (data.DeliveryAddress ?? "").Trim();
            if (p.Length > MaxDeliveryProvinceLength || a.Length > MaxDeliveryAddressLength)
                return false;
            data.DeliveryProvince = p;
            data.DeliveryAddress = a;

            // Không cho phép caller tự ý đổi các mốc thời gian/ trạng thái ở hàm update chung
            data.Status = order.Status;
            data.OrderTime = order.OrderTime;
            data.EmployeeID = order.EmployeeID;
            data.AcceptTime = order.AcceptTime;
            data.ShipperID = order.ShipperID;
            data.ShippedTime = order.ShippedTime;
            data.FinishedTime = order.FinishedTime;

            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một đơn hàng và toàn bộ chi tiết của đơn đó.
        /// Chỉ cho phép xóa khi đơn đang ở trạng thái Mới hoặc Đã duyệt.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            if (orderID <= 0)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            // Cho phép xóa khi đơn còn ở New hoặc Accepted.
            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            // Xóa chi tiết trước để tránh lỗi ràng buộc FK (nếu DB không cascade).
            var details = await orderDB.ListDetailsAsync(orderID);
            foreach (var detail in details)
            {
                await orderDB.DeleteDetailAsync(orderID, detail.ProductID);
            }

            return await orderDB.DeleteAsync(orderID);
        }

        /// <summary>
        /// Lấy dữ liệu thống kê tổng quan (số đơn theo trạng thái, doanh thu) cho dashboard quản trị.
        /// </summary>
        /// <returns>
        /// Đối tượng SalesDashboardData chứa các chỉ số thống kê tổng hợp.
        /// </returns>
        public static async Task<SalesDashboardData> GetDashboardDataAsync()
        {
            return await orderDB.GetDashboardDataAsync();
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng (chuyển trạng thái từ Mới sang Đã duyệt).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần duyệt.</param>
        /// <param name="employeeID">Mã nhân viên thực hiện duyệt đơn.</param>
        /// <returns>
        /// True nếu duyệt thành công, False nếu đơn không hợp lệ hoặc không ở trạng thái Mới.
        /// </returns>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            if (orderID <= 0 || employeeID <= 0)
                return false;

            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            if (employee == null)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng (chuyển trạng thái từ Mới sang Đã từ chối).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần từ chối.</param>
        /// <param name="employeeID">Mã nhân viên thực hiện từ chối.</param>
        /// <returns>
        /// True nếu từ chối thành công, False nếu đơn không hợp lệ hoặc không ở trạng thái Mới.
        /// </returns>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            if (orderID <= 0 || employeeID <= 0)
                return false;

            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            if (employee == null)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng (chuyển trạng thái sang Đã hủy).
        /// Áp dụng được khi đơn đang ở trạng thái Mới, Đã duyệt hoặc Đang giao.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần hủy.</param>
        /// <returns>
        /// True nếu hủy thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            if (orderID <= 0)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted &&
                order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn trả đơn đang giao về trạng thái Đã duyệt (khi giao không thành hoặc cần đổi shipper).
        /// Xóa thông tin shipper và thời điểm bàn giao.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần hoàn trả trạng thái.</param>
        /// <returns>
        /// True nếu hoàn trả thành công, False nếu đơn không ở trạng thái Đang giao.
        /// </returns>
        public static async Task<bool> RevertShippingToAcceptedAsync(int orderID)
        {
            if (orderID <= 0)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.Shipping)
                return false;

            order.Status = OrderStatusEnum.Accepted;
            order.ShipperID = null;
            order.ShippedTime = null;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Bàn giao đơn hàng cho shipper (chuyển trạng thái từ Đã duyệt sang Đang giao).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần giao.</param>
        /// <param name="shipperID">Mã shipper nhận giao hàng.</param>
        /// <returns>
        /// True nếu bàn giao thành công, False nếu đơn không hợp lệ hoặc không ở trạng thái Đã duyệt.
        /// </returns>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            if (orderID <= 0 || shipperID <= 0)
                return false;

            var shipper = await PartnerDataService.GetShipperAsync(shipperID);
            if (shipper == null)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng (chuyển trạng thái sang Hoàn thành).
        /// Áp dụng khi đơn đang ở trạng thái Đang giao hoặc Đã duyệt.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần hoàn tất.</param>
        /// <returns>
        /// True nếu hoàn tất thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            if (orderID <= 0)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.Shipping &&
                order.Status != OrderStatusEnum.Accepted)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;
            
            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách chi tiết (các dòng hàng) của một đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần lấy chi tiết.</param>
        /// <returns>
        /// Danh sách các dòng hàng trong đơn; trả về danh sách rỗng nếu mã không hợp lệ.
        /// </returns>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            if (orderID <= 0)
                return new List<OrderDetailViewInfo>();
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một dòng hàng trong đơn.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng.</param>
        /// <param name="productID">Mã sản phẩm trong dòng hàng.</param>
        /// <returns>
        /// Đối tượng OrderDetailViewInfo nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            if (orderID <= 0 || productID <= 0)
                return null;
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm một dòng hàng mới vào đơn hàng.
        /// Chỉ áp dụng khi đơn đang ở trạng thái Mới; không cho phép thêm trùng sản phẩm.
        /// </summary>
        /// <param name="data">Thông tin dòng hàng cần thêm (mã đơn, mã sản phẩm, số lượng, đơn giá).</param>
        /// <returns>
        /// True nếu thêm thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            if (data == null || data.OrderID <= 0 || data.ProductID <= 0)
                return false;
            if (data.Quantity <= 0 || data.Quantity > MaxQuantityPerLine)
                return false;
            if (data.SalePrice < 0 || data.SalePrice > MaxSalePricePerUnit)
                return false;

            var product = await CatalogDataService.GetProductAsync(data.ProductID);
            if (product == null || !product.IsSelling)
                return false;

            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null || order.Status != OrderStatusEnum.New)
                return false;

            // Tránh thêm trùng một mặt hàng đã tồn tại trong đơn.
            var existedDetail = await orderDB.GetDetailAsync(data.OrderID, data.ProductID);
            if (existedDetail != null)
                return false;

            var lineCount = (await orderDB.ListDetailsAsync(data.OrderID)).Count;
            if (lineCount >= MaxOrderLines)
                return false;

            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật số lượng và đơn giá của một dòng hàng trong đơn.
        /// Chỉ áp dụng khi đơn đang ở trạng thái Mới.
        /// </summary>
        /// <param name="data">Thông tin dòng hàng cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            if (data == null || data.OrderID <= 0 || data.ProductID <= 0)
                return false;
            if (data.Quantity <= 0 || data.Quantity > MaxQuantityPerLine)
                return false;
            if (data.SalePrice < 0 || data.SalePrice > MaxSalePricePerUnit)
                return false;

            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null || order.Status != OrderStatusEnum.New)
                return false;

            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa một dòng hàng khỏi đơn hàng.
        /// Chỉ áp dụng khi đơn đang ở trạng thái Mới.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng chứa dòng cần xóa.</param>
        /// <param name="productID">Mã sản phẩm của dòng hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            if (orderID <= 0 || productID <= 0)
                return false;

            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.New)
                return false;

            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion
    }
}
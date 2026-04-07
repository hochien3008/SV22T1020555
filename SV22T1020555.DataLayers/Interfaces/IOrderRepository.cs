using System.Collections.Generic;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Sales;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các chức năng xử lý dữ liệu cho đơn hàng
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang.
        /// </summary>
        /// <param name="input">Tiêu chí tìm kiếm: từ khóa, trạng thái, khoảng ngày, trang, số dòng.</param>
        /// <returns>
        /// PagedResult chứa danh sách OrderSearchInfo và thông tin phân trang.
        /// </returns>
        Task<PagedResult<OrderSearchInfo>> ListAsync(OrderSearchInput input);
        /// <summary>
        /// Lấy thông tin đầy đủ của một đơn hàng (join kèm tên khách hàng, nhân viên, shipper).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần lấy.</param>
        /// <returns>
        /// OrderViewInfo nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        Task<OrderViewInfo?> GetAsync(int orderID);
        /// <summary>
        /// Bổ sung đơn hàng (header only, chưa bao gồm chi tiết).
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng cần thêm.</param>
        /// <returns>
        /// Mã đơn hàng (IDENTITY) vừa được bổ sung;
        /// 0 nếu thất bại.
        /// </returns>
        Task<int> AddAsync(Order data);

        /// <summary>
        /// Tạo đơn hàng và toàn bộ chi tiết trong một transaction (atomic).
        /// </summary>
        /// <param name="order">Dữ liệu header đơn hàng.</param>
        /// <param name="details">Danh sách chi tiết mặt hàng cần thêm kèm.</param>
        /// <returns>
        /// Mã đơn hàng nếu thành công;
        /// 0 nếu transaction thất bại.
        /// </returns>
        Task<int> AddOrderWithDetailsAsync(Order order, IReadOnlyList<OrderDetail> details);
        /// <summary>
        /// Cập nhật thông tin đơn hàng (thông tin giao hàng, khách hàng).
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng mới (OrderID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy hoặc trạng thái không phải New/Accepted.
        /// </returns>
        Task<bool> UpdateAsync(Order data);
        /// <summary>
        /// Xóa đơn hàng cùng toàn bộ chi tiết.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy đơn hàng.
        /// </returns>
        Task<bool> DeleteAsync(int orderID);


        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng (kèm thông tin sản phẩm).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần lấy chi tiết.</param>
        /// <returns>
        /// Danh sách OrderDetailViewInfo;
        /// danh sách rỗng nếu đơn không có mặt hàng.
        /// </returns>
        Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID);
        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng.</param>
        /// <param name="productID">Mã mặt hàng.</param>
        /// <returns>
        /// OrderDetailViewInfo nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID);
        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng.
        /// </summary>
        /// <param name="data">Chi tiết mặt hàng cần thêm (OrderID, ProductID, Quantity, SalePrice).</param>
        /// <returns>
        /// True nếu thêm thành công;
        /// False nếu thất bại.
        /// </returns>
        Task<bool> AddDetailAsync(OrderDetail data);
        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng.
        /// </summary>
        /// <param name="data">Chi tiết mặt hàng mới (cần có OrderID và ProductID hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        Task<bool> UpdateDetailAsync(OrderDetail data);
        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng chứa mặt hàng cần xóa.</param>
        /// <param name="productID">Mã mặt hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        Task<bool> DeleteDetailAsync(int orderID, int productID);

        /// <summary>
        /// Thống kê nhanh cho dashboard trang chủ quản trị.
        /// </summary>
        /// <returns>
        /// SalesDashboardData chứa số đơn theo từng trạng thái và tổng doanh thu.
        /// </returns>
        Task<SalesDashboardData> GetDashboardDataAsync();
    }
}

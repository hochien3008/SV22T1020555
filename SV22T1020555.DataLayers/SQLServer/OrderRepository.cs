using Dapper;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Sales;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai IOrderRepository trên SQL Server;
    /// quản lý đơn hàng, chi tiết đơn hàng và dashboard bằng Dapper.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public OrderRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Mở và trả về IDbConnection đã được kết nối.
        /// </summary>
        /// <returns>
        /// IDbConnection đã mở kết nối; nhớ dispose sau khi dùng.
        /// </returns>
        private IDbConnection OpenConnection()
        {
            IDbConnection connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Tìm kiếm đơn hàng theo nhiều tiêu chí và trả về danh sách phân trang
        /// (join kèm tên khách hàng, nhân viên và tổng tiền từng đơn).
        /// </summary>
        /// <param name="input">Bộ lọc: từ khóa tên khách, trạng thái, khoảng ngày, mã khách, trang, số dòng.</param>
        /// <returns>
        /// PagedResult chứa danh sách OrderSearchInfo và thông tin phân trang.
        /// </returns>
        public async Task<PagedResult<OrderSearchInfo>> ListAsync(OrderSearchInput input)
        {
            using (var connection = OpenConnection())
            {
                int rowCount;
                List<OrderSearchInfo> data;

                var sql = @"SELECT COUNT(*)
                            FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            WHERE COALESCE(c.CustomerName, '') LIKE @searchValue
                              AND (@status = 0 OR o.Status = @status)
                              AND (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                              AND (@dateTo IS NULL OR o.OrderTime < DATEADD(day, 1, @dateTo))
                              AND (@customerId = 0 OR o.CustomerID = @customerId);

                            SELECT o.OrderID, o.CustomerID, o.OrderTime,
                                   o.DeliveryProvince, o.DeliveryAddress, o.EmployeeID, o.AcceptTime,
                                   o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status,
                                   c.CustomerName, c.Phone AS CustomerPhone,
                                   e.FullName AS EmployeeName,
                                   ISNULL(od.SumOfPrice, 0) AS SumOfPrice
                            FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            OUTER APPLY
                            (
                                SELECT SUM(d.Quantity * d.SalePrice) AS SumOfPrice
                                FROM OrderDetails d
                                WHERE d.OrderID = o.OrderID
                            ) od
                            WHERE COALESCE(c.CustomerName, '') LIKE @searchValue
                              AND (@status = 0 OR o.Status = @status)
                              AND (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                              AND (@dateTo IS NULL OR o.OrderTime < DATEADD(day, 1, @dateTo))
                              AND (@customerId = 0 OR o.CustomerID = @customerId)
                            ORDER BY o.OrderTime DESC
                            OFFSET (@page - 1) * @pageSize ROWS
                            FETCH NEXT @pageSize ROWS ONLY";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    page = input.Page,
                    pageSize = input.PageSize,
                    searchValue = "%" + (input.SearchValue ?? "") + "%",
                    status = (int)input.Status,
                    dateFrom = input.DateFrom,
                    dateTo = input.DateTo,
                    customerId = input.CustomerID
                }))
                {
                    rowCount = multi.Read<int>().Single();
                    data = multi.Read<OrderSearchInfo>().ToList();
                }

                return new PagedResult<OrderSearchInfo>()
                {
                    Page = input.Page,
                    PageSize = input.PageSize,
                    RowCount = rowCount,
                    DataItems = data
                };
            }
        }

        /// <summary>
        /// Lấy thông tin đầy đủ của một đơn hàng (join khách hàng, nhân viên, shipper).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần lấy.</param>
        /// <returns>
        /// OrderViewInfo nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT o.OrderID, o.CustomerID, o.OrderTime,
                                   o.DeliveryProvince, o.DeliveryAddress, o.EmployeeID, o.AcceptTime,
                                   o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status,
                                   ISNULL(c.CustomerName, '') AS CustomerName,
                                   ISNULL(c.ContactName, '') AS CustomerContactName,
                                   ISNULL(c.Email, '') AS CustomerEmail,
                                   ISNULL(c.Phone, '') AS CustomerPhone,
                                   ISNULL(c.Address, '') AS CustomerAddress,
                                   ISNULL(e.FullName, '') AS EmployeeName,
                                   ISNULL(s.ShipperName, '') AS ShipperName,
                                   ISNULL(s.Phone, '') AS ShipperPhone
                            FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE o.OrderID = @orderID";
                return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
            }
        }

        /// <summary>
        /// Thêm mới một đơn hàng (header only, chưa bao gồm chi tiết mặt hàng).
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng cần thêm.</param>
        /// <returns>
        /// OrderID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<int> AddAsync(Order data)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"INSERT INTO Orders
                            (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, Status)
                            VALUES
                            (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @Status);
                            SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Tạo đơn hàng và toàn bộ chi tiết trong một SQL transaction (atomic).
        /// Nếu bất kỳ bước nào thất bại, toàn bộ transaction sẽ được rollback.
        /// </summary>
        /// <param name="order">Dữ liệu header đơn hàng cần tạo.</param>
        /// <param name="details">Danh sách chi tiết mặt hàng (phải có ít nhất 1 mặt hàng).</param>
        /// <returns>
        /// OrderID vừa tạo nếu transaction thành công;
        /// 0 nếu details rỗng hoặc có lỗi xảy ra.
        /// </returns>
        public async Task<int> AddOrderWithDetailsAsync(Order order, IReadOnlyList<OrderDetail> details)
        {
            if (details == null || details.Count == 0)
                return 0;

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertOrderSql = @"INSERT INTO Orders
                            (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, Status)
                            VALUES
                            (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @Status);
                            SELECT CAST(SCOPE_IDENTITY() as int);";

                var orderId = await connection.ExecuteScalarAsync<int>(insertOrderSql, order, transaction);
                if (orderId <= 0)
                {
                    transaction.Rollback();
                    return 0;
                }

                const string insertDetailSql = @"INSERT INTO OrderDetails
                            (OrderID, ProductID, Quantity, SalePrice)
                            VALUES
                            (@OrderID, @ProductID, @Quantity, @SalePrice)";

                foreach (var d in details)
                {
                    var row = new OrderDetail
                    {
                        OrderID = orderId,
                        ProductID = d.ProductID,
                        Quantity = d.Quantity,
                        SalePrice = d.SalePrice
                    };
                    var n = await connection.ExecuteAsync(insertDetailSql, row, transaction);
                    if (n <= 0)
                    {
                        transaction.Rollback();
                        return 0;
                    }
                }

                transaction.Commit();
                return orderId;
            }
            catch
            {
                transaction.Rollback();
                return 0;
            }
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng (khách hàng, địa chỉ giao, nhân viên, shipper, trạng thái, thời gian).
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng mới (OrderID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy đơn hàng.
        /// </returns>
        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"UPDATE Orders
                            SET CustomerID = @CustomerID,
                                DeliveryProvince = @DeliveryProvince,
                                DeliveryAddress = @DeliveryAddress,
                                EmployeeID = @EmployeeID,
                                AcceptTime = @AcceptTime,
                                ShipperID = @ShipperID,
                                ShippedTime = @ShippedTime,
                                FinishedTime = @FinishedTime,
                                Status = @Status
                            WHERE OrderID = @OrderID";

                int result = await connection.ExecuteAsync(sql, data);
                return result > 0;
            }
        }

        /// <summary>
        /// Xóa đơn hàng khỏi bảng Orders.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy đơn hàng.
        /// </returns>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"DELETE FROM Orders WHERE OrderID = @orderID";
                int result = await connection.ExecuteAsync(sql, new { orderID });
                return result > 0;
            }
        }

        /// <summary>
        /// Lấy danh sách chi tiết mặt hàng của một đơn hàng (join kèm tên sản phẩm).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần lấy chi tiết.</param>
        /// <returns>
        /// Danh sách OrderDetailViewInfo;
        /// danh sách rỗng nếu đơn hàng không có mặt hàng.
        /// </returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT d.OrderID, d.ProductID, p.ProductName,
                                   d.Quantity, d.SalePrice
                            FROM OrderDetails d
                            JOIN Products p ON d.ProductID = p.ProductID
                            WHERE d.OrderID = @orderID";

                return (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { orderID })).ToList();
            }
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng cụ thể trong đơn hàng (join kèm tên sản phẩm).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng chứa mặt hàng.</param>
        /// <param name="productID">Mã mặt hàng cần lấy.</param>
        /// <returns>
        /// OrderDetailViewInfo nếu tìm thấy;
        /// null nếu không tồn tại trong đơn hàng này.
        /// </returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT d.OrderID, d.ProductID, p.ProductName,
                                   d.Quantity, d.SalePrice
                            FROM OrderDetails d
                            JOIN Products p ON d.ProductID = p.ProductID
                            WHERE d.OrderID = @orderID AND d.ProductID = @productID";

                return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql,
                    new { orderID, productID });
            }
        }

        /// <summary>
        /// Thêm một mặt hàng vào đơn hàng (insert vào bảng OrderDetails).
        /// </summary>
        /// <param name="data">Chi tiết mặt hàng cần thêm (OrderID, ProductID, Quantity, SalePrice).</param>
        /// <returns>
        /// True nếu thêm thành công;
        /// False nếu thất bại.
        /// </returns>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"INSERT INTO OrderDetails
                            (OrderID, ProductID, Quantity, SalePrice)
                            VALUES
                            (@OrderID, @ProductID, @Quantity, @SalePrice)";

                int result = await connection.ExecuteAsync(sql, data);
                return result > 0;
            }
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng.
        /// </summary>
        /// <param name="data">Chi tiết mặt hàng mới (OrderID và ProductID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy bản ghi.
        /// </returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"UPDATE OrderDetails
                            SET Quantity = @Quantity,
                                SalePrice = @SalePrice
                            WHERE OrderID = @OrderID AND ProductID = @ProductID";

                int result = await connection.ExecuteAsync(sql, data);
                return result > 0;
            }
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng (xóa khỏi bảng OrderDetails).
        /// </summary>
        /// <param name="orderID">Mã đơn hàng chứa mặt hàng cần xóa.</param>
        /// <param name="productID">Mã mặt hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy bản ghi.
        /// </returns>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"DELETE FROM OrderDetails
                            WHERE OrderID = @orderID AND ProductID = @productID";

                int result = await connection.ExecuteAsync(sql, new { orderID, productID });
                return result > 0;
            }
        }

        /// <summary>
        /// Tổng hợp dữ liệu dashboard quản trị: doanh thu hôm nay, số lượng đơn/khách/sản phẩm,
        /// danh sách đơn chờ xử lý, top 5 sản phẩm bán chạy và biểu đồ doanh thu 6 tháng gần nhất.
        /// </summary>
        /// <returns>
        /// SalesDashboardData chứa toàn bộ dữ liệu cần thiết cho trang dashboard.
        /// </returns>
        public async Task<SalesDashboardData> GetDashboardDataAsync()
        {
            using var connection = OpenConnection();
            var today = DateTime.Today;
            var completed = (int)OrderStatusEnum.Completed;

            var todayRevenue = await connection.ExecuteScalarAsync<decimal?>(@"
                SELECT ISNULL(SUM(CAST(d.Quantity AS DECIMAL(18,4)) * d.SalePrice), 0)
                FROM OrderDetails d
                INNER JOIN Orders o ON o.OrderID = d.OrderID
                WHERE o.Status = @completed
                  AND o.FinishedTime IS NOT NULL
                  AND CAST(o.FinishedTime AS DATE) = CAST(@today AS DATE)", new { today, completed }) ?? 0m;

            var totalOrders = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
            var customerCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers");
            var productCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");

            const string pendingSql = @"
                SELECT TOP 10 o.OrderID,
                       ISNULL(c.CustomerName, '') AS CustomerName,
                       o.OrderTime,
                       ISNULL(od.SumOfPrice, 0) AS SumOfPrice,
                       o.Status
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                OUTER APPLY (
                    SELECT SUM(CAST(d2.Quantity AS DECIMAL(18,4)) * d2.SalePrice) AS SumOfPrice
                    FROM OrderDetails d2
                    WHERE d2.OrderID = o.OrderID
                ) od
                WHERE o.Status IN (@newStatus, @acceptedStatus, @shippingStatus)
                ORDER BY o.OrderTime DESC";

            var pending = (await connection.QueryAsync<DashboardPendingOrderRow>(pendingSql, new
            {
                newStatus = (int)OrderStatusEnum.New,
                acceptedStatus = (int)OrderStatusEnum.Accepted,
                shippingStatus = (int)OrderStatusEnum.Shipping
            })).ToList();

            const string topSql = @"
                SELECT TOP 5 p.ProductName AS ProductName, CAST(SUM(d.Quantity) AS BIGINT) AS TotalQuantity
                FROM OrderDetails d
                INNER JOIN Products p ON p.ProductID = d.ProductID
                INNER JOIN Orders o ON o.OrderID = d.OrderID AND o.Status = @completed
                GROUP BY p.ProductName
                ORDER BY SUM(d.Quantity) DESC";

            var topProducts = (await connection.QueryAsync<DashboardTopProductRow>(topSql, new { completed })).ToList();

            var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            const string monthlySql = @"
                SELECT YEAR(o.FinishedTime) AS Year,
                       MONTH(o.FinishedTime) AS Month,
                       ISNULL(SUM(CAST(d.Quantity AS DECIMAL(18,4)) * d.SalePrice), 0) AS Revenue
                FROM Orders o
                INNER JOIN OrderDetails d ON d.OrderID = o.OrderID
                WHERE o.Status = @completed
                  AND o.FinishedTime IS NOT NULL
                  AND o.FinishedTime >= @monthStart
                GROUP BY YEAR(o.FinishedTime), MONTH(o.FinishedTime)
                ORDER BY YEAR(o.FinishedTime), MONTH(o.FinishedTime)";

            var monthlyRows = (await connection.QueryAsync<(int Year, int Month, decimal Revenue)>(
                monthlySql, new { monthStart, completed })).ToList();
            var monthlyDict = monthlyRows.ToDictionary(t => (t.Year, t.Month), t => t.Revenue);

            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();
            for (var d = monthStart; d <= new DateTime(today.Year, today.Month, 1); d = d.AddMonths(1))
            {
                chartLabels.Add($"Tháng {d.Month}/{d.Year}");
                var rev = monthlyDict.GetValueOrDefault((d.Year, d.Month));
                chartValues.Add(Math.Round(rev / 1_000_000m, 2));
            }

            return new SalesDashboardData
            {
                TodayRevenue = todayRevenue,
                TotalOrderCount = totalOrders,
                CustomerCount = customerCount,
                ProductCount = productCount,
                PendingOrders = pending,
                TopProducts = topProducts,
                MonthlyChartLabels = chartLabels,
                MonthlyChartValues = chartValues
            };
        }
    }
}
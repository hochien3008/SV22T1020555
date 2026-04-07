using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai ICustomerRepository trên SQL Server;
    /// thực hiện CRUD và kiểm tra email khách hàng bằng Dapper.
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tạo và trả về SqlConnection mới từ connection string.
        /// </summary>
        /// <returns>
        /// Một instance SqlConnection chưa mở kết nối.
        /// </returns>
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Thêm mới một khách hàng vào bảng Customers.
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng cần thêm.</param>
        /// <returns>
        /// CustomerID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = GetConnection();

            string sql = @"
                INSERT INTO Customers
                (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES
                (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng.
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng mới (CustomerID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = GetConnection();

            string sql = @"
                UPDATE Customers
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID
            ";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng khỏi bảng Customers.
        /// </summary>
        /// <param name="id">CustomerID cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM Customers WHERE CustomerID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin một khách hàng theo CustomerID.
        /// </summary>
        /// <param name="id">CustomerID cần lấy.</param>
        /// <returns>
        /// Customer nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM Customers WHERE CustomerID = @id";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra khách hàng có đang có đơn hàng tham chiếu không.
        /// </summary>
        /// <param name="id">CustomerID cần kiểm tra.</param>
        /// <returns>
        /// True nếu đang được sử dụng;
        /// False nếu có thể xóa an toàn.
        /// </returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE CustomerID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email khách hàng có trùng với khách hàng khác không.
        /// </summary>
        /// <param name="email">Email cần kiểm tra.</param>
        /// <param name="id">CustomerID cần loại trừ (0 nếu là khách hàng mới).</param>
        /// <returns>
        /// True nếu email hợp lệ (chưa có ai dùng);
        /// False nếu đã tồn tại.
        /// </returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = GetConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Customers
                WHERE Email = @email
                AND CustomerID <> @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });

            return count == 0;
        }

        /// <summary>
        /// Tìm kiếm khách hàng theo tên hoặc tên liên lạc và trả về danh sách phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// PagedResult chứa danh sách khách hàng và thông tin phân trang.
        /// </returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string countSql = @"
                SELECT COUNT(*)
                FROM Customers
                WHERE CustomerName LIKE @SearchValue
                   OR ContactName LIKE @SearchValue
            ";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, new
            {
                SearchValue = $"%{input.SearchValue}%"
            });

            string dataSql = @"
                SELECT *
                FROM Customers
                WHERE CustomerName LIKE @SearchValue
                   OR ContactName LIKE @SearchValue
                ORDER BY CustomerName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY
            ";

            var data = await connection.QueryAsync<Customer>(dataSql, new
            {
                SearchValue = $"%{input.SearchValue}%",
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            result.DataItems = data.ToList();

            return result;
        }
    }
}
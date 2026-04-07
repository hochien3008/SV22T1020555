using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai ISupplierRepository trên SQL Server; 
    /// Quản lý dữ liệu nhà cung cấp với các tác vụ CRUD cơ bản và kiểm tra email trùng.
    /// </summary>
    public class SupplierRepository : ISupplierRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository nhà cung cấp.
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL.</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tạo kết nối SQL Server mới.
        /// </summary>
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Thêm nhà cung cấp mới. mTrả về ID được tạo.
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp.</param>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = GetConnection();
            string sql = @"
                INSERT INTO Suppliers
                (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES
                (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp. Trả về True nếu thành công.
        /// </summary>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = GetConnection();
            string sql = @"
                UPDATE Suppliers
                SET SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID
            ";
            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa nhà cung cấp. Trả về True nếu thành công.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            string sql = "DELETE FROM Suppliers WHERE SupplierID = @id";
            return await connection.ExecuteAsync(sql, new { id }) > 0;
        }

        /// <summary>
        /// Lấy thông tin nhà cung cấp theo mã (ID).
        /// </summary>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = GetConnection();
            string sql = "SELECT * FROM Suppliers WHERE SupplierID = @id";
            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được sử dụng trong danh sách mặt hàng hay không.
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            string sql = "SELECT COUNT(*) FROM Products WHERE SupplierID = @id";
            return await connection.ExecuteScalarAsync<int>(sql, new { id }) > 0;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhà cung cấp phân trang.
        /// </summary>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();
            var result = new PagedResult<Supplier>() { Page = input.Page, PageSize = input.PageSize };

            string countSql = @"
                SELECT COUNT(*) FROM Suppliers
                WHERE SupplierName LIKE @SearchValue OR ContactName LIKE @SearchValue";
            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, new { SearchValue = $"%{input.SearchValue}%" });

            string dataSql = @"
                SELECT * FROM Suppliers
                WHERE SupplierName LIKE @SearchValue OR ContactName LIKE @SearchValue
                ORDER BY SupplierName OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var data = await connection.QueryAsync<Supplier>(dataSql, new
            {
                SearchValue = $"%{input.SearchValue}%",
                Offset = input.Offset,
                PageSize = input.PageSize
            });
            result.DataItems = data.ToList();
            return result;
        }

        /// <summary>
        /// Kiểm tra xem email có đang được dùng cho nhà cung cấp khác hay không. 
        /// Dùng cho việc validate email trên form Edit/Create.
        /// </summary>
        /// <param name="email">Email cần kiểm tra.</param>
        /// <param name="supplierID">Mã NCC (0 nếu tạo mới, >0 nếu cập nhật).</param>
        /// <returns>True nếu email hợp lệ (không trùng).</returns>
        public async Task<bool> ValidateEmailAsync(string email, int supplierID)
        {
            using var connection = GetConnection();
            string sql = @"
                SELECT COUNT(*) FROM Suppliers
                WHERE Email = @email AND (@supplierID = 0 OR SupplierID <> @supplierID)";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, supplierID });
            return count == 0;
        }
    }
}
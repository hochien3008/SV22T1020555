using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai IGenericRepository&lt;Shipper&gt; trên SQL Server;
    /// thực hiện CRUD người giao hàng bằng Dapper.
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public ShipperRepository(string connectionString)
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
        /// Thêm mới một người giao hàng vào bảng Shippers.
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần thêm.</param>
        /// <returns>
        /// ShipperID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = GetConnection();

            string sql = @"
                INSERT INTO Shippers(ShipperName, Phone)
                VALUES(@ShipperName, @Phone);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng.
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng mới (ShipperID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = GetConnection();

            string sql = @"
                UPDATE Shippers
                SET ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID
            ";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa người giao hàng khỏi bảng Shippers.
        /// </summary>
        /// <param name="id">ShipperID cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM Shippers WHERE ShipperID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin một người giao hàng theo ShipperID.
        /// </summary>
        /// <param name="id">ShipperID cần lấy.</param>
        /// <returns>
        /// Shipper nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM Shippers WHERE ShipperID = @id";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được gán cho đơn hàng nào không.
        /// </summary>
        /// <param name="id">ShipperID cần kiểm tra.</param>
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
                WHERE ShipperID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Tìm kiếm người giao hàng theo tên và trả về danh sách phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// PagedResult chứa danh sách người giao hàng và thông tin phân trang.
        /// </returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string countSql = @"
                SELECT COUNT(*)
                FROM Shippers
                WHERE ShipperName LIKE @SearchValue
            ";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, new
            {
                SearchValue = $"%{input.SearchValue}%"
            });

            string dataSql = @"
                SELECT *
                FROM Shippers
                WHERE ShipperName LIKE @SearchValue
                ORDER BY ShipperName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY
            ";

            var data = await connection.QueryAsync<Shipper>(dataSql, new
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
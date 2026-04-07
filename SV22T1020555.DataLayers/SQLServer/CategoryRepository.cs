using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Catalog;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai thực thi IGenericRepository&lt;Category&gt; trên SQL Server;
    /// thực hiện CRUD loại hàng bằng Dapper.
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public CategoryRepository(string connectionString)
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
        /// Thêm mới một loại hàng vào bảng Categories.
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần thêm.</param>
        /// <returns>
        /// CategoryID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = GetConnection();

            string sql = @"
                INSERT INTO Categories(CategoryName, Description)
                VALUES(@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật tên và mô tả của loại hàng.
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng mới (CategoryID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = GetConnection();

            string sql = @"
                UPDATE Categories
                SET CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID
            ";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa loại hàng khỏi bảng Categories.
        /// </summary>
        /// <param name="id">CategoryID cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM Categories WHERE CategoryID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin một loại hàng theo CategoryID.
        /// </summary>
        /// <param name="id">CategoryID cần lấy.</param>
        /// <returns>
        /// Category nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM Categories WHERE CategoryID = @id";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra loại hàng có đang được gán cho mặt hàng nào không.
        /// </summary>
        /// <param name="id">CategoryID cần kiểm tra.</param>
        /// <returns>
        /// True nếu đang được sử dụng (không thể xóa);
        /// False nếu có thể xóa an toàn.
        /// </returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"
                SELECT COUNT(*)
                FROM Products
                WHERE CategoryID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Tìm kiếm loại hàng theo tên và trả về danh sách phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// PagedResult chứa danh sách loại hàng và thông tin phân trang.
        /// </returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string countSql = @"
                SELECT COUNT(*)
                FROM Categories
                WHERE CategoryName LIKE @SearchValue
            ";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, new
            {
                SearchValue = $"%{input.SearchValue}%"
            });

            string dataSql = @"
                SELECT *
                FROM Categories
                WHERE CategoryName LIKE @SearchValue
                ORDER BY CategoryName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY
            ";

            var data = await connection.QueryAsync<Category>(dataSql, new
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
using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.DataDictionary;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai IDataDictionaryRepository&lt;Province&gt; trên SQL Server;
    /// lấy danh sách tỉnh/thành phố (read-only) bằng Dapper.
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public ProvinceRepository(string connectionString)
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
        /// Lấy danh sách toàn bộ tỉnh/thành phố, sắp xếp theo tên.
        /// </summary>
        /// <returns>
        /// Danh sách Province sắp xếp theo ProvinceName.
        /// </returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = GetConnection();

            string sql = @"
                SELECT ProvinceName
                FROM Provinces
                ORDER BY ProvinceName
            ";

            var data = await connection.QueryAsync<Province>(sql);

            return data.ToList();
        }
    }
}
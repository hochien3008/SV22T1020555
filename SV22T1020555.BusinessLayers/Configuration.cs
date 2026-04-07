using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020555.BusinessLayers
{
    /// <summary>
    /// Giữ cấu hình khởi tạo cho BusinessLayers,
    /// bao gồm chuỗi kết nối CSDL (ConnectionString).
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString = "";
        /// <summary>
        /// Khởi tạo các cấu hình cho BusinessLayers
        /// (hàm này được gọi một lần trước khi chạy ứng dụng tại Program.cs).
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới CSDL SQL Server.</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// Lấy chuỗi tham số kết nối đến CSDL đã được khởi tạo.
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}

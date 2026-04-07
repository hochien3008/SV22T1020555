using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Partner;
using SV22T1020555.Models.Security;
using System.Data;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai IUserAccountRepository trên SQL Server. 
    /// Quản lý xác thực và tài khoản cho cả Nhân viên và Khách hàng.
    /// </summary>
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public UserAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        /// <summary>
        /// Giải thích lỗi SQL thành thông báo tiếng Việt. 
        /// Logic này nằm ở tầng Data vì nó phụ thuộc vào SqlException.
        /// </summary>
        private string DescribeSqlError(SqlException ex)
        {
            return ex.Number switch
            {
                208 => "Không tìm thấy bảng trên database (kiểm tra tên bảng Customers và connection string).",
                207 => "Bảng Customers không khớp code (thiếu/sai tên cột Email, Password, …).",
                2627 or 2601 => "Email này đã được dùng cho một khách hàng khác.",
                515 => "Không thể lưu: có cột NOT NULL chưa được gán (ví dụ Customers.Password).",
                547 => "Vi phạm ràng buộc khóa ngoại hoặc check trên database. Chi tiết: " + ex.Message,
                8152 => "Chuỗi quá dài so với cột SQL.",
                _ => $"Lỗi hệ thống dữ liệu ({ex.Number}): {ex.Message}",
            };
        }

        public async Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
        {
            using var connection = GetConnection();
            var sql = @"
                SELECT TOP (1)
                    CAST(EmployeeID AS varchar(20)) AS UserId,
                    Email AS UserName,
                    FullName AS DisplayName,
                    Email,
                    Photo,
                    RoleNames
                FROM Employees
                WHERE Email = @userName
                  AND [Password] = @password
                  AND IsWorking = 1;
            ";
            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
        }

        public async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password, string customerRole)
        {
            using var connection = GetConnection();
            const string sql = @"
                SELECT TOP (1)
                    CAST(c.CustomerID AS varchar(20)) AS UserId,
                    LTRIM(RTRIM(c.Email)) AS UserName,
                    LTRIM(RTRIM(c.CustomerName)) AS DisplayName,
                    LTRIM(RTRIM(c.Email)) AS Email,
                    CAST(N'' AS nvarchar(500)) AS Photo,
                    @shopRole AS RoleNames
                FROM Customers c
                WHERE LTRIM(RTRIM(c.Email)) = @userName
                  AND c.[Password] = @password
                  AND ISNULL(c.IsLocked, 0) = 0;
            ";
            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, 
                new { userName, password, shopRole = customerRole });
        }

        public async Task<int> RegisterCustomerAsync(Customer data, string passwordHash)
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
            await using var tran = await connection.BeginTransactionAsync();
            try
            {
                const string sql = @"
                    INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, [Password], IsLocked)
                    VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var id = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    data.CustomerName,
                    data.ContactName,
                    data.Province,
                    data.Address,
                    data.Phone,
                    data.Email,
                    Password = passwordHash,
                    data.IsLocked
                }, tran);

                await tran.CommitAsync();
                return id;
            }
            catch (SqlException ex)
            {
                await tran.RollbackAsync();
                // Ném ngoại lệ với thông báo đã được giải thích để tầng Business nhận được chuỗi string sạch
                throw new Exception(DescribeSqlError(ex));
            }
            catch (Exception)
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ChangeCustomerPasswordAsync(string userName, string oldPassword, string newPassword)
        {
            using var connection = GetConnection();
            const string sql = @"
                UPDATE Customers
                SET [Password] = @newPassword
                WHERE LTRIM(RTRIM(Email)) = @userName
                  AND [Password] = @oldPassword
                  AND ISNULL(IsLocked, 0) = 0;";

            return await connection.ExecuteAsync(sql, new { userName, oldPassword, newPassword }) > 0;
        }
    }
}

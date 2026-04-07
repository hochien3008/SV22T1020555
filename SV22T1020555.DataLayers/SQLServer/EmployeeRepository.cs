using Dapper;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.HR;
using System.Data;
using Microsoft.Data.SqlClient;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai IEmployeeRepository trên SQL Server;
    /// CRUD nhân viên, quản lý role, đổi mật khẩu bằng Dapper.
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public EmployeeRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Mở và trả về IDbConnection đã được kết nối.
        /// </summary>
        /// <returns>
        /// IDbConnection đã mở; nhớ dispose sau khi dùng.
        /// </returns>
        private IDbConnection OpenConnection()
        {
            IDbConnection connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Thêm mới một nhân viên vào bảng Employees.
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên cần thêm.</param>
        /// <returns>
        /// EmployeeID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"INSERT INTO Employees
                            (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                            VALUES
                            (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                            SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên (không bao gồm mật khẩu).
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên mới (EmployeeID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"UPDATE Employees
                            SET FullName = @FullName,
                                BirthDate = @BirthDate,
                                Address = @Address,
                                Phone = @Phone,
                                Email = @Email,
                                Photo = @Photo,
                                IsWorking = @IsWorking
                            WHERE EmployeeID = @EmployeeID";

                int result = await connection.ExecuteAsync(sql, data);
                return result > 0;
            }
        }

        /// <summary>
        /// Xóa nhân viên khỏi bảng Employees.
        /// </summary>
        /// <param name="id">EmployeeID cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"DELETE FROM Employees WHERE EmployeeID = @id";
                int result = await connection.ExecuteAsync(sql, new { id });
                return result > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin một nhân viên theo EmployeeID.
        /// </summary>
        /// <param name="id">EmployeeID cần lấy.</param>
        /// <returns>
        /// Employee nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT * FROM Employees WHERE EmployeeID = @id";
                return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { id });
            }
        }

        /// <summary>
        /// Tìm kiếm nhân viên theo tên và trả về danh sách phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// PagedResult chứa danh sách nhân viên và thông tin phân trang.
        /// </returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using (var connection = OpenConnection())
            {
                int rowCount;
                List<Employee> data;

                var sql = @"SELECT COUNT(*) 
                            FROM Employees
                            WHERE FullName LIKE @searchValue;

                            SELECT *
                            FROM Employees
                            WHERE FullName LIKE @searchValue
                            ORDER BY FullName
                            OFFSET (@page - 1) * @pageSize ROWS
                            FETCH NEXT @pageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    page = input.Page,
                    pageSize = input.PageSize,
                    searchValue = "%" + input.SearchValue + "%"
                }))
                {
                    rowCount = multi.Read<int>().Single();
                    data = multi.Read<Employee>().ToList();
                }

                return new PagedResult<Employee>()
                {
                    Page = input.Page,
                    PageSize = input.PageSize,
                    RowCount = rowCount,
                    DataItems = data
                };
            }
        }

        /// <summary>
        /// Kiểm tra nhân viên có đang được gán cho đơn hàng nào không.
        /// </summary>
        /// <param name="id">EmployeeID cần kiểm tra.</param>
        /// <returns>
        /// True nếu đang được sử dụng;
        /// False nếu có thể xóa an toàn.
        /// </returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT COUNT(*) FROM Orders WHERE EmployeeID = @id";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
                return count > 0;
            }
        }

        /// <summary>
        /// Kiểm tra email nhân viên có trùng với nhân viên khác không.
        /// </summary>
        /// <param name="email">Email cần kiểm tra.</param>
        /// <param name="id">EmployeeID cần loại trừ (0 nếu là nhân viên mới).</param>
        /// <returns>
        /// True nếu email hợp lệ;
        /// False nếu đã có người sử dụng.
        /// </returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT COUNT(*) 
                            FROM Employees
                            WHERE Email = @email AND EmployeeID <> @id";

                int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });
                return count == 0;
            }
        }

        /// <summary>
        /// Lấy chuỗi tên role hiện tại của nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần lấy role.</param>
        /// <returns>
        /// Chuỗi role (ví dụ: "admin,sales");
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<string?> GetRoleNamesAsync(int employeeID)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"SELECT RoleNames FROM Employees WHERE EmployeeID = @employeeID";
                return await connection.ExecuteScalarAsync<string?>(sql, new { employeeID });
            }
        }

        /// <summary>
        /// Cập nhật chuỗi tên role của nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần cập nhật role.</param>
        /// <param name="roleNames">Chuỗi role mới phân cách bằng dấu phẩy.</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy.
        /// </returns>
        public async Task<bool> UpdateRoleNamesAsync(int employeeID, string roleNames)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"UPDATE Employees SET RoleNames = @roleNames WHERE EmployeeID = @employeeID";
                int rows = await connection.ExecuteAsync(sql, new { employeeID, roleNames });
                return rows > 0;
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên (xác thực mật khẩu cũ trước khi cập nhật).
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần đổi mật khẩu.</param>
        /// <param name="oldPassword">Mật khẩu cũ đã hash MD5 (để xác thực danh tính).</param>
        /// <param name="newPassword">Mật khẩu mới đã hash MD5.</param>
        /// <returns>
        /// True nếu đổi thành công;
        /// False nếu mật khẩu cũ không đúng.
        /// </returns>
        public async Task<bool> ChangePasswordAsync(int employeeID, string oldPassword, string newPassword)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"
                    UPDATE Employees
                    SET [Password] = @newPassword
                    WHERE EmployeeID = @employeeID
                      AND [Password] = @oldPassword;
                ";
                int rows = await connection.ExecuteAsync(sql, new { employeeID, oldPassword, newPassword });
                return rows > 0;
            }
        }

        /// <summary>
        /// Reset mật khẩu nhân viên không cần xác thực mật khẩu cũ (admin reset).
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần reset mật khẩu.</param>
        /// <param name="newPassword">Mật khẩu mới đã hash MD5.</param>
        /// <returns>
        /// True nếu reset thành công;
        /// False nếu không tìm thấy nhân viên.
        /// </returns>
        public async Task<bool> SetPasswordAsync(int employeeID, string newPassword)
        {
            using (var connection = OpenConnection())
            {
                var sql = @"UPDATE Employees SET [Password] = @newPassword WHERE EmployeeID = @employeeID";
                int rows = await connection.ExecuteAsync(sql, new { employeeID, newPassword });
                return rows > 0;
            }
        }
    }
}
using SV22T1020555.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (chưa ai dùng);
        /// False nếu email đã tồn tại.
        /// </returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);

        /// <summary>
        /// Lấy danh sách role của nhân viên (dạng chuỗi phân cách bằng dấu phẩy, ví dụ: "admin,sales").
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần lấy role.</param>
        /// <returns>
        /// Chuỗi tên role; 
        /// null nếu nhân viên không tồn tại hoặc chưa có role.
        /// </returns>
        Task<string?> GetRoleNamesAsync(int employeeID);

        /// <summary>
        /// Cập nhật danh sách role của nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần cập nhật role.</param>
        /// <param name="roleNames">Chuỗi tên role mới, phân cách bằng dấu phẩy (ví dụ: "admin,sales").</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy nhân viên.
        /// </returns>
        Task<bool> UpdateRoleNamesAsync(int employeeID, string roleNames);

        /// <summary>
        /// Đổi mật khẩu nhân viên (yêu cầu xác thực đúng mật khẩu cũ - đã được hash MD5).
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần đổi mật khẩu.</param>
        /// <param name="oldPassword">Mật khẩu cũ đã hash MD5.</param>
        /// <param name="newPassword">Mật khẩu mới đã hash MD5.</param>
        /// <returns>
        /// True nếu mật khẩu cũ khớp và đổi thành công;
        /// False nếu xác thực thất bại.
        /// </returns>
        Task<bool> ChangePasswordAsync(int employeeID, string oldPassword, string newPassword);

        /// <summary>
        /// Thiết lập lại mật khẩu nhân viên không cần mật khẩu cũ (admin reset - đã được hash MD5).
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần reset mật khẩu.</param>
        /// <param name="newPassword">Mật khẩu mới đã hash MD5.</param>
        /// <returns>
        /// True nếu reset thành công;
        /// False nếu không tìm thấy nhân viên.
        /// </returns>
        Task<bool> SetPasswordAsync(int employeeID, string newPassword);
    }
}

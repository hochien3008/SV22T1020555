using SV22T1020555.Models.Partner;
using SV22T1020555.Models.Security;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu liên quan đến tài khoản (Xác thực, Đăng ký, Đổi mật khẩu)
    /// </summary>
    public interface IUserAccountRepository
    {
        /// <summary>
        /// Xác thực tài khoản nhân viên (Admin).
        /// </summary>
        Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password);

        /// <summary>
        /// Xác thực tài khoản khách hàng (Shop).
        /// </summary>
        Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password, string customerRole);

        /// <summary>
        /// Đăng ký tài khoản khách hàng mới.
        /// </summary>
        Task<int> RegisterCustomerAsync(Customer data, string passwordHash);

        /// <summary>
        /// Đổi mật khẩu cho khách hàng.
        /// </summary>
        Task<bool> ChangeCustomerPasswordAsync(string userName, string oldPassword, string newPassword);
    }
}

using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.DataLayers.SQLServer;
using SV22T1020555.Models.Partner;
using SV22T1020555.Models.Security;

namespace SV22T1020555.BusinessLayers
{
    /// <summary>
    /// Xử lý xác thực (login) và đăng ký tài khoản của nhân viên (Admin) và khách hàng (Shop);
    /// quản lý đổi/reset mật khẩu khách thông qua Repository.
    /// </summary>
    public static class SecurityDataSerer
    {
        private static readonly IUserAccountRepository accountDB;

        static SecurityDataSerer()
        {
            accountDB = new UserAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>Vai trò cookie cho khách Shop (không lưu trong Customers; chỉ dùng khi SignIn).</summary>
        public const string ShopCustomerRole = "customer";

        /// <summary>
        /// Xác thực nhân viên dựa trên Email và mật khẩu đã hash (MD5).
        /// </summary>
        public static async Task<UserAccount?> EmployeeAuthorizeAsync(string userName, string password)
        {
            return await accountDB.AuthorizeEmployeeAsync(userName, password);
        }

        /// <summary>
        /// Xác thực khách hàng qua Email và mật khẩu MD5.
        /// </summary>
        public static async Task<UserAccount?> CustomerAuthorizeAsync(string userName, string passwordMd5)
        {
            userName = (userName ?? "").Trim();
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passwordMd5))
                return null;

            var acc = await accountDB.AuthorizeCustomerAsync(userName, passwordMd5, ShopCustomerRole);
            if (acc != null && CustomerProfileHelper.IsPendingDisplayName(acc.DisplayName))
                acc.DisplayName = acc.Email ?? acc.UserName;
            return acc;
        }

        /// <summary>
        /// Đăng ký tài khoản khách hàng mới.
        /// </summary>
        public static async Task<(bool ok, int customerId, string? error)> RegisterCustomerWithAccountAsync(string email, string plainPassword)
        {
            email = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email))
                return (false, 0, "Vui lòng nhập email.");
            if (string.IsNullOrWhiteSpace(plainPassword) || plainPassword.Length < 6)
                return (false, 0, "Mật khẩu tối thiểu 6 ký tự.");

            if (!await PartnerDataService.ValidatelCustomerEmailAsync(email, 0))
                return (false, 0, "Email đã được sử dụng.");

            var pending = CustomerProfileHelper.PendingCustomerDisplayName;
            var customer = new Customer
            {
                CustomerName = pending,
                ContactName = pending,
                Province = null,
                Address = null,
                Phone = null,
                Email = email,
                IsLocked = false,
            };

            try
            {
                var hash = HashHelper.HashMD5(plainPassword);
                var customerId = await accountDB.RegisterCustomerAsync(customer, hash);
                if (customerId <= 0)
                    return (false, 0, "Không tạo được khách hàng.");

                return (true, customerId, null);
            }
            catch (Exception ex)
            {
                // Nhận thông báo lỗi đã được Data Layer biên dịch sang tiếng Việt
                return (false, 0, ex.Message);
            }
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản khách hàng.
        /// </summary>
        public static async Task<bool> ChangeCustomerPasswordAsync(string userName, string oldPasswordMd5, string newPasswordMd5)
        {
            userName = (userName ?? "").Trim();
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(oldPasswordMd5) || string.IsNullOrEmpty(newPasswordMd5))
                return false;

            return await accountDB.ChangeCustomerPasswordAsync(userName, oldPasswordMd5, newPasswordMd5);
        }
    }
}

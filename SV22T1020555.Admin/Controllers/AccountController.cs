using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Admin.Models;
using SV22T1020555.Models.Security;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý tài khoản quản trị: đăng nhập, đăng xuất, đổi mật khẩu và xử lý truy cập bị từ chối.
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Hiển thị form đổi mật khẩu của nhân viên đang đăng nhập.
        /// </summary>
        /// <returns>View form đổi mật khẩu với model mới rỗng.</returns>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new AccountChangePasswordViewModel());
        }

        /// <summary>
        /// Xử lý đổi mật khẩu của nhân viên đang đăng nhập; sau khi thành công buộc đăng xuất và đăng nhập lại.
        /// </summary>
        /// <param name="model">Dữ liệu form đổi mật khẩu (mật khẩu cũ, mới, xác nhận).</param>
        /// <returns>
        /// Chuyển sang trang Login sau khi đổi thành công;
        /// hiển thị lại form kèm lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(AccountChangePasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.OldPassword))
                ModelState.AddModelError(nameof(model.OldPassword), "Vui lòng nhập mật khẩu cũ");
            if (string.IsNullOrWhiteSpace(model.NewPassword))
                ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới");
            if (model.NewPassword != model.ConfirmPassword)
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Xác nhận mật khẩu không khớp");

            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserId) || !int.TryParse(userData.UserId, out int employeeId))
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
                return View(model);

            var oldHash = CryptHelper.HashMD5(model.OldPassword);
            var newHash = CryptHelper.HashMD5(model.NewPassword);
            bool ok = await HRDataService.ChangeEmployeePasswordAsync(employeeId, oldHash, newHash);
            if (!ok)
            {
                ModelState.AddModelError("Error", "Mật khẩu cũ không đúng hoặc tài khoản không tồn tại");
                return View(model);
            }

            // Đổi mật khẩu thành công -> đăng xuất để đăng nhập lại
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            TempData["Message"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
        /// <summary>
        /// Hiển thị form đăng nhập quản trị.
        /// </summary>
        /// <returns>
        /// View form đăng nhập.
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() 
        {
            return View();
        }
        /// <summary>
        /// Xử lý đăng nhập nhân viên: xác thực email + mật khẩu, tạo cookie phân quyền theo RoleNames.
        /// </summary>
        /// <param name="username">Email đăng nhập của nhân viên.</param>
        /// <param name="password">Mật khẩu thô (sẽ được hash MD5 trước khi xác thực).</param>
        /// <returns>
        /// Chuyển về trang chủ Admin nếu đăng nhập thành công;
        /// hiển thị lại form đăng nhập kèm lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password) 
        {
            ViewBag.Username = username;

            if(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Hãy nhập đủ tên và mật khẩu");
                return View();
            }
            password = CryptHelper.HashMD5(password);

            var userAccount = await SecurityDataSerer.EmployeeAuthorizeAsync(username, password);
            if(userAccount == null)
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại");
                return View();
            }

            //Dữ liệu sẽ dùng để "ghi" vào giấy chứng nhận (principal)
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName= userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = (userAccount.RoleNames ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList()
            };

            //Thiết lập phiên đăng nhập (cấp giấy chứng nhận)
            await HttpContext.SignInAsync
                (
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal()
                );
            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// Xóa cookie và session; đăng xuất khỏi hệ thống quản trị.
        /// </summary>
        /// <returns>
        /// Chuyển về trang đăng nhập sau khi đăng xuất.
        /// </returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
        /// <summary>
        /// Hiển thị trang thông báo khi nhân viên không có quyền truy cập chức năng.
        /// </summary>
        /// <returns>
        /// View trang thông báo truy cập bị từ chối.
        /// </returns>
        public IActionResult AccessDenied() 
        { 
            return View(); 
        }
    }
}

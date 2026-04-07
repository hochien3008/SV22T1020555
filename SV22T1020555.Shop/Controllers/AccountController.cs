using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Shop;
using SV22T1020555.Models.Partner;
using SV22T1020555.Shop.Models;

namespace SV22T1020555.Shop.Controllers
{
    /// <summary>
    /// Đăng ký, đăng nhập/đăng xuất khách hàng; quản lý hồ sơ và đổi mật khẩu.
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>Hiển thị form đăng ký tài khoản khách.</summary>
        /// <returns>View form đăng ký với model mới rỗng.</returns>
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        /// <summary>Xử lý đăng ký: tạo tài khoản Customer gắn với email, sau đó chuyển sang trang đăng nhập.</summary>
        /// <param name="model">Dữ liệu form đăng ký (email, mật khẩu).</param>
        /// <returns>
        /// Chuyển sang trang Login nếu đăng ký thành công;
        /// hiển thị lại form kèm thông báo lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (ok, _, err) = await SecurityDataSerer.RegisterCustomerWithAccountAsync(model.Email, model.Password);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, err ?? "Đăng ký không thành công.");
                return View(model);
            }

            TempData["Message"] = "Đăng ký thành công, hãy đăng nhập để bắt đầu mua sắm!";
            return RedirectToAction(nameof(Login));
        }

        /// <summary>Hiển thị form đăng nhập.</summary>
        /// <param name="returnUrl">URL nội bộ để chuyển hướng sau khi đăng nhập thành công.</param>
        /// <returns>View form đăng nhập với model mới rỗng.</returns>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>Xử lý đăng nhập cookie, gắn vai trò khách hàng vào Claims.</summary>
        /// <param name="model">Dữ liệu form đăng nhập (email, mật khẩu, nhớ đăng nhập).</param>
        /// <param name="returnUrl">URL nội bộ để chuyển hướng sau khi đăng nhập, nếu có.</param>
        /// <returns>
        /// Chuyển về returnUrl hoặc trang chủ nếu đăng nhập thành công;
        /// hiển thị lại form kèm lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var hash = HashHelper.HashMD5(model.Password);
            var account = await SecurityDataSerer.CustomerAuthorizeAsync(model.Email, hash);
            if (account == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var user = new ShopUserData
            {
                UserId = account.UserId,
                UserName = account.UserName,
                DisplayName = account.DisplayName,
                Email = account.Email,
                Photo = account.Photo ?? "",
                Roles = new List<string> { ShopRoles.Customer }
            };

            var props = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                user.CreatePrincipal(),
                props);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        /// <summary>Xóa cookie xác thực và đăng xuất khỏi hệ thống.</summary>
        /// <returns>Chuyển về trang chủ sau khi đăng xuất.</returns>
        [Authorize(Roles = ShopRoles.Customer)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        /// <summary>Hiển thị form hồ sơ khách để chỉnh sửa (tên, liên hệ, địa chỉ giao hàng).</summary>
        /// <param name="returnUrl">URL để quay lại sau khi lưu hồ sơ, nếu có.</param>
        /// <returns>View form hồ sơ với dữ liệu hiện tại của khách; chuyển sang Login nếu chưa đăng nhập.</returns>
        [Authorize(Roles = ShopRoles.Customer)]
        [HttpGet]
        public async Task<IActionResult> Profile(string? returnUrl = null)
        {
            var id = User.GetCustomerId();
            if (id == null)
                return RedirectToAction(nameof(Login));
            var c = await PartnerDataService.GetCustomerAsync(id.Value);
            if (c == null)
                return RedirectToAction(nameof(Logout));
            ViewBag.ProfileIncomplete = !CustomerProfileHelper.IsCompleteForCheckout(c);
            c.CustomerName = CustomerProfileHelper.ForEditableDisplay(c.CustomerName);
            c.ContactName = CustomerProfileHelper.ForEditableDisplay(c.ContactName);
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            ViewBag.ReturnUrl = returnUrl;
            return View(c);
        }

        /// <summary>
        /// Lưu hồ sơ khách; đồng bộ tên hiển thị vào cookie đăng nhập sau khi cập nhật thành công.
        /// </summary>
        /// <param name="model">Dữ liệu hồ sơ khách cần lưu (tên, điện thoại, tỉnh/thành, địa chỉ).</param>
        /// <param name="returnUrl">URL để quay lại sau khi lưu thành công, nếu có.</param>
        /// <returns>
        /// Chuyển về returnUrl hoặc lại trang Profile nếu lưu thành công;
        /// hiển thị lại form kèm lỗi nếu thất bại.
        /// </returns>
        [Authorize(Roles = ShopRoles.Customer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer model, string? returnUrl = null)
        {
            var id = User.GetCustomerId();
            if (id == null || model.CustomerID != id.Value)
            {
                ModelState.AddModelError(string.Empty, "Phiên không hợp lệ.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var existing = await PartnerDataService.GetCustomerAsync(id.Value);
            if (existing == null)
                return RedirectToAction(nameof(Logout));

            model.Email = existing.Email;
            model.CustomerID = id.Value;
            model.IsLocked = existing.IsLocked;

            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError(nameof(model.CustomerName), "Bắt buộc");

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.ProfileIncomplete = !CustomerProfileHelper.IsCompleteForCheckout(model);
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.ContactName))
                model.ContactName = model.CustomerName;

            if (!await PartnerDataService.UpdateCustomerAsync(model))
            {
                ModelState.AddModelError(string.Empty, "Không lưu được. Kiểm tra dữ liệu.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.ProfileIncomplete = !CustomerProfileHelper.IsCompleteForCheckout(model);
                return View(model);
            }

            var sessionUser = User.GetShopUser();
            if (sessionUser != null)
            {
                sessionUser.DisplayName = model.CustomerName;
                var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var props = authResult.Properties ?? new AuthenticationProperties();
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    sessionUser.CreatePrincipal(),
                    props);
            }

            TempData["Message"] = "Đã cập nhật thông tin.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>Hiển thị form đổi mật khẩu (yêu cầu mật khẩu hiện tại).</summary>
        /// <returns>View form đổi mật khẩu với model mới rỗng.</returns>
        [Authorize(Roles = ShopRoles.Customer)]
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordShopViewModel());

        /// <summary>
        /// Xử lý đổi mật khẩu; sau khi thành công buộc đăng xuất và chuyển sang Login.
        /// </summary>
        /// <param name="model">Dữ liệu form đổi mật khẩu (mật khẩu hiện tại, mới, xác nhận).</param>
        /// <returns>
        /// Chuyển sang trang Login sau khi đổi thành công;
        /// hiển thị lại form kèm lỗi nếu thất bại.
        /// </returns>
        [Authorize(Roles = ShopRoles.Customer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordShopViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var u = User.GetShopUser();
            if (u?.UserName == null)
                return RedirectToAction(nameof(Login));

            var ok = await SecurityDataSerer.ChangeCustomerPasswordAsync(
                u.UserName,
                HashHelper.HashMD5(model.OldPassword),
                HashHelper.HashMD5(model.NewPassword));

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Message"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
    }
}

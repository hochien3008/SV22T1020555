using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020555.Shop
{
    /// <summary>Tên vai trò dùng trong cookie shop (khách đã đăng nhập).</summary>
    public class ShopRoles
    {
        public const string Customer = "customer";
    }

    /// <summary>Dữ liệu người dùng shop đưa vào Claims khi đăng nhập.</summary>
    public class ShopUserData
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? Photo { get; set; }
        public List<string>? Roles { get; set; }

        private List<Claim> Claims
        {
            get
            {
                var claims = new List<Claim>
                {
                    new Claim(nameof(UserId), UserId ?? ""),
                    new Claim(nameof(UserName), UserName ?? ""),
                    new Claim(nameof(DisplayName), DisplayName ?? ""),
                    new Claim(nameof(Email), Email ?? ""),
                    new Claim(nameof(Photo), Photo ?? "")
                };
                if (Roles != null)
                    foreach (var r in Roles)
                        claims.Add(new Claim(ClaimTypes.Role, r));
                return claims;
            }
        }

        /// <summary>Tạo principal cookie authentication từ các claim hiện có.</summary>
        public ClaimsPrincipal CreatePrincipal()
        {
            var id = new ClaimsIdentity(Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(id);
        }
    }

    /// <summary>Đọc ShopUserData và mã khách (CustomerID) từ principal.</summary>
    public static class ShopUserExtensions
    {
        /// <summary>Lấy thông tin shop từ claims; null nếu chưa đăng nhập.</summary>
        public static ShopUserData? GetShopUser(this ClaimsPrincipal principal)
        {
            try
            {
                if (principal?.Identity?.IsAuthenticated != true)
                    return null;
                return new ShopUserData
                {
                    UserId = principal.FindFirstValue(nameof(ShopUserData.UserId)),
                    UserName = principal.FindFirstValue(nameof(ShopUserData.UserName)),
                    DisplayName = principal.FindFirstValue(nameof(ShopUserData.DisplayName)),
                    Email = principal.FindFirstValue(nameof(ShopUserData.Email)),
                    Photo = principal.FindFirstValue(nameof(ShopUserData.Photo)),
                    Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary><c>UserId</c> claim parse thành số khách hàng; null nếu không hợp lệ.</summary>
        public static int? GetCustomerId(this ClaimsPrincipal principal)
        {
            var uid = principal.GetShopUser()?.UserId;
            return int.TryParse(uid, out var id) && id > 0 ? id : null;
        }
    }
}

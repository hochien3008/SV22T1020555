using System.Security.Cryptography;
using System.Text;

namespace SV22T1020555.BusinessLayers
{
    /// <summary>
    /// Tiện ích hàm băm (hash) mật khẩu cho hệ thống.
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Biến đầu vào thành chuỗi hash MD5 dạng hex thường (32 ký tự).
        /// </summary>
        /// <param name="input">Chuỗi cần hash. Nếu null sẽ trả về chuỗi rỗng.</param>
        /// <returns>
        /// Chuỗi hex MD5 32 ký tự; rỗng nếu <paramref name="input"/> là null.
        /// </returns>
        public static string HashMD5(string input)
        {
            if (input == null) return "";
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}

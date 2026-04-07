using Newtonsoft.Json;

namespace SV22T1020555.Shop
{
    /// <summary>
    /// Truy cập HttpContext, môi trường host và cấu hình; lưu session dạng JSON.
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>Gọi một lần lúc khởi động ứng dụng (Program.cs).</summary>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;
        public static IWebHostEnvironment? WebHostEnvironment => _webHostEnvironment;
        public static IConfiguration? Configuration => _configuration;

        /// <summary>Serialize <paramref name="value"/> (JSON) và ghi vào session.</summary>
        public static void SetSessionData(string key, object value)
        {
            try
            {
                var s = JsonConvert.SerializeObject(value);
                if (!string.IsNullOrEmpty(s))
                    _httpContextAccessor?.HttpContext?.Session.SetString(key, s);
            }
            catch { /* ignore */ }
        }

        /// <summary>Đọc và deserialize session; lỗi hoặc thiếu key trả về null.</summary>
        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                var s = _httpContextAccessor?.HttpContext?.Session.GetString(key) ?? "";
                if (!string.IsNullOrEmpty(s))
                    return JsonConvert.DeserializeObject<T>(s);
            }
            catch { /* ignore */ }
            return null;
        }
    }
}

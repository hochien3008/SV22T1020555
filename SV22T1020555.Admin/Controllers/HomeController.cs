using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.Admin.Models;
using SV22T1020555.BusinessLayers;
using System.Diagnostics;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Trang chủ quản trị: hiển thị dashboard thống kê tổng quan bán hàng.
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Khởi tạo controller với logger do DI cếp.
        /// </summary>
        /// <param name="logger">Logger được inject bởi ASP.NET DI.</param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị dashboard thống kê tổng quan (số đơn theo trạng thái, doanh thu).
        /// </summary>
        /// <returns>
        /// View trang chủ kèm dữ liệu dashboard.
        /// </returns>
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Trang chủ";
            var data = await SalesDataService.GetDashboardDataAsync();
            return View(data);
        }

        /// <summary>
        /// Trang chính sách / quyền riêng tư (nội dung tĩnh).
        /// </summary>
        /// <returns>
        /// View trang chính sách.
        /// </returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Trang lỗi pipeline ASP.NET; không được cache.
        /// </summary>
        /// <returns>
        /// View lỗi kèm RequestId để tra cứu trong log.
        /// </returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

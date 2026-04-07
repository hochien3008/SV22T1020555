using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Shop.Models;
using SV22T1020555.Models.Catalog;
using SV22T1020555.Models.Common;

namespace SV22T1020555.Shop.Controllers
{
    /// <summary>
    /// Trang chủ shop: danh mục và sản phẩm nổi bật.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Trang chủ: hiển thị danh mục và tối đa 8 mặt hàng nổi bật đang bán.
        /// </summary>
        /// <returns>
        /// View trang chủ kèm danh sách danh mục và sản phẩm nổi bật.
        /// </returns>
        public async Task<IActionResult> Index()
        {
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 12,
                SearchValue = ""
            });

            var input = new ProductSearchInput
            {
                Page = 1,
                PageSize = 8,
                SearchValue = "",
                OnlySelling = true
            };
            var featured = await CatalogDataService.ListProductsAsync(input);
            ViewBag.Categories = categories.DataItems ?? new List<Category>();
            return View(featured);
        }

        /// <summary>
        /// Trang chính sách / quyền riêng tư (mẫu, nội dung tĩnh).
        /// </summary>
        /// <returns>
        /// View trang chính sách.
        /// </returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Trang lỗi pipeline ASP.NET; không được cache bởi trình duyệt hay proxy.
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

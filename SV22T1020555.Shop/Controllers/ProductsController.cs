using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Catalog;
using SV22T1020555.Models.Common;

namespace SV22T1020555.Shop.Controllers
{
    /// <summary>
    /// Danh mục sản phẩm công khai: lọc, tìm kiếm; chỉ mặt hàng đang bán.
    /// </summary>
    public class ProductsController : Controller
    {
        private const string PRODUCT_SEARCH_INPUT = "ShopProductSearchInput";

        /// <summary>
        /// Hiển thị danh sách sản phẩm đang bán, hỗ trợ lọc theo danh mục, khoảng giá và từ khóa.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm và phân trang (danh mục, giá min/max, từ khóa, trang).</param>
        /// <returns>
        /// View danh sách sản phẩm kèm bộ lọc và danh mục sidebar.
        /// </returns>
        public async Task<IActionResult> Index(ProductSearchInput input)
        {
            // Nếu không có tham số lọc nào được truyền từ URL, hãy thử lấy từ Session
            if (Request.Query.Count == 0)
            {
                var sessionInput = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_INPUT);
                if (sessionInput != null)
                {
                    input = sessionInput;
                }
            }

            if (input.PageSize <= 0)
                input.PageSize = 12;
            if (input.Page <= 0)
                input.Page = 1;
            input.OnlySelling = true;

            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 10_000,
                SearchValue = ""
            });

            ViewBag.Categories = categories.DataItems ?? new List<Category>();
            ViewBag.Search = input;

            ApplicationContext.SetSessionData(PRODUCT_SEARCH_INPUT, input);

            return View(input);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (input.PageSize <= 0)
                input.PageSize = 12;
            input.OnlySelling = true;
            
            var result = await CatalogDataService.ListProductsAsync(input);
            ViewBag.Search = input;

            ApplicationContext.SetSessionData(PRODUCT_SEARCH_INPUT, input);

            return View(result);
        }

        /// <summary>
        /// Hiển thị trang chi tiết sản phẩm kèm thuộc tính, ảnh và các sản phẩm liên quan cùng danh mục.
        /// </summary>
        /// <param name="id">Mã sản phẩm cần xem chi tiết.</param>
        /// <returns>
        /// View chi tiết sản phẩm; 404 nếu không tìm thấy hoặc sản phẩm ngừng bán.
        /// </returns>
        public async Task<IActionResult> Details(int id)
        {
            var p = await CatalogDataService.GetProductAsync(id);
            if (p == null || !p.IsSelling)
                return NotFound();

            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);

            var related = await CatalogDataService.ListProductsAsync(new ProductSearchInput
            {
                Page = 1,
                PageSize = 8,
                CategoryID = p.CategoryID ?? 0,
                SearchValue = "",
                OnlySelling = true
            });
            ViewBag.RelatedProducts = (related.DataItems ?? new List<Product>())
                .Where(x => x.ProductID != p.ProductID)
                .Take(6)
                .ToList();
            return View(p);
        }
    }
}

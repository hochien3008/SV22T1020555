using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Catalog;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý loại hàng (Category): xem danh sách, bổ sung, cập nhật, xóa.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        private const int PAGESIZE = 10;
        private const string CATEGORY_SEARCH_INPUT = "CategorySearchInput";

        /// <summary>
        /// Hiển thị giao diện tìm kiếm loại hàng; đọc bộ lọc từ session nếu đã tìm trước đó.
        /// </summary>
        /// <returns>
        /// View tìm kiếm với bộ lọc hiện tại.
        /// </returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH_INPUT);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm và trả về danh sách loại hàng phân trang dưới dạng partial view.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm và phân trang (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// Partial view kết quả tìm kiếm loại hàng.
        /// </returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH_INPUT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form bổ sung loại hàng mới.
        /// </summary>
        /// <returns>
        /// View form nhập liệu (Edit) với model mới rỗng.
        /// </returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Hiển thị form cập nhật thông tin loại hàng.
        /// </summary>
        /// <param name="id">Mã loại hàng cần cập nhật.</param>
        /// <returns>
        /// View form Edit; chuyển về Index nếu không tìm thấy loại hàng.
        /// </returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu bổ sung hoặc cập nhật loại hàng vào CSDL.
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng từ form (CategoryID = 0 → bổ sung; > 0 → cập nhật).</param>
        /// <returns>
        /// Chuyển về Index nếu thành công; hiển thị lại form kèm lỗi nếu không hợp lệ.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật thông tin loại hàng";
            try
            {
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");

                if (string.IsNullOrEmpty(data.Description))
                    data.Description = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.CategoryID == 0)
                    await CatalogDataService.AddCategoryAsync(data);
                else
                    await CatalogDataService.UpdateCategoryAsync(data);

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("Error", "Hệ thống đang bận, Vui lòng thử lại sau!");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa loại hàng: GET hiển thị trang xác nhận, POST thực hiện xóa.
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa.</param>
        /// <returns>
        /// POST: chuyển về Index (kèm TempData lỗi nếu xóa không được).
        /// GET: View xác nhận xóa kèm cờ AllowDelete.
        /// </returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                if (!await CatalogDataService.DeleteCategoryAsync(id))
                    TempData["DeleteError"] = "Không xóa được loại hàng (đang có mặt hàng thuộc loại này).";
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));
            return View(model);
        }
    }
}

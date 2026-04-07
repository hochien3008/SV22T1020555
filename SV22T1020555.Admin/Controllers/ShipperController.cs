using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý người giao hàng (Shipper): xem danh sách, bổ sung, cập nhật, xóa.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        private const int PAGESIZE = 10;
        private const string SHIPPER_SEARCH_INPUT = "ShipperSearchInput";

        /// <summary>
        /// Hiển thị giao diện tìm kiếm người giao hàng; đọc bộ lọc từ session nếu đã tìm trước đó.
        /// </summary>
        /// <returns>
        /// View tìm kiếm với bộ lọc hiện tại.
        /// </returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH_INPUT);
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
        /// Thực hiện tìm kiếm và trả về danh sách người giao hàng phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm và phân trang (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// Partial view kết quả danh sách người giao hàng.
        /// </returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH_INPUT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form bổ sung người giao hàng mới.
        /// </summary>
        /// <returns>
        /// View form nhập liệu (Edit) với model mới rỗng.
        /// </returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Hiển thị form cập nhật thông tin người giao hàng.
        /// </summary>
        /// <param name="id">Mã người giao hàng cần cập nhật.</param>
        /// <returns>
        /// View form Edit; chuyển về Index nếu không tìm thấy.
        /// </returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu bổ sung hoặc cập nhật người giao hàng vào CSDL.
        /// </summary>
        /// <param name="data">Dữ liệu từ form (ShipperID = 0 → bổ sung; > 0 → cập nhật).</param>
        /// <returns>
        /// Chuyển về Index nếu thành công; hiển thị lại form kèm lỗi nếu không hợp lệ.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";
            try
            {
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên người giao hàng");
                if (string.IsNullOrEmpty(data.Phone))
                    data.Phone = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.ShipperID == 0)
                    await PartnerDataService.AddShipperAsync(data);
                else
                    await PartnerDataService.UpdateShipperAsync(data);

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("Error", "Hệ thống đang bận, Vui lòng thử lại sau!");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa người giao hàng: GET hiển thị trang xác nhận, POST thực hiện xóa.
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa.</param>
        /// <returns>
        /// POST: chuyển về Index; GET: View xác nhận xóa kèm cờ AllowDelete.
        /// </returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                if (!await PartnerDataService.DeleteShipperAsync(id))
                    TempData["DeleteError"] = "Không xóa được người giao hàng (đang được gán cho đơn).";
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedShipperAsync(id));
            return View(model);
        }
    }
}

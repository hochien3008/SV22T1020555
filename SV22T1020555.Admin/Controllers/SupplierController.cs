using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý nhà cung cấp (Supplier): xem danh sách, bổ sung, cập nhật, xóa.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        private const int PAGESIZE = 10;
        private const string SUPPLIER_SEARCH_INPUT = "SupplierSearchInput";

        /// <summary>
        /// Hiển thị giao diện tìm kiếm nhà cung cấp; đọc bộ lọc từ session nếu đã tìm trước đó.
        /// </summary>
        /// <returns>
        /// View tìm kiếm với bộ lọc hiện tại.
        /// </returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH_INPUT);
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
        /// Thực hiện tìm kiếm và trả về danh sách nhà cung cấp phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm và phân trang (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// Partial view kết quả danh sách nhà cung cấp.
        /// </returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH_INPUT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form bổ sung nhà cung cấp mới.
        /// </summary>
        /// <returns>
        /// View form nhập liệu (Edit) với model mới rỗng.
        /// </returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Hiển thị form cập nhật thông tin nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần cập nhật.</param>
        /// <returns>
        /// View form Edit; chuyển về Index nếu không tìm thấy.
        /// </returns>
        public async Task<IActionResult> Edit(int id )
        {
            ViewBag.Title = "Cập nhật thông tin Nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu bổ sung hoặc cập nhật nhà cung cấp vào CSDL.
        /// </summary>
        /// <param name="data">Dữ liệu từ form (SupplierID = 0 → bổ sung; > 0 → cập nhật).</param>
        /// <returns>
        /// Chuyển về Index nếu thành công; hiển thị lại form kèm lỗi nếu không hợp lệ.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0 ? "Bổ sung Nhà cung cấp" : "Cập nhật thông tin Nhà cung cấp";

            try
            {
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên của khách hàng");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng cho biết Email của khách hàng");
                else if (!(await PartnerDataService.ValidateSupplierEmailAsync(data.Email, data.SupplierID)))
                    ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");

                //Điều chỉnh lại các giá trị dữ liệu khác theo qui định/qui ước của App
                if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }


                //Yêu cầu lưu dữ liệu vào CSDL
                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }
                return RedirectToAction("Index");

            }
            catch (Exception)
            {
                //Lưu log lỗi (ghi log tại đây nếu có ILogger)
                ModelState.AddModelError("Error", "Hệ thống đang bận, Vui lòng thủ lại sau!");
                return View("Edit", data);
            }

        }
        /// <summary>
        /// Xóa nhà cung cấp: GET hiển thị trang xác nhận, POST thực hiện xóa.
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa.</param>
        /// <returns>
        /// POST: chuyển về Index; GET: View xác nhận xóa kèm cờ AllowDelete.
        /// </returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                if (!await PartnerDataService.DeleteSupplierAsync(id))
                    TempData["DeleteError"] = "Không xóa được nhà cung cấp (đang được sử dụng).";
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedSupplierAsync(id));

            return View(model);
        }

    }
}

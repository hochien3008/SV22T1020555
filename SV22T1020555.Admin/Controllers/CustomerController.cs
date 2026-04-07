using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;
using System.Linq.Expressions;

namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý khách hàng (Customer): xem danh sách, bổ sung, cập nhật, xóa.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CustomerController : Controller
    {
        //private const int PAGESIZE = 10; //Hard Code
        /// <summary>
        /// tên biến session lưu lại điều kiện tìm kiếm khách hàng
        /// </summary>
        private const string CUSTOMER_SEARCH_INPUT = "CustomerSearchInput";
        /// <summary>Hiển thị giao diện tìm kiếm khách hàng; đọc bộ lọc từ session nếu đã tìm trước đó.</summary>
        /// <returns>View tìm kiếm với bộ lọc hiện tại.</returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH_INPUT);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm và trả về danh sách khách hàng phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm và phân trang (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// Partial view kết quả danh sách khách hàng.
        /// </returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH_INPUT, input);
            return View(result);
        }
        /// <summary>
        /// Hiển thị form bổ sung khách hàng mới.
        /// </summary>
        /// <returns>
        /// View form nhập liệu (Edit) với model mới rỗng.
        /// </returns>
        public IActionResult Create() {
            ViewBag.Title = "Bổ sung khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Hiển thị form cập nhật thông tin khách hàng.
        /// </summary>
        /// <param name="id">Mã khách hàng cần cập nhật.</param>
        /// <returns>
        /// View form Edit; chuyển về Index nếu không tìm thấy.
        /// </returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu bổ sung hoặc cập nhật khách hàng vào CSDL.
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng từ form (CustomerID = 0 → bổ sung; > 0 → cập nhật).</param>
        /// <returns>
        /// Chuyển về Index nếu thành công; hiển thị lại form kèm lỗi nếu không hợp lệ.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

            //Sử dụng ModelState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
            try {
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên của khách hàng");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng cho biết Email của khách hàng");
                else if (!(await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID)))
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
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
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
        /// Xóa khách hàng: GET hiển thị trang xác nhận, POST thực hiện xóa.
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa.</param>
        /// <returns>
        /// POST: chuyển về Index; GET: View xác nhận xóa kèm cờ AllowDelete.
        /// </returns>
        public async Task<IActionResult> Delete(int id)
        {
            if(Request.Method == "POST")
            {
                if (!await PartnerDataService.DeleteCustomerAsync(id))
                    TempData["DeleteError"] = "Không xóa được khách hàng (đang có đơn hàng tham chiếu).";
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));

            return View(model);
        }
        /// <summary>
        /// Chỳc năng đổi mật khẩu khách hàng.
        /// </summary>
        /// <param name="id">Mã khách hàng.</param>
        /// <returns>
        /// View placeholder.
        /// </returns>
        public IActionResult ChangePassword(int id)
        {
            return View();
        }
    }
}

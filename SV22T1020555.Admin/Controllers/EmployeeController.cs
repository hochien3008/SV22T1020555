using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020555.BusinessLayers;
using SV22T1020555.Admin.Models;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.HR;
namespace SV22T1020555.Admin.Controllers
{
    /// <summary>
    /// Quản lý nhân viên (Employee): xem, bổ sung, cập nhật, xóa, đổi mật khẩu và phân quyền.
    /// Chỉ Administrator mới có quyền truy cập.
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class EmployeeController : Controller
    {
        private const int PAGESIZE = 10;
        private const string EMPLOYEE_SEARCH_INPUT = "EmployeeSearchInput";

        /// <summary>
        /// Hiển thị giao diện tìm kiếm nhân viên; đọc bộ lọc từ session nếu đã tìm trước đó.
        /// </summary>
        /// <returns>
        /// View tìm kiếm với bộ lọc hiện tại.
        /// </returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH_INPUT);
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
        /// Thực hiện tìm kiếm và trả về danh sách nhân viên phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm và phân trang (từ khóa, trang, số dòng).</param>
        /// <returns>
        /// Partial view kết quả danh sách nhân viên.
        /// </returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH_INPUT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form bổ sung nhân viên mới.
        /// </summary>
        /// <returns>
        /// View form nhập liệu (Edit) với model mới rỗng, IsWorking = true.
        /// </returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Hiển thị form cập nhật thông tin nhân viên.
        /// </summary>
        /// <param name="id">Mã nhân viên cần cập nhật.</param>
        /// <returns>
        /// View form Edit; chuyển về Index nếu không tìm thấy.
        /// </returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu bổ sung hoặc cập nhật nhân viên vào CSDL.
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên từ form (EmployeeID = 0 → bổ sung; > 0 → cập nhật).</param>
        /// <returns>
        /// Chuyển về Index nếu thành công; hiển thị lại form kèm lỗi nếu không hợp lệ.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data)
        {
            ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";
            try
            {
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");
                else if (!(await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID)))
                    ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");

                if (string.IsNullOrEmpty(data.Address))
                    data.Address = "";
                if (string.IsNullOrEmpty(data.Phone))
                    data.Phone = "";

                if (data.EmployeeID > 0 && string.IsNullOrWhiteSpace(data.Photo))
                {
                    var oldData = await HRDataService.GetEmployeeAsync(data.EmployeeID);
                    data.Photo = oldData?.Photo;
                }

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.EmployeeID == 0)
                    await HRDataService.AddEmployeeAsync(data);
                else
                    await HRDataService.UpdateEmployeeAsync(data);

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("Error", "Hệ thống đang bận, Vui lòng thử lại sau!");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa nhân viên: GET hiển thị trang xác nhận, POST thực hiện xóa.
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa.</param>
        /// <returns>
        /// POST: chuyển về Index; GET: View xác nhận xóa kèm cờ AllowDelete.
        /// </returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                if (!await HRDataService.DeleteEmployeeAsync(id))
                    TempData["DeleteError"] = "Không xóa được nhân viên (đang được tham chiếu, ví dụ đơn hàng).";
                return RedirectToAction("Index");
            }

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
            return View(model);
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu cho nhân viên (admin reset, không cần mật khẩu cũ).
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu.</param>
        /// <returns>
        /// View form đổi mật khẩu; chuyển về Index nếu nhân viên không tồn tại.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            var model = new EmployeeChangePasswordViewModel
            {
                Employee = employee
            };
            return View(model);
        }

        /// <summary>
        /// Lưu mật khẩu mới cho nhân viên (admin reset, không cần xác thực mật khẩu cũ).
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu.</param>
        /// <param name="model">Dữ liệu form gồm mật khẩu mới và xác nhận.</param>
        /// <returns>
        /// Chuyển về Index sau khi đổi thành công; hiển thị lại form kèm lỗi nếu thất bại.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, EmployeeChangePasswordViewModel model)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            model.Employee = employee;

            if (string.IsNullOrWhiteSpace(model.NewPassword))
                ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới");
            if (model.NewPassword != model.ConfirmPassword)
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Xác nhận mật khẩu không khớp");

            if (!ModelState.IsValid)
                return View(model);

            var newHash = CryptHelper.HashMD5(model.NewPassword);
            bool ok = await HRDataService.SetEmployeePasswordAsync(id, newHash);
            if (!ok)
            {
                ModelState.AddModelError("Error", "Không đổi được mật khẩu. Vui lòng thử lại.");
                return View(model);
            }

            TempData["Message"] = "Đổi mật khẩu nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }
        /// <summary>
        /// Hiển thị form phân quyền (roles) của nhân viên.
        /// </summary>
        /// <param name="id">Mã nhân viên cần xem/cập nhật quyền.</param>
        /// <returns>
        /// View form phân quyền với danh sách role đang được chọn.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            var roleNames = await HRDataService.GetEmployeeRoleNamesAsync(id) ?? string.Empty;
            var selectedRoles = roleNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var model = new EmployeeRoleViewModel
            {
                Employee = employee,
                SelectedRoles = selectedRoles
            };

            return View(model);
        }

        /// <summary>
        /// Lưu danh sách quyền (roles) mới cho nhân viên; lọc bỏ role không hợp lệ.
        /// </summary>
        /// <param name="id">Mã nhân viên cần cập nhật quyền.</param>
        /// <param name="model">Model chứa danh sách role được chọn từ form.</param>
        /// <returns>
        /// Chuyển lại trang ChangeRole sau khi lưu.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, EmployeeRoleViewModel model)
        {
            if (model.Employee.EmployeeID != id)
                model.Employee.EmployeeID = id;

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            // Chỉ cho phép các role hợp lệ theo WebUserRoles
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                WebUserRoles.Administrator,
                WebUserRoles.DataManager,
                WebUserRoles.Sales
            };

            var roles = (model.SelectedRoles ?? new List<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Where(r => allowed.Contains(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var roleNames = string.Join(",", roles);
            await HRDataService.UpdateEmployeeRoleNamesAsync(id, roleNames);

            return RedirectToAction(nameof(ChangeRole), new { id });
        }
    }
}

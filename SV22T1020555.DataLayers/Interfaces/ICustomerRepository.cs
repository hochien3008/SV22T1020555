using SV22T1020555.Models.Partner;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Customer
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ hay không?
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của khách hàng mới.
        /// Nếu id <> 0: Kiểm tra email đối với khách hàng đã tồn tại
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (chưa có ai dùng);
        /// False nếu email đã tồn tại.
        /// </returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);
    }
}

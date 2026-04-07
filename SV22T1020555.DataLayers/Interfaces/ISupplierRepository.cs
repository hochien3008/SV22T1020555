using SV22T1020555.Models.Partner;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu cho nhà cung cấp vượt ngoài các phép xử lý cơ bản (Generic)
    /// </summary>
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Kiểm tra xem email của nhà cung cấp có hợp lệ (không trùng) hay không.
        /// </summary>
        Task<bool> ValidateEmailAsync(string email, int supplierID);
    }
}

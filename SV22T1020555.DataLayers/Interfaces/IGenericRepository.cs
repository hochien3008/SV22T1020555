using SV22T1020555.Models.Common;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu đơn giản trên một
    /// kiểu dữ liệu T nào đó (T là một Entity/DomainModel nào đó)
    /// </summary>
    /// <typeparam name="T">Kiểu entity/model tương ứng với bảng trong CSDL.</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Truy vấn, tìm kiếm dữ liệu và trả về kết quả dưới dạng được phân trang.
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm: từ khóa, số trang, số dòng mỗi trang.</param>
        /// <returns>
        /// Đối tượng PagedResult chứa danh sách dữ liệu và thông tin phân trang.
        /// </returns>
        Task<PagedResult<T>> ListAsync(PaginationSearchInput input);
        /// <summary>
        /// Lấy dữ liệu của một bản ghi có mã là id (trả về null nếu không có dữ liệu).
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cần lấy.</param>
        /// <returns>
        /// Đối tượng T nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        Task<T?> GetAsync(int id);
        /// <summary>
        /// Bổ sung một bản ghi vào bảng trong CSDL.
        /// </summary>
        /// <param name="data">Đối tượng dữ liệu cần thêm mới.</param>
        /// <returns>
        /// Mã định danh (IDENTITY) của bản ghi vừa được thêm;
        /// 0 nếu thất bại.
        /// </returns>
        Task<int> AddAsync(T data);
        /// <summary>
        /// Cập nhật một bản ghi trong bảng của CSDL.
        /// </summary>
        /// <param name="data">Đối tượng dữ liệu chứa thông tin cần cập nhật (ID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công (có ít nhất 1 dòng bị ảnh hưởng);
        /// False nếu không tìm thấy bản ghi.
        /// </returns>
        Task<bool> UpdateAsync(T data);
        /// <summary>
        /// Xóa bản ghi có mã là id.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy bản ghi.
        /// </returns>
        Task<bool> DeleteAsync(int id);
        /// <summary>
        /// Kiểm tra xem một bản ghi có mã là id có đang được tham chiếu bởi dữ liệu khác không.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cần kiểm tra.</param>
        /// <returns>
        /// True nếu bản ghi đang được sử dụng (không thể xóa);
        /// False nếu có thể xóa an toàn.
        /// </returns>
        Task<bool> IsUsedAsync(int id);
    }
}

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu đọc cho từ điển dữ liệu (danh mục tham chiếu, chỉ đọc).
    /// </summary>
    /// <typeparam name="T">Kiểu entity từ điển (ví dụ: Province).</typeparam>
    public interface IDataDictionaryRepository<T> where T : class
    {
        /// <summary>
        /// Lấy toàn bộ danh sách dữ liệu từ điển (sắp xếp theo tên).
        /// </summary>
        /// <returns>
        /// Danh sách tất cả bản ghi kiểu T từ bảng từ điển tương ứng.
        /// </returns>
        Task<List<T>> ListAsync();
    }
}

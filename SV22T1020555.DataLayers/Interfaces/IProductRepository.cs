using SV22T1020555.Models.Catalog;
using SV22T1020555.Models.Common;

namespace SV22T1020555.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu cho mặt hàng
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Tìm kiếm và lấy danh sách mặt hàng phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm mặt hàng (từ khóa, loại, nhà cung cấp, giá).</param>
        /// <returns>PagedResult chứa danh sách mặt hàng và thông tin phân trang.</returns>
        Task<PagedResult<Product>> ListAsync(ProductSearchInput input);
        /// <summary>
        /// Lấy thông tin đầy đủ của một mặt hàng.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần lấy.</param>
        /// <returns>Product nếu tìm thấy; null nếu không tồn tại.</returns>
        Task<Product?> GetAsync(int productID);
        /// <summary>
        /// Bổ sung mặt hàng mới vào CSDL.
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng cần thêm.</param>
        /// <returns>Mã mặt hàng (IDENTITY) vừa được thêm; 0 nếu thất bại.</returns>
        Task<int> AddAsync(Product data);
        /// <summary>
        /// Cập nhật thông tin mặt hàng.
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng (ProductID phải hợp lệ).</param>
        /// <returns>True nếu cập nhật thành công; False nếu không tìm thấy.</returns>
        Task<bool> UpdateAsync(Product data);
        /// <summary>
        /// Xóa mặt hàng khỏi CSDL.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần xóa.</param>
        /// <returns>True nếu xóa thành công; False nếu không tìm thấy.</returns>
        Task<bool> DeleteAsync(int productID);
        /// <summary>
        /// Kiểm tra mặt hàng có đang được tham chiếu trong đơn hàng không.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần kiểm tra.</param>
        /// <returns>True nếu đang được sử dụng (không thể xóa); False nếu có thể xóa an toàn.</returns>
        Task<bool> IsUsedAsync(int productID);

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng.
        /// </summary>
        /// <param name="productID">Mã của mặt hàng cần lấy thuộc tính.</param>
        /// <returns>Danh sách các ProductAttribute.</returns>
        Task<List<ProductAttribute>> ListAttributesAsync(int productID);
        /// <summary>
        /// Lấy thông tin của một thuộc tính mặt hàng.
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính cần lấy.</param>
        /// <returns>ProductAttribute nếu tìm thấy; null nếu không tồn tại.</returns>
        Task<ProductAttribute?> GetAttributeAsync(long attributeID);
        /// <summary>
        /// Bổ sung thuộc tính mới cho mặt hàng.
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính cần thêm.</param>
        /// <returns>Mã thuộc tính (IDENTITY) vừa thêm; 0 nếu thất bại.</returns>
        Task<long> AddAttributeAsync(ProductAttribute data);
        /// <summary>
        /// Cập nhật thuộc tính mặt hàng.
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính mới.</param>
        /// <returns>True nếu cập nhật thành công; False nếu không tìm thấy.</returns>
        Task<bool> UpdateAttributeAsync(ProductAttribute data);
        /// <summary>
        /// Xóa thuộc tính khỏi mặt hàng.
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính cần xóa.</param>
        /// <returns>True nếu xóa thành công; False nếu không tìm thấy.</returns>
        Task<bool> DeleteAttributeAsync(long attributeID);

        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần lấy ảnh.</param>
        /// <returns>Danh sách ProductPhoto.</returns>
        Task<List<ProductPhoto>> ListPhotosAsync(int productID);
        /// <summary>
        /// Lấy thông tin một ảnh của mặt hàng.
        /// </summary>
        /// <param name="photoID">Mã ảnh cần lấy.</param>
        /// <returns>ProductPhoto nếu tìm thấy; null nếu không tồn tại.</returns>
        Task<ProductPhoto?> GetPhotoAsync(long photoID);
        /// <summary>
        /// Bổ sung ảnh mới cho mặt hàng.
        /// </summary>
        /// <param name="data">Dữ liệu ảnh cần thêm.</param>
        /// <returns>Mã ảnh (IDENTITY) vừa thêm; 0 nếu thất bại.</returns>
        Task<long> AddPhotoAsync(ProductPhoto data);
        /// <summary>
        /// Cập nhật thông tin ảnh.
        /// </summary>
        /// <param name="data">Dữ liệu ảnh mới.</param>
        /// <returns>True nếu cập nhật thành công; False nếu không tìm thấy.</returns>
        Task<bool> UpdatePhotoAsync(ProductPhoto data);
        /// <summary>
        /// Xóa ảnh khỏi mặt hàng.
        /// </summary>
        /// <param name="photoID">Mã ảnh cần xóa.</param>
        /// <returns>True nếu xóa thành công; False nếu không tìm thấy.</returns>
        Task<bool> DeletePhotoAsync(long photoID);
        /// <summary>
        /// Kiểm tra xem tên ảnh (fileName) có đang được dùng trong Products hoặc ProductPhotos hay không.
        /// </summary>
        /// <param name="fileName">Tên file ảnh cần kiểm tra.</param>
        /// <returns>True nếu đang dùng (không được xóa file); False nếu không còn chỗ nào dùng.</returns>
        Task<bool> IsPhotoUsedAsync(string fileName);
    }
}

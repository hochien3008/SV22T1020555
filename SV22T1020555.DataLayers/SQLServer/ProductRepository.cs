using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.Models.Catalog;
using SV22T1020555.Models.Common;

namespace SV22T1020555.DataLayers.SQLServer
{
    /// <summary>
    /// Triển khai IProductRepository trên SQL Server;
    /// CRUD mặt hàng, quản lý thuộc tính và ảnh bằng Dapper.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối SQL Server.
        /// </summary>
        /// <param name="connectionString">Connection string đến CSDL SQL Server.</param>
        public ProductRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Tạo và trả về SqlConnection mới từ connection string.
        /// </summary>
        /// <returns>
        /// Một instance SqlConnection chưa mở kết nối.
        /// </returns>
        private SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        #region PRODUCT

        /// <summary>
        /// Tìm kiếm mặt hàng theo nhiều tiêu chí và trả về danh sách phân trang.
        /// </summary>
        /// <param name="input">Bộ lọc tìm kiếm (từ khóa, loại hàng, nhà cung cấp, giá, trạng thái bán).</param>
        /// <returns>
        /// PagedResult chứa danh sách mặt hàng và thông tin phân trang.
        /// </returns>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = GetConnection();

            string sql = @"
                        SELECT COUNT(*)
                        FROM Products
                        WHERE ProductName LIKE @search
                          AND (@categoryId = 0 OR CategoryID = @categoryId)
                          AND (@supplierId = 0 OR SupplierID = @supplierId)
                          AND (@minPrice = 0 OR Price >= @minPrice)
                          AND (@maxPrice = 0 OR Price <= @maxPrice)
                          AND (@onlySelling = 0 OR IsSelling = 1);

                        SELECT *
                        FROM Products
                        WHERE ProductName LIKE @search
                          AND (@categoryId = 0 OR CategoryID = @categoryId)
                          AND (@supplierId = 0 OR SupplierID = @supplierId)
                          AND (@minPrice = 0 OR Price >= @minPrice)
                          AND (@maxPrice = 0 OR Price <= @maxPrice)
                          AND (@onlySelling = 0 OR IsSelling = 1)
                        ORDER BY ProductName
                        OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY";

            var param = new
            {
                search = $"%{input.SearchValue}%",
                offset = (input.Page - 1) * input.PageSize,
                pagesize = input.PageSize,
                categoryId = input.CategoryID,
                supplierId = input.SupplierID,
                minPrice = input.MinPrice,
                maxPrice = input.MaxPrice,
                onlySelling = input.OnlySelling ? 1 : 0
            };

            using var multi = await connection.QueryMultipleAsync(sql, param);

            int count = multi.Read<int>().Single();
            var data = multi.Read<Product>().ToList();

            return new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin đầy đủ của một mặt hàng theo ProductID.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần lấy.</param>
        /// <returns>
        /// Product nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM Products WHERE ProductID=@productID";

            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { productID });
        }

        /// <summary>
        /// Thêm mới một mặt hàng vào bảng Products.
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng cần thêm.</param>
        /// <returns>
        /// ProductID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = GetConnection();

            string sql = @"
                        INSERT INTO Products(ProductName,ProductDescription,SupplierID,CategoryID,Unit,Price,Photo,IsSelling)
                        VALUES(@ProductName,@ProductDescription,@SupplierID,@CategoryID,@Unit,@Price,@Photo,@IsSelling);
                        SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng (tên, mô tả, loại, nhà cung cấp, đơn vị, giá, ảnh, trạng thái bán).
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng mới (ProductID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy mặt hàng.
        /// </returns>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = GetConnection();

            string sql = @"
                        UPDATE Products
                        SET ProductName=@ProductName,
                            ProductDescription=@ProductDescription,
                            SupplierID=@SupplierID,
                            CategoryID=@CategoryID,
                            Unit=@Unit,
                            Price=@Price,
                            Photo=@Photo,
                            IsSelling=@IsSelling
                        WHERE ProductID=@ProductID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa mặt hàng khỏi bảng Products.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy mặt hàng.
        /// </returns>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM Products WHERE ProductID=@productID";

            return await connection.ExecuteAsync(sql, new { productID }) > 0;
        }

        /// <summary>
        /// Kiểm tra mặt hàng có đang được tham chiếu trong bảng OrderDetails không.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần kiểm tra.</param>
        /// <returns>
        /// True nếu đang được sử dụng trong đơn hàng (không thể xóa);
        /// False nếu có thể xóa an toàn.
        /// </returns>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = "SELECT COUNT(*) FROM OrderDetails WHERE ProductID=@productID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { productID });

            return count > 0;
        }

        #endregion


        #region ATTRIBUTE

        /// <summary>
        /// Lấy toàn bộ danh sách thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần lấy thuộc tính.</param>
        /// <returns>
        /// Danh sách ProductAttribute;
        /// danh sách rỗng nếu mặt hàng không có thuộc tính.
        /// </returns>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM ProductAttributes WHERE ProductID=@productID";

            var data = await connection.QueryAsync<ProductAttribute>(sql, new { productID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin một thuộc tính mặt hàng theo AttributeID.
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính cần lấy.</param>
        /// <returns>
        /// ProductAttribute nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM ProductAttributes WHERE AttributeID=@attributeID";

            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { attributeID });
        }

        /// <summary>
        /// Thêm mới một thuộc tính cho mặt hàng vào bảng ProductAttributes.
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính cần thêm (ProductID, AttributeName, AttributeValue, DisplayOrder).</param>
        /// <returns>
        /// AttributeID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = GetConnection();

            string sql = @"
                        INSERT INTO ProductAttributes(ProductID,AttributeName,AttributeValue,DisplayOrder)
                        VALUES(@ProductID,@AttributeName,@AttributeValue,@DisplayOrder);
                        SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        /// <summary>
        /// Cập nhật tên, giá trị và thứ tự hiển thị của một thuộc tính mặt hàng.
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính mới (AttributeID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy thuộc tính.
        /// </returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = GetConnection();

            string sql = @"
                        UPDATE ProductAttributes
                        SET AttributeName=@AttributeName,
                            AttributeValue=@AttributeValue,
                            DisplayOrder=@DisplayOrder
                        WHERE AttributeID=@AttributeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa một thuộc tính mặt hàng khỏi bảng ProductAttributes.
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy thuộc tính.
        /// </returns>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM ProductAttributes WHERE AttributeID=@attributeID";

            return await connection.ExecuteAsync(sql, new { attributeID }) > 0;
        }

        #endregion


        #region PHOTO

        /// <summary>
        /// Lấy toàn bộ danh sách ảnh của một mặt hàng.
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần lấy ảnh.</param>
        /// <returns>
        /// Danh sách ProductPhoto;
        /// danh sách rỗng nếu mặt hàng không có ảnh.
        /// </returns>
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM ProductPhotos WHERE ProductID=@productID";

            var data = await connection.QueryAsync<ProductPhoto>(sql, new { productID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin một ảnh mặt hàng theo PhotoID.
        /// </summary>
        /// <param name="photoID">Mã ảnh cần lấy.</param>
        /// <returns>
        /// ProductPhoto nếu tìm thấy;
        /// null nếu không tồn tại.
        /// </returns>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM ProductPhotos WHERE PhotoID=@photoID";

            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { photoID });
        }

        /// <summary>
        /// Thêm mới một ảnh cho mặt hàng vào bảng ProductPhotos.
        /// </summary>
        /// <param name="data">Dữ liệu ảnh cần thêm (ProductID, Photo, Description, DisplayOrder, IsHidden).</param>
        /// <returns>
        /// PhotoID (IDENTITY) vừa được tạo;
        /// 0 nếu thất bại.
        /// </returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            data.Description ??= string.Empty;

            using var connection = GetConnection();

            string sql = @"
                        INSERT INTO ProductPhotos(ProductID,Photo,Description,DisplayOrder,IsHidden)
                        VALUES(@ProductID,@Photo,@Description,@DisplayOrder,@IsHidden);
                        SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin ảnh mặt hàng (đường dẫn ảnh, mô tả, thứ tự, trạng thái ẩn).
        /// </summary>
        /// <param name="data">Dữ liệu ảnh mới (PhotoID phải hợp lệ).</param>
        /// <returns>
        /// True nếu cập nhật thành công;
        /// False nếu không tìm thấy ảnh.
        /// </returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            data.Description ??= string.Empty;

            using var connection = GetConnection();

            string sql = @"
                        UPDATE ProductPhotos
                        SET Photo=@Photo,
                            Description=@Description,
                            DisplayOrder=@DisplayOrder,
                            IsHidden=@IsHidden
                        WHERE PhotoID=@PhotoID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa một ảnh mặt hàng khỏi bảng ProductPhotos.
        /// </summary>
        /// <param name="photoID">Mã ảnh cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công;
        /// False nếu không tìm thấy ảnh.
        /// </returns>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM ProductPhotos WHERE PhotoID=@photoID";

            return await connection.ExecuteAsync(sql, new { photoID }) > 0;
        }

        public async Task<bool> IsPhotoUsedAsync(string fileName)
        {
            using var connection = GetConnection();

            string sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM Products WHERE LTRIM(RTRIM(ISNULL(Photo,''))) = @name)
                  + (SELECT COUNT(*) FROM ProductPhotos WHERE LTRIM(RTRIM(ISNULL(Photo,''))) = @name);";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { name = fileName.Trim() });

            return count > 0;
        }

        #endregion
    }
}
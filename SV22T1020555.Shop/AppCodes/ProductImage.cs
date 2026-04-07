namespace SV22T1020555.Shop
{
    /// <summary>Đường dẫn ảnh sản phẩm trong thư mục <c>wwwroot/images/products</c>.</summary>
    public static class ProductImage
    {
        /// <summary>Tên file ảnh mặc định khi sản phẩm không có ảnh.</summary>
        public const string DefaultFileName = "default-thumbnail-400.jpg";

        /// <summary>Trả về tên file ảnh hoặc <see cref="DefaultFileName"/> nếu rỗng.</summary>
        public static string FileName(string? photo) =>
            string.IsNullOrWhiteSpace(photo) ? DefaultFileName : photo.Trim();
    }
}

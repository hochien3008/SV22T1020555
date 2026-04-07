using SV22T1020555.Models.Sales;

namespace SV22T1020555.Shop
{
    /// <summary>
    /// Giỏ hàng lưu session (JSON): giỏ thường và giỏ Mua ngay tách biệt.
    /// </summary>
    public static class ShopCartHelper
    {
        private const string CartKey = "ShopShoppingCart";
        private const string BuyNowCartKey = "ShopBuyNowCart";

        /// <summary>Lấy hoặc khởi tạo giỏ hàng thường trong session.</summary>
        public static List<OrderDetailViewInfo> GetCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CartKey);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CartKey, cart);
            }
            return cart;
        }

        /// <summary>Giỏ Mua ngay (một hoặc nhiều dòng tùy lần set); rỗng nếu chưa có.</summary>
        public static List<OrderDetailViewInfo> GetBuyNowCart()
        {
            return ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(BuyNowCartKey) ?? new List<OrderDetailViewInfo>();
        }

        /// <summary>True nếu session đang có giỏ Mua ngay không rỗng.</summary>
        public static bool HasBuyNowCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(BuyNowCartKey);
            return cart != null && cart.Count > 0;
        }

        /// <summary>Ghi đè giỏ Mua ngay bằng một dòng (thanh toán nhanh).</summary>
        public static void SetBuyNowCart(OrderDetailViewInfo item)
        {
            ApplicationContext.SetSessionData(BuyNowCartKey, new List<OrderDetailViewInfo> { item });
        }

        /// <summary>Xóa nội dung giỏ Mua ngay.</summary>
        public static void ClearBuyNowCart()
        {
            ApplicationContext.SetSessionData(BuyNowCartKey, new List<OrderDetailViewInfo>());
        }

        /// <summary>Ưu tiên giỏ Mua ngay nếu có; không thì giỏ thường.</summary>
        public static List<OrderDetailViewInfo> GetCartForCheckout()
        {
            return HasBuyNowCart() ? GetBuyNowCart() : GetCart();
        }

        /// <summary>Sau đặt hàng: xóa giỏ tương ứng nguồn thanh toán.</summary>
        public static void ClearCartForCheckout()
        {
            if (HasBuyNowCart()) ClearBuyNowCart();
            else Clear();
        }

        /// <summary>Thêm dòng hoặc cộng số lượng nếu cùng <c>ProductID</c>; cập nhật giá theo lần thêm.</summary>
        public static void Add(OrderDetailViewInfo item)
        {
            var cart = GetCart();
            var existing = cart.Find(m => m.ProductID == item.ProductID);
            if (existing == null)
                cart.Add(item);
            else
            {
                existing.Quantity += item.Quantity;
                existing.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(CartKey, cart);
        }

        /// <summary>Cập nhật số lượng và đơn giá một dòng trong giỏ thường.</summary>
        public static void Update(int productId, int quantity, decimal salePrice)
        {
            if (productId <= 0 || quantity <= 0 || salePrice < 0) return;
            var cart = GetCart();
            var item = cart.Find(m => m.ProductID == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
                ApplicationContext.SetSessionData(CartKey, cart);
            }
        }

        /// <summary>Xóa sản phẩm khỏi giỏ thường.</summary>
        public static void Remove(int productId)
        {
            var cart = GetCart();
            var i = cart.FindIndex(m => m.ProductID == productId);
            if (i >= 0)
            {
                cart.RemoveAt(i);
                ApplicationContext.SetSessionData(CartKey, cart);
            }
        }

        /// <summary>Làm rỗng giỏ thường.</summary>
        public static void Clear()
        {
            ApplicationContext.SetSessionData(CartKey, new List<OrderDetailViewInfo>());
        }
    }
}

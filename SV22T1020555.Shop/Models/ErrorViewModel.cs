namespace SV22T1020555.Shop.Models
{
    /// <summary>View model trang lỗi (RequestId để tra cứu log).</summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

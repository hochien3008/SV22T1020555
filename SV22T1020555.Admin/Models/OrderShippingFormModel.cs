using SV22T1020555.Models.Partner;

namespace SV22T1020555.Admin.Models
{
    public class OrderShippingFormModel
    {
        public int OrderID { get; set; }
        public List<Shipper> Shippers { get; set; } = new();
    }
}

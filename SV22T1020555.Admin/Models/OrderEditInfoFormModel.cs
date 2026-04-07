using SV22T1020555.Models.DataDictionary;

namespace SV22T1020555.Admin.Models
{
    public class OrderEditInfoFormModel
    {
        public int OrderID { get; set; }
        public int? CustomerID { get; set; }
        public string? DeliveryProvince { get; set; }
        public string? DeliveryAddress { get; set; }

        public List<Province> Provinces { get; set; } = new();
    }
}


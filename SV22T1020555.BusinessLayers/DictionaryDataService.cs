using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.DataLayers.SQLServer;
using SV22T1020555.Models.DataDictionary;
using System.Threading.Tasks;

namespace SV22T1020555.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến từ điển dữ liệu của hệ thống,
    /// bao gồm: danh sách tỉnh/thành (Province).
    /// </summary>
    public static class DictionaryDataService
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static DictionaryDataService()
        {
            provinceDB = new ProvinceRepository(Configuration.ConnectionString);
        }
        /// <summary>
        /// Lấy danh sách toàn bộ tỉnh/thành trong hệ thống.
        /// </summary>
        /// <returns>
        /// Danh sách các đối tượng Province; danh sách rỗng nếu chưa có dữ liệu.
        /// </returns>
        public static async Task<List<Province>> ListProvincesAsync()
        {
            return await provinceDB.ListAsync();
        }
    }
}

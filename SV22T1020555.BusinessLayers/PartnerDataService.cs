using SV22T1020555.BusinessLayers;
using SV22T1020555.DataLayers.Interfaces;
using SV22T1020555.DataLayers.SQLServer;
using SV22T1020555.Models.Common;
using SV22T1020555.Models.Partner;

/// <summary>
/// Cung cấp các chức năng xử lý dữ liệu liên quan đến các đối tác của hệ thống
/// bao gồm: nhà cung cấp (Supplier), khách hàng (Customer) và người giao hàng (Shipper)
/// </summary>
public static class PartnerDataService
{
    private static readonly ISupplierRepository supplierDB;
    private static readonly ICustomerRepository customerDB;
    private static readonly IGenericRepository<Shipper> shipperDB;

    /// <summary>
    /// Constructor
    /// </summary>
    static PartnerDataService()
    {
        supplierDB = new SupplierRepository(Configuration.ConnectionString);
        customerDB = new CustomerRepository(Configuration.ConnectionString);
        shipperDB = new ShipperRepository(Configuration.ConnectionString);
    }

    #region Supplier

    /// <summary>
    /// Tìm kiếm và lấy danh sách nhà cung cấp dưới dạng phân trang.
    /// </summary>
    public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
    {
        return await supplierDB.ListAsync(input);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một nhà cung cấp dựa vào mã nhà cung cấp.
    /// </summary>
    public static async Task<Supplier?> GetSupplierAsync(int supplierID)
    {
        return await supplierDB.GetAsync(supplierID);
    }

    /// <summary>
    /// Bổ sung một nhà cung cấp mới vào hệ thống.
    /// </summary>
    public static async Task<int> AddSupplierAsync(Supplier data)
    {
        if (data == null)
            return 0;
        data.SupplierName = (data.SupplierName ?? "").Trim();
        data.ContactName = (data.ContactName ?? "").Trim();
        data.Province = (data.Province ?? "").Trim();
        data.Address = (data.Address ?? "").Trim();
        data.Phone = (data.Phone ?? "").Trim();
        data.Email = (data.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(data.SupplierName) || string.IsNullOrWhiteSpace(data.Email))
            return 0;

        return await supplierDB.AddAsync(data);
    }

    /// <summary>
    /// Cập nhật thông tin của một nhà cung cấp.
    /// </summary>
    public static async Task<bool> UpdateSupplierAsync(Supplier data)
    {
        if (data == null || data.SupplierID <= 0)
            return false;
        data.SupplierName = (data.SupplierName ?? "").Trim();
        data.ContactName = (data.ContactName ?? "").Trim();
        data.Province = (data.Province ?? "").Trim();
        data.Address = (data.Address ?? "").Trim();
        data.Phone = (data.Phone ?? "").Trim();
        data.Email = (data.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(data.SupplierName) || string.IsNullOrWhiteSpace(data.Email))
            return false;

        return await supplierDB.UpdateAsync(data);
    }

    /// <summary>
    /// Xóa một nhà cung cấp dựa vào mã nhà cung cấp.
    /// </summary>
    public static async Task<bool> DeleteSupplierAsync(int supplierID)
    {
        if (await supplierDB.IsUsedAsync(supplierID))
            return false;

        return await supplierDB.DeleteAsync(supplierID);
    }

    /// <summary>
    /// Kiểm tra xem một nhà cung cấp có đang được sử dụng trong dữ liệu hay không.
    /// </summary>
    public static async Task<bool> IsUsedSupplierAsync(int supplierID)
    {
        return await supplierDB.IsUsedAsync(supplierID);
    }

    /// <summary>
    /// Kiểm tra xem email của nhà cung cấp có hợp lệ (không bị trùng với nhà cung cấp khác) không.
    /// </summary>
    public static async Task<bool> ValidateSupplierEmailAsync(string email, int supplierID = 0)
    {
        email = (email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await supplierDB.ValidateEmailAsync(email, supplierID);
    }

    #endregion

    #region Customer

    /// <summary>
    /// Tìm kiếm và lấy danh sách khách hàng dưới dạng phân trang.
    /// </summary>
    public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
    {
        return await customerDB.ListAsync(input);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một khách hàng dựa vào mã khách hàng.
    /// </summary>
    public static async Task<Customer?> GetCustomerAsync(int customerID)
    {
        return await customerDB.GetAsync(customerID);
    }

    /// <summary>
    /// Bổ sung một khách hàng mới vào hệ thống.
    /// </summary>
    public static async Task<int> AddCustomerAsync(Customer data)
    {
        if (data == null)
            return 0;
        data.CustomerName = (data.CustomerName ?? "").Trim();
        data.ContactName = (data.ContactName ?? "").Trim();
        data.Province = (data.Province ?? "").Trim();
        data.Address = (data.Address ?? "").Trim();
        data.Phone = (data.Phone ?? "").Trim();
        data.Email = (data.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(data.CustomerName) || string.IsNullOrWhiteSpace(data.Email))
            return 0;

        return await customerDB.AddAsync(data);
    }

    /// <summary>
    /// Cập nhật thông tin của một khách hàng.
    /// </summary>
    public static async Task<bool> UpdateCustomerAsync(Customer data)
    {
        if (data == null || data.CustomerID <= 0)
            return false;
        data.CustomerName = (data.CustomerName ?? "").Trim();
        data.ContactName = (data.ContactName ?? "").Trim();
        data.Province = (data.Province ?? "").Trim();
        data.Address = (data.Address ?? "").Trim();
        data.Phone = (data.Phone ?? "").Trim();
        data.Email = (data.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(data.CustomerName) || string.IsNullOrWhiteSpace(data.Email))
            return false;

        return await customerDB.UpdateAsync(data);
    }

    /// <summary>
    /// Xóa một khách hàng dựa vào mã khách hàng.
    /// </summary>
    public static async Task<bool> DeleteCustomerAsync(int customerID)
    {
        if (await customerDB.IsUsedAsync(customerID))
            return false;

        return await customerDB.DeleteAsync(customerID);
    }

    /// <summary>
    /// Kiểm tra xem một khách hàng có đang được sử dụng trong dữ liệu hay không.
    /// </summary>
    public static async Task<bool> IsUsedCustomerAsync(int customerID)
    {
        return await customerDB.IsUsedAsync(customerID);
    }

    /// <summary>
    /// Kiểm tra xem email của khách hàng có hợp lệ (không bị trùng với khách khác) không.
    /// </summary>
    public static async Task<bool> ValidatelCustomerEmailAsync(string email, int customerID = 0)
    {
        return await customerDB.ValidateEmailAsync(email, customerID);
    }

    #endregion

    #region Shipper

    /// <summary>
    /// Tìm kiếm và lấy danh sách người giao hàng dưới dạng phân trang.
    /// </summary>
    public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
    {
        return await shipperDB.ListAsync(input);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một người giao hàng dựa vào mã người giao hàng.
    /// </summary>
    public static async Task<Shipper?> GetShipperAsync(int shipperID)
    {
        return await shipperDB.GetAsync(shipperID);
    }

    /// <summary>
    /// Bổ sung một người giao hàng mới vào hệ thống.
    /// </summary>
    public static async Task<int> AddShipperAsync(Shipper data)
    {
        if (data == null)
            return 0;
        data.ShipperName = (data.ShipperName ?? "").Trim();
        data.Phone = (data.Phone ?? "").Trim();
        if (string.IsNullOrWhiteSpace(data.ShipperName))
            return 0;

        return await shipperDB.AddAsync(data);
    }

    /// <summary>
    /// Cập nhật thông tin của một người giao hàng.
    /// </summary>
    public static async Task<bool> UpdateShipperAsync(Shipper data)
    {
        if (data == null || data.ShipperID <= 0)
            return false;
        data.ShipperName = (data.ShipperName ?? "").Trim();
        data.Phone = (data.Phone ?? "").Trim();
        if (string.IsNullOrWhiteSpace(data.ShipperName))
            return false;

        return await shipperDB.UpdateAsync(data);
    }

    /// <summary>
    /// Xóa một người giao hàng dựa vào mã người giao hàng.
    /// </summary>
    public static async Task<bool> DeleteShipperAsync(int shipperID)
    {
        if (await shipperDB.IsUsedAsync(shipperID))
            return false;

        return await shipperDB.DeleteAsync(shipperID);
    }

    /// <summary>
    /// Kiểm tra xem một người giao hàng có đang được sử dụng trong dữ liệu hay không.
    /// </summary>
    public static async Task<bool> IsUsedShipperAsync(int shipperID)
    {
        return await shipperDB.IsUsedAsync(shipperID);
    }

    #endregion
}
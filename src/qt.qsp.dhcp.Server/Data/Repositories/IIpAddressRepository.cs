using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public interface IIpAddressRepository
{
    Task<IpAddressStatus?> GetByIpAddressAsync(string ipAddress);
    Task<List<IpAddressStatus>> GetAllAsync();
    Task<IpAddressStatus> AddOrUpdateAsync(IpAddressStatus status);
    Task DeleteAsync(string ipAddress);
}

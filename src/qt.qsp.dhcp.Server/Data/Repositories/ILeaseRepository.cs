using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public interface ILeaseRepository
{
    Task<DhcpLease?> GetByIdAsync(int id);
    Task<DhcpLease?> GetByIpAddressAsync(string ipAddress);
    Task<DhcpLease?> GetByMacAddressAsync(string macAddress);
    Task<List<DhcpLease>> GetAllAsync();
    Task<List<DhcpLease>> GetActiveAsync();
    Task<DhcpLease> AddAsync(DhcpLease lease);
    Task UpdateAsync(DhcpLease lease);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string ipAddress);
}

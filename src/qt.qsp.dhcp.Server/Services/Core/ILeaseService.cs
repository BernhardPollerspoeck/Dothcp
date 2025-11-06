using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Services.Core;

public interface ILeaseService
{
    Task<DhcpLease?> GetLeaseAsync(string ipAddress);
    Task<DhcpLease?> GetLeaseByMacAsync(string macAddress);
    Task<List<DhcpLease>> GetAllLeasesAsync();
    Task<List<DhcpLease>> GetActiveLeasesAsync();
    Task<bool> UpdateLeaseAsync(DhcpLease lease);
    Task<bool> RevokeLeaseAsync(string ipAddress);
    Task<bool> IsExpiredAsync(string ipAddress);
    Task<string?> GetMacAddressAsync(string ipAddress);
}

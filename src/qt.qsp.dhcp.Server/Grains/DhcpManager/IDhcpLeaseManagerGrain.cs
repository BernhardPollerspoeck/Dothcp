using Orleans;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[Alias("IDhcpLeaseManagerGrain")]
public interface IDhcpLeaseManagerGrain : IGrainWithStringKey
{
    Task<DhcpLease?> GetLeaseByIp(string ipAddress);
    Task<DhcpLease?> GetLeaseByMac(string macAddress);
    Task<IEnumerable<DhcpLease>> GetLeasesByStatus(LeaseStatus status);
    Task<bool> AddOrUpdateLease(DhcpLease lease);
    Task<bool> RemoveLease(string ipAddress);
    Task<IEnumerable<DhcpLease>> GetAllLeases();
    Task PerformLeasesMaintenance();
}
using Orleans;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[Alias("IDhcpLeaseGrain")]
public interface IDhcpLeaseGrain : IGrainWithStringKey
{
    // Primary key is the IP address
    Task<DhcpLease?> GetLease();
    Task<bool> UpdateLease(DhcpLease lease);
    Task<bool> RevokeLease();
    Task<bool> IsExpired();
    Task<string?> GetMacAddress();
}
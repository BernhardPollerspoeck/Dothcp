using Orleans;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public interface ILeaseGrainSearchService
{
    Task<DhcpLease?> FindLeaseByMac(IGrainFactory grainFactory, string macAddress, string ipRange);
}
namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public interface ILeaseGrainSearchService
{
    Task<DhcpLease?> FindLeaseByMac(string macAddress, string ipRange);
}
namespace qt.qsp.dhcp.Server.Services;

public interface ILeaseGrainSearchService
{
    Task<DhcpLease?> FindLeaseByMac(string macAddress, string ipRange);
}
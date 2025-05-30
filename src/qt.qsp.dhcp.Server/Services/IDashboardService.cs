using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net.NetworkInformation;

namespace qt.qsp.dhcp.Server.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
}

public class DashboardData
{
    public ServerStatus ServerStatus { get; set; } = new();
    public LeaseStatistics LeaseStatistics { get; set; } = new();
    public List<DhcpLease> RecentLeases { get; set; } = [];
    public DhcpNetworkInfo DhcpNetwork { get; set; } = new();
}

public class ServerStatus
{
    public TimeSpan Uptime { get; set; }
    public ServerState State { get; set; } = ServerState.Running;
    public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; } = [];
}

public class LeaseStatistics
{
    public int TotalAddresses { get; set; }
    public int LeasedAddresses { get; set; }
    public int ReservedAddresses { get; set; }
    public int AvailableAddresses => TotalAddresses - LeasedAddresses - ReservedAddresses;
    public double UtilizationPercentage => TotalAddresses > 0 ? (double)LeasedAddresses / TotalAddresses * 100 : 0;
}

public class NetworkInterfaceInfo
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public OperationalStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActiveDhcpInterface { get; set; } = false;
}

public class DhcpNetworkInfo
{
    public string NetworkAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public string RouterAddress { get; set; } = string.Empty;
    public string NetworkCidr { get; set; } = string.Empty;
}

public enum ServerState
{
    Running,
    Stopped,
    Error
}
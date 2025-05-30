using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace qt.qsp.dhcp.Server.Services;

public class DashboardService : IDashboardService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILeaseGrainSearchService _leaseSearchService;
    private readonly ILogger<DashboardService> _logger;
    private static readonly DateTime _serverStartTime = DateTime.UtcNow;

    public DashboardService(
        IGrainFactory grainFactory,
        ILeaseGrainSearchService leaseSearchService,
        ILogger<DashboardService> logger)
    {
        _grainFactory = grainFactory;
        _leaseSearchService = leaseSearchService;
        _logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        try
        {
            var dashboardData = new DashboardData
            {
                ServerStatus = await GetServerStatusAsync(),
                LeaseStatistics = await GetLeaseStatisticsAsync(),
                RecentLeases = await GetRecentLeasesAsync()
            };

            return dashboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return new DashboardData(); // Return empty data on error
        }
    }

    private Task<ServerStatus> GetServerStatusAsync()
    {
        var uptime = DateTime.UtcNow - _serverStartTime;
        var networkInterfaces = GetNetworkInterfaces();

        var serverStatus = new ServerStatus
        {
            Uptime = uptime,
            State = ServerState.Running, // For now, if we're executing, we're running
            NetworkInterfaces = networkInterfaces
        };

        return Task.FromResult(serverStatus);
    }

    private List<NetworkInterfaceInfo> GetNetworkInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>();

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                try
                {
                    var ipProps = ni.GetIPProperties();
                    var unicastAddress = ipProps.UnicastAddresses
                        .FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    
                    var ipAddress = unicastAddress?.Address?.ToString() ?? "No IP";
                    var subnetMask = unicastAddress?.IPv4Mask?.ToString() ?? "Unknown";

                    interfaces.Add(new NetworkInterfaceInfo
                    {
                        Name = ni.Name,
                        IpAddress = ipAddress,
                        SubnetMask = subnetMask,
                        Status = ni.OperationalStatus,
                        Description = ni.Description
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error getting properties for network interface {Name}", ni.Name);
                    
                    // Add interface with limited info if we can't get all properties
                    interfaces.Add(new NetworkInterfaceInfo
                    {
                        Name = ni.Name,
                        IpAddress = "Unknown",
                        SubnetMask = "Unknown",
                        Status = OperationalStatus.Unknown,
                        Description = ni.Description
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving network interfaces");
        }

        return interfaces;
    }

    private async Task<LeaseStatistics> GetLeaseStatisticsAsync()
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, you'd iterate through configured IP ranges
            const string sampleIpRange = "192.168.1.";
            const int totalAddresses = 254; // .1 to .254
            var activeLeases = 0;

            // Sample check of first 50 IPs to get an estimate
            for (var i = 1; i <= 50; i++)
            {
                try
                {
                    var ipAddress = $"{sampleIpRange}{i}";
                    var leaseGrain = _grainFactory.GetGrain<IDhcpLeaseGrain>(ipAddress);
                    var lease = await leaseGrain.GetLease();
                    
                    if (lease != null && lease.Status == LeaseStatus.Active && !lease.IsExpired())
                    {
                        activeLeases++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error checking lease for IP {IpAddress}", $"{sampleIpRange}{i}");
                }
            }

            // Extrapolate the count (this is a rough estimate)
            var estimatedActiveLeases = (int)((double)activeLeases / 50 * totalAddresses);

            return new LeaseStatistics
            {
                TotalAddresses = totalAddresses,
                LeasedAddresses = estimatedActiveLeases,
                ReservedAddresses = 10 // Placeholder - would need configuration data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lease statistics");
            return new LeaseStatistics { TotalAddresses = 254, LeasedAddresses = 0, ReservedAddresses = 0 };
        }
    }

    private async Task<List<DhcpLease>> GetRecentLeasesAsync()
    {
        var recentLeases = new List<DhcpLease>();

        try
        {
            // Sample recent leases from a range
            const string sampleIpRange = "192.168.1.";
            
            for (var i = 1; i <= 20; i++) // Check first 20 IPs for recent activity
            {
                try
                {
                    var ipAddress = $"{sampleIpRange}{i}";
                    var leaseGrain = _grainFactory.GetGrain<IDhcpLeaseGrain>(ipAddress);
                    var lease = await leaseGrain.GetLease();
                    
                    if (lease != null && lease.Status == LeaseStatus.Active)
                    {
                        recentLeases.Add(lease);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error getting lease for IP {IpAddress}", $"{sampleIpRange}{i}");
                }
            }

            // Sort by most recent lease start time
            return recentLeases
                .OrderByDescending(l => l.LeaseStart)
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent leases");
            return [];
        }
    }
}
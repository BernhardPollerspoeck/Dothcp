using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net.NetworkInformation;
using System.Diagnostics;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Utilities;

namespace qt.qsp.dhcp.Server.Services;

public class DashboardService : IDashboardService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILeaseGrainSearchService _leaseSearchService;
    private readonly ILogger<DashboardService> _logger;
    private readonly ISettingsLoaderService _settingsLoader;
    private readonly INetworkUtilityService _networkUtility;
    private static readonly DateTime _serverStartTime = DateTime.UtcNow;

    public DashboardService(
        IGrainFactory grainFactory,
        ILeaseGrainSearchService leaseSearchService,
        ILogger<DashboardService> logger,
        ISettingsLoaderService settingsLoader,
        INetworkUtilityService networkUtility)
    {
        _grainFactory = grainFactory;
        _leaseSearchService = leaseSearchService;
        _logger = logger;
        _settingsLoader = settingsLoader;
        _networkUtility = networkUtility;
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
            // Get DHCP configuration from settings
            var minAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
            var maxAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);
            var routerBytes = await _settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);
            var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);

            // Build the network base (first 3 octets)
            var networkBase = string.Join('.', routerBytes[0..^1]);
            
            // Calculate actual address space
            var totalAddresses = maxAddress - minAddress + 1;
            
            // Calculate network and broadcast addresses to exclude reserved IPs
            var networkAddress = _networkUtility.CalculateNetworkAddress($"{networkBase}.0", subnetMask);
            var broadcastAddress = _networkUtility.CalculateBroadcastAddress($"{networkBase}.0", subnetMask);
            
            var activeLeases = 0;
            var reservedAddresses = 0;

            // Check all IPs in the configured range
            for (var i = minAddress; i <= maxAddress; i++)
            {
                try
                {
                    var ipAddress = $"{networkBase}.{i}";
                    
                    // Count reserved addresses (network and broadcast)
                    if (_networkUtility.IsReservedIp(ipAddress, networkAddress, broadcastAddress))
                    {
                        reservedAddresses++;
                        continue;
                    }
                    
                    var leaseGrain = _grainFactory.GetGrain<IDhcpLeaseGrain>(ipAddress);
                    var lease = await leaseGrain.GetLease();
                    
                    if (lease != null && lease.Status == LeaseStatus.Active && !lease.IsExpired())
                    {
                        activeLeases++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error checking lease for IP {IpAddress}", $"{networkBase}.{i}");
                }
            }

            return new LeaseStatistics
            {
                TotalAddresses = totalAddresses,
                LeasedAddresses = activeLeases,
                ReservedAddresses = reservedAddresses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lease statistics");
            return new LeaseStatistics { TotalAddresses = 0, LeasedAddresses = 0, ReservedAddresses = 0 };
        }
    }

    private async Task<List<DhcpLease>> GetRecentLeasesAsync()
    {
        var recentLeases = new List<DhcpLease>();

        try
        {
            // Get DHCP configuration from settings
            var minAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
            var maxAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);
            var routerBytes = await _settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);
            var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);

            // Build the network base (first 3 octets)
            var networkBase = string.Join('.', routerBytes[0..^1]);
            
            // Calculate network and broadcast addresses to skip reserved IPs
            var networkAddress = _networkUtility.CalculateNetworkAddress($"{networkBase}.0", subnetMask);
            var broadcastAddress = _networkUtility.CalculateBroadcastAddress($"{networkBase}.0", subnetMask);
            
            // Check all IPs in the configured range for active leases
            for (var i = minAddress; i <= maxAddress; i++)
            {
                try
                {
                    var ipAddress = $"{networkBase}.{i}";
                    
                    // Skip reserved addresses (network and broadcast)
                    if (_networkUtility.IsReservedIp(ipAddress, networkAddress, broadcastAddress))
                    {
                        continue;
                    }
                    
                    var leaseGrain = _grainFactory.GetGrain<IDhcpLeaseGrain>(ipAddress);
                    var lease = await leaseGrain.GetLease();
                    
                    if (lease != null && lease.Status == LeaseStatus.Active)
                    {
                        recentLeases.Add(lease);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error getting lease for IP {IpAddress}", $"{networkBase}.{i}");
                }
            }

            // Sort by most recent lease start time and take the most recent 10
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
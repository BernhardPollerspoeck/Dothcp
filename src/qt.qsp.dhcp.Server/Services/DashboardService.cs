using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net.NetworkInformation;
using System.Diagnostics;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Utilities;
using System.Net;

namespace qt.qsp.dhcp.Server.Services;

public class DashboardService : IDashboardService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILeaseGrainSearchService _leaseSearchService;
    private readonly ILogger<DashboardService> _logger;
    private readonly ISettingsLoaderService _settingsLoader;
    private readonly INetworkUtilityService _networkUtility;
    private readonly IDhcpServerService _dhcpServerService;
    private readonly IWebHostEnvironment _environment;
    private static readonly DateTime _serverStartTime = DateTime.UtcNow;

    public DashboardService(
        IGrainFactory grainFactory,
        ILeaseGrainSearchService leaseSearchService,
        ILogger<DashboardService> logger,
        ISettingsLoaderService settingsLoader,
        INetworkUtilityService networkUtility,
        IDhcpServerService dhcpServerService,
        IWebHostEnvironment environment)
    {
        _grainFactory = grainFactory;
        _leaseSearchService = leaseSearchService;
        _logger = logger;
        _settingsLoader = settingsLoader;
        _networkUtility = networkUtility;
        _dhcpServerService = dhcpServerService;
        _environment = environment;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        try
        {
            // Get DHCP network configuration first
            var dhcpNetwork = await GetDhcpNetworkInfoAsync();
            
            var dashboardData = new DashboardData
            {
                DhcpNetwork = dhcpNetwork,
                ServerStatus = await GetServerStatusAsync(dhcpNetwork),
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

    private async Task<DhcpNetworkInfo> GetDhcpNetworkInfoAsync()
    {
        try
        {
            var routerBytes = await _settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);
            var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
            
            // Check if settings exist - return empty info if not configured
            if (routerBytes == null || string.IsNullOrEmpty(subnetMask))
            {
                _logger.LogDebug("DHCP network settings not configured yet");
                return new DhcpNetworkInfo();
            }
            
            var routerAddress = string.Join('.', routerBytes);
            var networkAddress = _networkUtility.CalculateNetworkAddress(routerAddress, subnetMask);
            var cidrNotation = GetCidrNotation(subnetMask);
            
            return new DhcpNetworkInfo
            {
                NetworkAddress = networkAddress,
                SubnetMask = subnetMask,
                RouterAddress = routerAddress,
                NetworkCidr = $"{networkAddress}/{cidrNotation}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DHCP network info");
            return new DhcpNetworkInfo();
        }
    }

    private int GetCidrNotation(string subnetMask)
    {
        try
        {
            var subnet = IPAddress.Parse(subnetMask);
            var bytes = subnet.GetAddressBytes();
            var binaryString = string.Concat(bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            return binaryString.Count(c => c == '1');
        }
        catch
        {
            return 24; // Default to /24 if we can't parse
        }
    }

    private Task<ServerStatus> GetServerStatusAsync(DhcpNetworkInfo dhcpNetwork)
    {
        var uptime = DateTime.UtcNow - _serverStartTime;
        var networkInterfaces = GetNetworkInterfaces(dhcpNetwork);

        var serverStatus = new ServerStatus
        {
            Uptime = uptime,
            State = _dhcpServerService.CurrentState,
            NetworkInterfaces = networkInterfaces
        };

        return Task.FromResult(serverStatus);
    }

    private List<NetworkInterfaceInfo> GetNetworkInterfaces(DhcpNetworkInfo dhcpNetwork)
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

                    // Check if this interface is the active DHCP interface
                    var isActiveDhcp = IsActiveDhcpInterface(ipAddress, subnetMask, dhcpNetwork);

                    interfaces.Add(new NetworkInterfaceInfo
                    {
                        Name = ni.Name,
                        IpAddress = ipAddress,
                        SubnetMask = subnetMask,
                        Status = ni.OperationalStatus,
                        Description = ni.Description,
                        IsActiveDhcpInterface = isActiveDhcp
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
                        Description = ni.Description,
                        IsActiveDhcpInterface = false
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

    private bool IsActiveDhcpInterface(string interfaceIp, string interfaceSubnet, DhcpNetworkInfo dhcpNetwork)
    {
        try
        {
            if (interfaceIp == "No IP" || interfaceIp == "Unknown" || string.IsNullOrEmpty(dhcpNetwork.RouterAddress))
                return false;

            // Check if the interface IP is on the same network as the DHCP router
            var interfaceNetwork = _networkUtility.CalculateNetworkAddress(interfaceIp, interfaceSubnet);
            var dhcpNetworkAddr = _networkUtility.CalculateNetworkAddress(dhcpNetwork.RouterAddress, dhcpNetwork.SubnetMask);
            
            return string.Equals(interfaceNetwork, dhcpNetworkAddr, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error determining if interface {InterfaceIp} is active DHCP interface", interfaceIp);
            return false;
        }
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

            // Check if settings exist - return empty statistics if not configured
            if (routerBytes == null || string.IsNullOrEmpty(subnetMask))
            {
                _logger.LogDebug("DHCP network settings not configured yet - returning empty lease statistics");
                return new LeaseStatistics { TotalAddresses = 0, LeasedAddresses = 0, ReservedAddresses = 0 };
            }

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

            // Check if settings exist - return empty list if not configured
            if (routerBytes == null || string.IsNullOrEmpty(subnetMask))
            {
                _logger.LogDebug("DHCP network settings not configured yet - returning empty recent leases");
                return recentLeases;
            }

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

    public async Task<List<DhcpLease>> GetAllLeasesAsync()
    {
        var allLeases = new List<DhcpLease>();

        try
        {
            // Get DHCP configuration from settings
            var minAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
            var maxAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);
            var routerBytes = await _settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);
            var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);

            // Check if settings exist - add test leases for development if not configured
            if (routerBytes == null || string.IsNullOrEmpty(subnetMask))
            {
                _logger.LogDebug("DHCP network settings not configured yet - checking for development test leases");
                
                // Add test leases for development environment
                if (_environment.IsDevelopment())
                {
                    allLeases.AddRange(GetTestLeases());
                    _logger.LogInformation("Added {Count} test leases for development", allLeases.Count);
                }
                
                return allLeases;
            }

            // Build the network base (first 3 octets)
            var networkBase = string.Join('.', routerBytes[0..^1]);
            
            // Calculate network and broadcast addresses to skip reserved IPs
            var networkAddress = _networkUtility.CalculateNetworkAddress($"{networkBase}.0", subnetMask);
            var broadcastAddress = _networkUtility.CalculateBroadcastAddress($"{networkBase}.0", subnetMask);
            
            // Check all IPs in the configured range for leases
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
                    
                    if (lease != null)
                    {
                        allLeases.Add(lease);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error getting lease for IP {IpAddress}", $"{networkBase}.{i}");
                }
            }

            // Add test leases for development environment (in addition to real leases)
            if (_environment.IsDevelopment() && allLeases.Count < 5)
            {
                var testLeases = GetTestLeases();
                allLeases.AddRange(testLeases);
                _logger.LogInformation("Added {Count} additional test leases for development", testLeases.Count);
            }

            return allLeases.OrderByDescending(l => l.LeaseStart).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all leases");
            
            // Return test leases for development even on error
            if (_environment.IsDevelopment())
            {
                return GetTestLeases();
            }
            
            return [];
        }
    }

    private List<DhcpLease> GetTestLeases()
    {
        var testLeases = new List<DhcpLease>();
        var random = new Random();
        var hostnames = new[] { "laptop-001", "desktop-pc", "mobile-phone", "tablet-device", "smart-tv", "printer-hp", "router-wifi", "camera-ip" };
        var macPrefixes = new[] { "00:1B:44", "00:50:56", "08:00:27", "52:54:00", "00:16:3E" };

        for (int i = 0; i < 8; i++)
        {
            var macPrefix = macPrefixes[random.Next(macPrefixes.Length)];
            var macSuffix = $"{random.Next(0, 256):X2}:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}";
            var hostname = hostnames[i % hostnames.Length];
            var ipAddress = IPAddress.Parse($"192.168.1.{100 + i}");
            
            var leaseStart = DateTime.UtcNow.AddHours(-random.Next(1, 24));
            var leaseDuration = TimeSpan.FromHours(random.Next(8, 48));
            
            var status = i switch
            {
                7 => LeaseStatus.Expired, // One expired lease
                6 => LeaseStatus.Renewed, // One renewed lease
                _ => LeaseStatus.Active    // Rest are active
            };

            // For expired lease, make sure it's actually expired
            if (status == LeaseStatus.Expired)
            {
                leaseStart = DateTime.UtcNow.AddHours(-25);
                leaseDuration = TimeSpan.FromHours(24);
            }

            var testLease = new DhcpLease
            {
                IpAddress = ipAddress,
                MacAddress = $"{macPrefix}:{macSuffix}",
                HostName = hostname,
                LeaseStart = leaseStart,
                LeaseDuration = leaseDuration,
                Status = status,
                Subnet = IPAddress.Parse("255.255.255.0"),
                Router = IPAddress.Parse("192.168.1.1"),
                DhcpServer = IPAddress.Parse("192.168.1.1"),
                DnsServers = new List<IPAddress> 
                { 
                    IPAddress.Parse("8.8.8.8"), 
                    IPAddress.Parse("8.8.4.4") 
                }
            };

            testLeases.Add(testLease);
        }

        return testLeases;
    }
}
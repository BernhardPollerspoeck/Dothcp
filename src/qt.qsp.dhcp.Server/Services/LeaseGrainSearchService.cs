using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Services.Core;
using qt.qsp.dhcp.Server.Utilities;

namespace qt.qsp.dhcp.Server.Services;

public class LeaseGrainSearchService : ILeaseGrainSearchService
{
    private readonly ILogger<LeaseGrainSearchService> _logger;
    private readonly ISettingsLoaderService _settingsLoader;
    private readonly INetworkUtilityService _networkUtility;
    private readonly ILeaseService _leaseService;

    public LeaseGrainSearchService(
        ILogger<LeaseGrainSearchService> logger,
        ISettingsLoaderService settingsLoader,
        INetworkUtilityService networkUtility,
        ILeaseService leaseService)
    {
        _logger = logger;
        _settingsLoader = settingsLoader;
        _networkUtility = networkUtility;
        _leaseService = leaseService;
    }

    // Implementation to find a lease by MAC address
    public async Task<DhcpLease?> FindLeaseByMac(string macAddress, string ipRange)
    {
        try
        {
            // Use LeaseService directly to find by MAC
            var lease = await _leaseService.GetLeaseByMacAsync(macAddress);

            if (lease != null && !lease.IsExpired())
            {
                // Convert from Models.DhcpLease
                return ConvertToGrainModel(lease);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for lease by MAC address {MacAddress}", macAddress);
        }

        return null;
    }

    private DhcpLease ConvertToGrainModel(Models.DhcpLease modelLease)
    {
        var grainLease = new DhcpLease
        {
            MacAddress = modelLease.MacAddress,
            IpAddressString = modelLease.IpAddressString,
            HostName = modelLease.HostName,
            LeaseDuration = modelLease.LeaseDuration,
            LeaseStart = modelLease.LeaseStart,
            Status = (LeaseStatus)modelLease.Status,
            SubnetString = modelLease.SubnetString,
            RouterString = modelLease.RouterString,
            DhcpServerString = modelLease.DhcpServerString
        };

        // Convert DNS servers
        if (!string.IsNullOrEmpty(modelLease.DnsServerStringsJson))
        {
            var dnsServers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(modelLease.DnsServerStringsJson);
            if (dnsServers != null)
            {
                grainLease.DnsServerStrings = dnsServers;
            }
        }

        return grainLease;
    }
}
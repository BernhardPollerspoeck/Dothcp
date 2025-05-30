using Orleans;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Utilities;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class LeaseGrainSearchService : ILeaseGrainSearchService
{
    private readonly ILogger<LeaseGrainSearchService> _logger;
    private readonly ISettingsLoaderService _settingsLoader;
    private readonly INetworkUtilityService _networkUtility;
    
    public LeaseGrainSearchService(
        ILogger<LeaseGrainSearchService> logger,
        ISettingsLoaderService settingsLoader,
        INetworkUtilityService networkUtility)
    {
        _logger = logger;
        _settingsLoader = settingsLoader;
        _networkUtility = networkUtility;
    }
    
    // Implementation to find a lease by MAC address
    public async Task<DhcpLease?> FindLeaseByMac(IGrainFactory grainFactory, string macAddress, string ipRange)
    {
        var foundLeases = new List<DhcpLease>();

        try
        {
            // Get DHCP configuration from settings
            var minAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
            var maxAddress = await _settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);

            // Iterate through the configured IP range instead of hardcoded 1-254
            for (var i = minAddress; i <= maxAddress; i++)
            {
                var ipToCheck = $"{ipRange}{i}";
                var leaseGrain = grainFactory.GetGrain<IDhcpLeaseGrain>(ipToCheck);
                
                // Check if this grain has the MAC we're looking for
                var mac = await leaseGrain.GetMacAddress();
                if (mac == macAddress)
                {
                    var lease = await leaseGrain.GetLease();
                    if (lease != null)
                    {
                        foundLeases.Add(lease);
                        break; // Found what we're looking for, exit early
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for lease by MAC address {MacAddress} in range {IpRange}", macAddress, ipRange);
        }

        // Return the first active lease found for this MAC
        return foundLeases
            .OrderByDescending(l => l.LeaseStart) // Get the newest lease
            .FirstOrDefault(l => l.Status != LeaseStatus.Expired);
    }
}
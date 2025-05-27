using Orleans;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class LeaseGrainSearchService : ILeaseGrainSearchService
{
    private readonly ILogger<LeaseGrainSearchService> _logger;
    
    public LeaseGrainSearchService(ILogger<LeaseGrainSearchService> logger)
    {
        _logger = logger;
    }
    
    // Implementation to find a lease by MAC address
    public async Task<DhcpLease?> FindLeaseByMac(IGrainFactory grainFactory, string macAddress, string ipRange)
    {
        var foundLeases = new List<DhcpLease>();

        // Iterate through potential IPs in the range
        // This is a simple implementation example - could be optimized or expanded to cover more ranges
        for (int i = 1; i <= 254; i++)
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

        // Return the first active lease found for this MAC
        return foundLeases
            .OrderByDescending(l => l.LeaseStart) // Get the newest lease
            .FirstOrDefault(l => l.Status != LeaseStatus.Expired);
    }
}
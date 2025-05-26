using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[Serializable]
public class LeaseDatabase
{
    // Primary storage by IP address
    public Dictionary<string, DhcpLease> LeasesByIp { get; set; } = new();
    
    // Secondary index by MAC address -> IP address mapping
    public Dictionary<string, string> MacToIpMapping { get; set; } = new();

    // Add or update a lease
    public void AddOrUpdateLease(DhcpLease lease)
    {
        var ipString = lease.IpAddress.ToString();
        
        // If this MAC already has a different IP, remove that mapping
        if (MacToIpMapping.TryGetValue(lease.MacAddress, out var existingIp) && 
            existingIp != ipString)
        {
            if (LeasesByIp.ContainsKey(existingIp))
            {
                LeasesByIp.Remove(existingIp);
            }
        }
        
        // Update or add the lease and mappings
        LeasesByIp[ipString] = lease;
        MacToIpMapping[lease.MacAddress] = ipString;
    }

    // Get a lease by IP
    public DhcpLease? GetLeaseByIp(string ipAddress)
    {
        return LeasesByIp.TryGetValue(ipAddress, out var lease) ? lease : null;
    }

    // Get a lease by MAC
    public DhcpLease? GetLeaseByMac(string macAddress)
    {
        if (MacToIpMapping.TryGetValue(macAddress, out var ipAddress))
        {
            return GetLeaseByIp(ipAddress);
        }
        return null;
    }

    // Get all leases with a specific status
    public IEnumerable<DhcpLease> GetLeasesByStatus(LeaseStatus status)
    {
        return LeasesByIp.Values.Where(lease => lease.Status == status);
    }
    
    // Remove a lease by IP
    public bool RemoveLease(string ipAddress)
    {
        if (LeasesByIp.TryGetValue(ipAddress, out var lease))
        {
            // Remove from MAC mapping
            if (MacToIpMapping.ContainsKey(lease.MacAddress))
            {
                MacToIpMapping.Remove(lease.MacAddress);
            }
            
            // Remove from IP dictionary
            return LeasesByIp.Remove(ipAddress);
        }
        return false;
    }
    
    // Get all leases
    public IEnumerable<DhcpLease> GetAllLeases()
    {
        return LeasesByIp.Values;
    }
    
    // Check for and update expired leases
    public void CheckExpiredLeases()
    {
        foreach (var lease in LeasesByIp.Values)
        {
            if (lease.Status != LeaseStatus.Expired && lease.IsExpired())
            {
                lease.Expire();
            }
        }
    }
}
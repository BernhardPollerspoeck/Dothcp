using Orleans;
using Orleans.Runtime;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpLeaseGrain : Grain, IDhcpLeaseGrain
{
    private readonly ILogger<DhcpLeaseGrain> _logger;
    private readonly IPersistentState<DhcpLease> _leaseState;

    public DhcpLeaseGrain(
        [PersistentState("lease", "File")] IPersistentState<DhcpLease> leaseState,
        ILogger<DhcpLeaseGrain> logger)
    {
        _leaseState = leaseState;
        _logger = logger;
    }

    public Task<DhcpLease?> GetLease()
    {
        // Return null if the lease IP doesn't match the grain key (uninitialized)
        if (_leaseState.State.IpAddress == IPAddress.None || 
            _leaseState.State.IpAddress.ToString() != this.GetPrimaryKeyString())
        {
            return Task.FromResult<DhcpLease?>(null);
        }
        
        return Task.FromResult<DhcpLease?>(_leaseState.State);
    }

    public async Task<bool> UpdateLease(DhcpLease lease)
    {
        try
        {
            // Ensure the IP address matches this grain's key
            if (lease.IpAddress.ToString() != this.GetPrimaryKeyString())
            {
                _logger.LogError("Attempted to store lease with IP {providedIp} in grain with key {grainKey}", 
                    lease.IpAddress, this.GetPrimaryKeyString());
                return false;
            }
            
            _leaseState.State = lease;
            await _leaseState.WriteStateAsync();
            _logger.LogInformation("Lease for IP {ipAddress} updated for MAC {macAddress}", 
                lease.IpAddress, lease.MacAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lease for IP {ipAddress}", lease.IpAddress);
            return false;
        }
    }

    public async Task<bool> RevokeLease()
    {
        try
        {
            // Check if lease exists
            if (_leaseState.State.IpAddress == IPAddress.None)
            {
                return false;
            }
            
            _logger.LogInformation("Lease for IP {ipAddress} revoked from MAC {macAddress}", 
                _leaseState.State.IpAddress, _leaseState.State.MacAddress);
            
            // Reset state
            _leaseState.State = new DhcpLease { IpAddress = IPAddress.Parse(this.GetPrimaryKeyString()) };
            await _leaseState.WriteStateAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking lease for IP {ipAddress}", this.GetPrimaryKeyString());
            return false;
        }
    }

    public Task<bool> IsExpired()
    {
        // Check if lease exists and is expired
        if (_leaseState.State.IpAddress == IPAddress.None)
        {
            return Task.FromResult(true); // No lease means effectively expired
        }
        
        bool expired = _leaseState.State.IsExpired();
        
        // If expired but not marked as such, mark it
        if (expired && _leaseState.State.Status != LeaseStatus.Expired)
        {
            _leaseState.State.Expire();
            _leaseState.WriteStateAsync().Ignore();
        }
        
        return Task.FromResult(expired);
    }

    public Task<string?> GetMacAddress()
    {
        // Return null if no valid lease
        if (_leaseState.State.IpAddress == IPAddress.None || 
            string.IsNullOrEmpty(_leaseState.State.MacAddress))
        {
            return Task.FromResult<string?>(null);
        }
        
        return Task.FromResult<string?>(_leaseState.State.MacAddress);
    }
}
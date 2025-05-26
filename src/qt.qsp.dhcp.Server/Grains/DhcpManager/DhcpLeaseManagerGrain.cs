using Orleans;
using Orleans.Runtime;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpLeaseManagerGrain : Grain, IDhcpLeaseManagerGrain
{
    private readonly ILogger<DhcpLeaseManagerGrain> _logger;
    private readonly IPersistentState<LeaseDatabase> _leaseState;
    private IDisposable? _leaseMaintenanceTimer;

    public DhcpLeaseManagerGrain(
        [PersistentState("leaseDatabase", "File")] IPersistentState<LeaseDatabase> leaseState,
        ILogger<DhcpLeaseManagerGrain> logger)
    {
        _leaseState = leaseState;
        _logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Set up a timer to check for expired leases
        _leaseMaintenanceTimer = RegisterTimer(
            _ => PerformLeasesMaintenance(),
            null,
            TimeSpan.FromMinutes(5), // Initial delay
            TimeSpan.FromMinutes(5)); // Interval
        
        return base.OnActivateAsync(cancellationToken);
    }
    
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _leaseMaintenanceTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task<DhcpLease?> GetLeaseByIp(string ipAddress)
    {
        return Task.FromResult(_leaseState.State.GetLeaseByIp(ipAddress));
    }

    public Task<DhcpLease?> GetLeaseByMac(string macAddress)
    {
        return Task.FromResult(_leaseState.State.GetLeaseByMac(macAddress));
    }

    public Task<IEnumerable<DhcpLease>> GetLeasesByStatus(LeaseStatus status)
    {
        return Task.FromResult(_leaseState.State.GetLeasesByStatus(status));
    }

    public async Task<bool> AddOrUpdateLease(DhcpLease lease)
    {
        try
        {
            _leaseState.State.AddOrUpdateLease(lease);
            await _leaseState.WriteStateAsync();
            _logger.LogInformation("Lease for IP {ipAddress} added/updated for MAC {macAddress}", 
                lease.IpAddress, lease.MacAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/updating lease for IP {ipAddress}", lease.IpAddress);
            return false;
        }
    }

    public async Task<bool> RemoveLease(string ipAddress)
    {
        try
        {
            var result = _leaseState.State.RemoveLease(ipAddress);
            if (result)
            {
                await _leaseState.WriteStateAsync();
                _logger.LogInformation("Lease for IP {ipAddress} removed", ipAddress);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing lease for IP {ipAddress}", ipAddress);
            return false;
        }
    }

    public Task<IEnumerable<DhcpLease>> GetAllLeases()
    {
        return Task.FromResult(_leaseState.State.GetAllLeases());
    }

    public async Task PerformLeasesMaintenance()
    {
        try
        {
            _logger.LogDebug("Performing lease maintenance");
            bool stateChanged = false;
            
            // Check for expired leases
            var activeLeases = _leaseState.State.GetLeasesByStatus(LeaseStatus.Active).ToList();
            var renewedLeases = _leaseState.State.GetLeasesByStatus(LeaseStatus.Renewed).ToList();
            
            foreach (var lease in activeLeases.Concat(renewedLeases))
            {
                if (lease.IsExpired() && lease.Status != LeaseStatus.Expired)
                {
                    _logger.LogInformation("Lease for IP {ipAddress} expired", lease.IpAddress);
                    lease.Expire();
                    stateChanged = true;
                }
            }
            
            if (stateChanged)
            {
                await _leaseState.WriteStateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during lease maintenance");
        }
    }
}
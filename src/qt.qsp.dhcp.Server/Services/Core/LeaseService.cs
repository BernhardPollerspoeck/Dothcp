using Microsoft.Extensions.Logging;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Services.Core;

public class LeaseService : ILeaseService
{
    private readonly ILeaseRepository _leaseRepository;
    private readonly ILogger<LeaseService> _logger;

    public LeaseService(ILeaseRepository leaseRepository, ILogger<LeaseService> logger)
    {
        _leaseRepository = leaseRepository;
        _logger = logger;
    }

    public async Task<DhcpLease?> GetLeaseAsync(string ipAddress)
    {
        var lease = await _leaseRepository.GetByIpAddressAsync(ipAddress);

        // Return null if uninitialized
        if (lease == null || string.IsNullOrEmpty(lease.IpAddressString))
        {
            return null;
        }

        return lease;
    }

    public async Task<DhcpLease?> GetLeaseByMacAsync(string macAddress)
    {
        return await _leaseRepository.GetByMacAddressAsync(macAddress);
    }

    public async Task<List<DhcpLease>> GetAllLeasesAsync()
    {
        return await _leaseRepository.GetAllAsync();
    }

    public async Task<List<DhcpLease>> GetActiveLeasesAsync()
    {
        return await _leaseRepository.GetActiveAsync();
    }

    public async Task<bool> UpdateLeaseAsync(DhcpLease lease)
    {
        try
        {
            if (string.IsNullOrEmpty(lease.IpAddressString))
            {
                _logger.LogError("Attempted to store lease with empty IP address");
                return false;
            }

            // Check if lease exists
            var existing = await _leaseRepository.GetByIpAddressAsync(lease.IpAddressString);

            if (existing != null)
            {
                // Update existing lease
                existing.MacAddress = lease.MacAddress;
                existing.HostName = lease.HostName;
                existing.LeaseDuration = lease.LeaseDuration;
                existing.LeaseStart = lease.LeaseStart;
                existing.Status = lease.Status;
                existing.SubnetString = lease.SubnetString;
                existing.RouterString = lease.RouterString;
                existing.DhcpServerString = lease.DhcpServerString;
                existing.DnsServerStringsJson = lease.DnsServerStringsJson;

                await _leaseRepository.UpdateAsync(existing);
            }
            else
            {
                // Add new lease
                await _leaseRepository.AddAsync(lease);
            }

            _logger.LogInformation("Lease for IP {ipAddress} updated for MAC {macAddress}",
                lease.IpAddress, lease.MacAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lease for IP {ipAddress}", lease.IpAddressString);
            return false;
        }
    }

    public async Task<bool> RevokeLeaseAsync(string ipAddress)
    {
        try
        {
            var lease = await _leaseRepository.GetByIpAddressAsync(ipAddress);

            if (lease == null)
            {
                return false;
            }

            _logger.LogInformation("Lease for IP {ipAddress} revoked from MAC {macAddress}",
                lease.IpAddress, lease.MacAddress);

            await _leaseRepository.DeleteAsync(lease.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking lease for IP {ipAddress}", ipAddress);
            return false;
        }
    }

    public async Task<bool> IsExpiredAsync(string ipAddress)
    {
        var lease = await _leaseRepository.GetByIpAddressAsync(ipAddress);

        // No lease means effectively expired
        if (lease == null)
        {
            return true;
        }

        var expired = lease.IsExpired();

        // If expired but not marked as such, mark it
        if (expired && lease.Status != LeaseStatus.Expired)
        {
            lease.Expire();
            await _leaseRepository.UpdateAsync(lease);
        }

        return expired;
    }

    public async Task<string?> GetMacAddressAsync(string ipAddress)
    {
        var lease = await _leaseRepository.GetByIpAddressAsync(ipAddress);

        // Return null if no valid lease
        if (lease == null || string.IsNullOrEmpty(lease.MacAddress))
        {
            return null;
        }

        return lease.MacAddress;
    }
}

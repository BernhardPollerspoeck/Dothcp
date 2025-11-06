using Microsoft.Extensions.Logging;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Services.Core;

public class IpAddressService : IIpAddressService
{
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly ILogger<IpAddressService> _logger;

    public IpAddressService(IIpAddressRepository ipAddressRepository, ILogger<IpAddressService> logger)
    {
        _ipAddressRepository = ipAddressRepository;
        _logger = logger;
    }

    public async Task<EIpAddressStatus> GetStatusAsync(string ipAddress)
    {
        var status = await _ipAddressRepository.GetByIpAddressAsync(ipAddress);
        return status?.Status ?? EIpAddressStatus.Available;
    }

    public async Task SetStatusAsync(string ipAddress, EIpAddressStatus status, string? clientId = null)
    {
        try
        {
            var ipStatus = new IpAddressStatus
            {
                IpAddressString = ipAddress,
                Status = status,
                ClientId = clientId
            };

            await _ipAddressRepository.AddOrUpdateAsync(ipStatus);

            _logger.LogDebug("Set IP {IpAddress} status to {Status} for client {ClientId}",
                ipAddress, status, clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting status for IP {IpAddress}", ipAddress);
            throw;
        }
    }

    public async Task<IpAddressStatus?> GetIpAddressInfoAsync(string ipAddress)
    {
        return await _ipAddressRepository.GetByIpAddressAsync(ipAddress);
    }
}

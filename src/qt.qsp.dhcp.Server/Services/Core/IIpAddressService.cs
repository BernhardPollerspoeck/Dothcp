using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Services.Core;

public interface IIpAddressService
{
    Task<EIpAddressStatus> GetStatusAsync(string ipAddress);
    Task SetStatusAsync(string ipAddress, EIpAddressStatus status, string? clientId = null);
    Task<IpAddressStatus?> GetIpAddressInfoAsync(string ipAddress);
}

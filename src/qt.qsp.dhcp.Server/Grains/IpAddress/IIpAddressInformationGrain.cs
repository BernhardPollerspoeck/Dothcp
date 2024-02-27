namespace qt.qsp.dhcp.Server.Grains.IpAddress;

/// <summary>
/// Identifies by ip address
/// </summary>
[Alias("IIpAddressInformationGrain")]
public interface IIpAddressInformationGrain : IGrainWithStringKey
{
	[Alias("GetStatus")]
	Task<IpAddressStatus> GetStatus();
	[Alias("SetStatus")]
	Task SetStatus(EIpAddressStatus status, string? clientId);

}

namespace qt.qsp.dhcp.Server.Grains.IpAddress;

[GenerateSerializer]
[Alias("IpAddressStatus")]
public class IpAddressStatus
{
	[Id(0)]
	public EIpAddressStatus Status { get; set; }
	[Id(1)]
	public string? ClientId { get; set; }
}

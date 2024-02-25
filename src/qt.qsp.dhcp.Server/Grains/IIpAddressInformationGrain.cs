using Orleans.Runtime;

namespace qt.qsp.dhcp.Server.Grains;

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

public class IpAddressInformationGrain(
	[PersistentState("ipAddressInformation", "File")] IPersistentState<IpAddressStatus> state)
	: Grain, IIpAddressInformationGrain
{
	public Task<IpAddressStatus> GetStatus()
	{
		return Task.FromResult(state.State ?? new());
	}

	public Task SetStatus(EIpAddressStatus status, string? clientId)
	{
		throw new NotImplementedException();
	}
}

[GenerateSerializer]
[Alias("IpAddressStatus")]
public class IpAddressStatus
{
	[Id(0)]
	public EIpAddressStatus Status { get; set; }
	[Id(1)]
	public string? ClientId { get; set; }
}

public enum EIpAddressStatus
{
	Available,
	Offered,
	Claimed,
	Reserved,
}

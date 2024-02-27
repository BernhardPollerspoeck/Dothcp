using Orleans.Runtime;

namespace qt.qsp.dhcp.Server.Grains.IpAddress;

public class IpAddressInformationGrain(
	[PersistentState("ipAddressInformation", "File")] IPersistentState<IpAddressStatus> state,
	ILogger<IpAddressInformationGrain> logger)
	: Grain, IIpAddressInformationGrain
{
	#region IpAddressInformationGrain
	public Task<IpAddressStatus> GetStatus()
	{
		return Task.FromResult(state.State ?? new());
	}
	public Task SetStatus(EIpAddressStatus status, string? clientId)
	{
		logger.LogDebug("Set status {status} for client {clientId}", status, clientId);
		state.State.Status = status;
		state.State.ClientId = clientId;
		return state.WriteStateAsync();
	}
	#endregion
}

using Microsoft.AspNetCore.Authentication;
using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains;

[Alias("IDhcpLeaseGrain")]
public interface IDhcpLeaseGrain : IGrainWithStringKey
{
	[Alias("HandleMessage")]
	Task<DhcpMessage?> HandleMessage(DhcpMessage message);
}

public class DhcpLeaseGrain
	: Grain, IDhcpLeaseGrain
{
	//private readonly IPersistentState<DhcpLease> _state;

	public DhcpLeaseGrain(
		//[PersistentState("dhcp", "File")]
		//IPersistentState<DhcpLease> state
		)
	{
		//_state = state;
	}


	#region IDhcpLeaseGrain
	public async Task<DhcpMessage?> HandleMessage(DhcpMessage message)
	{
		var messageType = message.GetMessageType();

		return messageType switch
		{
			EMessageType.Discover => await HandleDiscover(message),
			EMessageType.Request => await HandleRequest(message),
			_ => null,
		};
	}
	#endregion

	#region message handling
	public async Task<DhcpMessage?> HandleDiscover(DhcpMessage message)
	{
		return null;

	}
	public async Task<DhcpMessage?> HandleRequest(DhcpMessage message)
	{
		//TODO: find the lease offer
		//not found: return null. we dont work with this client
		//found:
		//- create lease from offer
		//- respond with ack
		return null;
	}
	#endregion

}

public class DhcpLease
{


	public IPAddress? Subnet { get; set; }//TODO: how to get this value
	public IPAddress? Router { get; set; }//TODO: how to get this value
	public TimeSpan? LeaseDuration { get; set; }//TODO: default 1 day
	public DateTime? LeaseTime { get; set; }//TODO: from when on do we need to count?
	public IPAddress? DhcpServer { get; set; }//TODO: will be us, but we store in case of different user configuration

	public IList<IPAddress> DnsServers { get; set; } = [];


}

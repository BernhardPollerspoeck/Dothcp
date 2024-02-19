using Microsoft.AspNetCore.Authentication;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
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

		//TODO create offer
		//store offer
		//integrate offer and settings

		//TODO: propper values
		return new DhcpMessage
		{
			Direction = EMessageDirection.Reply,
			HardwareType = message.HardwareType,
			ClientIdLength = message.ClientIdLength,
			Hops = 0,
			TransactionId = message.TransactionId,
			ResponseCastType = message.ResponseCastType,
			ClientIpAdress = BitConverter.ToUInt32(IPAddress.Parse("0.0.0.0").GetAddressBytes()),
			AssigneeAdress = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.200").GetAddressBytes()),
			ServerIpAdress = BitConverter.ToUInt32(IPAddress.Parse("127.0.0.1").GetAddressBytes()),
			ClientHardwareAdresses = message.ClientHardwareAdresses,
			Options = new DhcpOptionsBuilder()
				.AddMessageType(EMessageType.Offer)
				.AddServerIdentifier("123.123.123.123")
				.AddAddressLeaseTime(TimeSpan.FromHours(24))
				.AddRenewalTime(TimeSpan.FromHours(12))
				.AddRebindingTime(TimeSpan.FromHours(21))
				.AddSubnetMask("255.255.255.0")
				.AddBroadcastAddressOption("192.168.0.255")
				.AddRouterOption(["192.168.0.1"])
				.AddDnsServerOptions(["192.168.0.1"])
				.AddInterfaceMtuOption(1500)
				.AddTimeOffset(DateTime.Now - DateTime.UtcNow)
				.Build()
		};
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

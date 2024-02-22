using Microsoft.AspNetCore.Authentication;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using System.Net;
using System.Net.Sockets;

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

	public static IPAddress GetLocalIpAddress()
	{
		return Dns
			.GetHostEntry(Dns.GetHostName())
			.AddressList
			.FirstOrDefault(a => a is { AddressFamily: AddressFamily.InterNetwork })
			?? throw new Exception("No network adapters with an IPv4 address in the system!");
	}
	#region message handling
	public async Task<DhcpMessage?> HandleDiscover(DhcpMessage message)
	{
		//choose:
		//- previous
		//-requested(option)
		//-from pool
		var localIp = GetLocalIpAddress();

		//TODO: ParameterRequestList
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
			AssigneeAdress = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.200").GetAddressBytes()),//TODO: offer
			ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
			ClientHardwareAdress = message.ClientHardwareAdress,
			Options = new DhcpOptionsBuilder()
				.AddAddressLeaseTime(await GrainFactory.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_TIME).GetValue<TimeSpan>())
				.AddMessageType(EMessageType.Offer)
				.AddServerIdentifier(localIp)
				.AddRenewalTime(await GrainFactory.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_RENEWAL).GetValue<TimeSpan>())
				.AddRebindingTime(await GrainFactory.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_REBINDING).GetValue<TimeSpan>())
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

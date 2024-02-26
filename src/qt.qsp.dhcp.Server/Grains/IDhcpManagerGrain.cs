using Microsoft.AspNetCore.Authentication;
using Orleans.Runtime;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using System.Net;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;

namespace qt.qsp.dhcp.Server.Grains;

/// <summary>
/// Identified by client address
/// </summary>
[Alias("IDhcpManagerGrain")]
public interface IDhcpManagerGrain : IGrainWithStringKey
{
	[Alias("HandleMessage")]
	Task<DhcpMessage?> HandleMessage(DhcpMessage message);
}

public class DhcpManagerGrain(
	[PersistentState("clientInfo", "File")] IPersistentState<ClientInfo> state)
	: Grain, IDhcpManagerGrain
{
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
		//get previously assigned ip
		if (state.State is { HasAssignedAddress: true, Address: not null })
		{
			var previousOfferGrain = GrainFactory.GetGrain<IIpAddressInformationGrain>(state.State.Address);
			var status = await previousOfferGrain.GetStatus();
			if (status is { Status: EIpAddressStatus.Claimed or EIpAddressStatus.Offered }
				&& status.ClientId == this.GetPrimaryKeyString())
			{
				state.State.Address = previousOfferGrain.GetPrimaryKeyString();
				state.State.State = EClientState.Offered;
				await state.WriteStateAsync();

				await previousOfferGrain.SetStatus(EIpAddressStatus.Offered, this.GetPrimaryKeyString());
				return await CreateOffer(message, state.State.Address);
			}
		}

		var minAddress = await GrainFactory.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW).GetValue<byte>();
		var maxAddress = await GrainFactory.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH).GetValue<byte>();
		var router = (await GrainFactory.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER).GetValue<byte[]>())[0..^1];

		//give the client a requested if available
		if (message.HasOption(EOption.AdressRequest))
		{
			var requestedAddress = message.GetRequestedAddress();

		}


		//get one by random
		for (var i = minAddress; i <= maxAddress; i++)
		{
			var addressInfo = GrainFactory.GetGrain<IIpAddressInformationGrain>($"{string.Join('.', router)}.{i}");
			var addressStatus = await addressInfo.GetStatus();
			if (addressStatus is not { Status: EIpAddressStatus.Available })
			{
				continue;
			}
			return await CreateOffer(message, addressInfo.GetPrimaryKeyString());
		}




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

	#region helpers
	private async Task<DhcpMessage> CreateOffer(DhcpMessage incomming, string address)
	{
		var localIp = GetLocalIpAddress();


		var optionsBuilder = new DhcpOptionsBuilder()
			.AddAddressLeaseTime(await GrainFactory
				.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_TIME)
				.GetValue<TimeSpan>())
			.AddMessageType(EMessageType.Offer)
			.AddServerIdentifier(localIp)
			.AddRenewalTime(await GrainFactory
				.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_RENEWAL)
				.GetValue<TimeSpan>())
			.AddRebindingTime(await GrainFactory
				.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_REBINDING)
				.GetValue<TimeSpan>())
			.AddTimeOffset(DateTime.Now - DateTime.UtcNow);

		var parameters = incomming.GetParameterList();
		foreach (EOption item in parameters)
		{
			switch (item)
			{
				case EOption.SubnetMask:
					optionsBuilder.AddSubnetMask(await GrainFactory
						.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET)
						.GetValue<string>());
					break;

				case EOption.RouterOptions:
					optionsBuilder.AddRouterOption(await GrainFactory
						.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER)
						.GetValue<string[]>());
					break;

				case EOption.DnsServerOptions:
					optionsBuilder.AddDnsServerOptions(await GrainFactory
						.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_DNS)
						.GetValue<string[]>());
					break;

				case EOption.HostName:
					optionsBuilder.AddHostName("Affe mit Waffe");//TODO: how to optain?
					break;

				case EOption.DomainName:
					optionsBuilder.AddDomainName("HomeDomain");//TODO: how to optain?
					break;

				case EOption.BroadcastAddressOption:
					optionsBuilder.AddBroadcastAddressOption("192.168.0.1");//TODO: how to optain?
					break;

				case EOption.NtpServers:
					optionsBuilder.AddNtpServerOptions(await GrainFactory
						.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_NTP_SERVERS)
						.GetValue<string[]>());
					break;
			}

		}


		return new DhcpMessage
		{
			Direction = EMessageDirection.Reply,
			HardwareType = incomming.HardwareType,
			ClientIdLength = incomming.ClientIdLength,
			Hops = 0,
			TransactionId = incomming.TransactionId,
			ResponseCastType = incomming.ResponseCastType,
			ClientIpAdress = BitConverter
						.ToUInt32(IPAddress
							.Parse("0.0.0.0")//TODO: value
							.GetAddressBytes()),
			AssigneeAdress = BitConverter
						.ToUInt32(IPAddress
							.Parse(address)
							.GetAddressBytes()),
			ServerIpAdress = BitConverter
						.ToUInt32(localIp
							.GetAddressBytes()),
			ClientHardwareAdress = incomming.ClientHardwareAdress,
			Options = optionsBuilder.Build()
		};
	}


	public static IPAddress GetLocalIpAddress()
	{
		return Dns
			.GetHostEntry(Dns.GetHostName())
			.AddressList
			.FirstOrDefault(a => a is { AddressFamily: AddressFamily.InterNetwork })
			?? throw new Exception("No network adapters with an IPv4 address in the system!");
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

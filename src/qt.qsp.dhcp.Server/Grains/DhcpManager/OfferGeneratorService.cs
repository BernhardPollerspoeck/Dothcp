using Orleans.Runtime;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.IpAddress;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using qt.qsp.dhcp.Server.Services;
using System.Net;
using System.Net.Sockets;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class OfferGeneratorService(
	ILogger<OfferGeneratorService> logger,
	ISettingsLoaderService settingsLoader,
	IGrainFactory grainFactory)
	: IOfferGeneratorService
{
	#region IOfferGeneratorService
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromPreviousIp(DhcpMessage message, IPersistentState<ClientInfo> clientInfo, string clientId)
	{
		if (clientInfo.State is { HasAssignedAddress: true, Address: not null })
		{
			var previousOfferGrain = grainFactory.GetGrain<IIpAddressInformationGrain>(clientInfo.State.Address);
			var status = await previousOfferGrain.GetStatus();
			if (status is { Status: EIpAddressStatus.Claimed or EIpAddressStatus.Offered }
				&& status.ClientId == clientId)
			{
				clientInfo.State.Address = previousOfferGrain.GetPrimaryKeyString();
				clientInfo.State.State = EClientState.Offered;
				await clientInfo.WriteStateAsync();

				await previousOfferGrain.SetStatus(EIpAddressStatus.Offered, clientId);

				logger.LogInformation("Create offer for {clientAddress} based on previously assigned address", clientInfo.State.Address);
				var offer = await CreateOffer(message, clientInfo.State.Address);
				return (offer is not null, offer);
			}
		}
		return (false, null);
	}
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromRandomIp(DhcpMessage message, IPersistentState<ClientInfo> clientInfo, string clientId)
	{
		var minAddress = await settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
		var maxAddress = await settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);
		var router = (await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))[0..^1];

		for (var i = minAddress; i <= maxAddress; i++)
		{
			var addressInfo = grainFactory.GetGrain<IIpAddressInformationGrain>($"{string.Join('.', router)}.{i}");
			var addressStatus = await addressInfo.GetStatus();
			if (addressStatus is not { Status: EIpAddressStatus.Available })
			{
				continue;
			}

			await addressInfo.SetStatus(EIpAddressStatus.Offered, clientId);

			logger.LogInformation("Create offer for {clientAddress} based on random address", addressInfo.GetPrimaryKeyString());
			var offer = await CreateOffer(message, addressInfo.GetPrimaryKeyString());
			return (offer is not null, offer);
		}
		return (false, null);
	}
	#endregion

	#region helpers
	//TODO: offer builder 
	private async Task<DhcpMessage> CreateOffer(DhcpMessage incomming, string address)
	{
		var localIp = GetLocalIpAddress();


		var optionsBuilder = new DhcpOptionsBuilder()
			.AddAddressLeaseTime(await settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_TIME))
			.AddMessageType(EMessageType.Offer)
			.AddServerIdentifier(localIp)
			.AddRenewalTime(await settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_RENEWAL))
			.AddRebindingTime(await settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_REBINDING))
			.AddTimeOffset(DateTime.Now - DateTime.UtcNow);

		var parameters = incomming.GetParameterList();
		foreach (var item in parameters.Cast<EOption>())
		{
			switch (item)
			{
				case EOption.SubnetMask:
					optionsBuilder.AddSubnetMask(await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET));
					break;

				case EOption.RouterOptions:
					optionsBuilder.AddRouterOption(await settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_ROUTER));
					break;

				case EOption.DnsServerOptions:
					optionsBuilder.AddDnsServerOptions(await settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_DNS));
					break;

				case EOption.HostName:
					//TODO: maybe later optionsBuilder.AddHostName("Affe mit Waffe");
					break;

				case EOption.DomainName:
					//TODO: later with dns optionsBuilder.AddDomainName("HomeDomain");
					break;

				case EOption.BroadcastAddressOption:
					optionsBuilder.AddBroadcastAddressOption("192.168.0.255");//TODO: calculate based on router
					break;

				case EOption.NtpServers:
					optionsBuilder.AddNtpServerOptions(await settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_NTP_SERVERS));
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

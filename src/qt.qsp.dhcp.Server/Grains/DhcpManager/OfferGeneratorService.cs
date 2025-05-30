using Orleans.Runtime;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.IpAddress;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Utilities;
using System.Net;
using System.Net.Sockets;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class OfferGeneratorService(
	ILogger<OfferGeneratorService> logger,
	ISettingsLoaderService settingsLoader,
	IGrainFactory grainFactory,
	INetworkUtilityService networkUtilityService)
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
	
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromRequestedIp(DhcpMessage message, IPersistentState<ClientInfo> clientInfo, string clientId)
	{
		// Check if the client has requested a specific IP
		if (message.RequestedIpAddress != null && !string.IsNullOrEmpty(message.RequestedIpAddress.ToString()))
		{
			var requestedIp = message.RequestedIpAddress.ToString();
			
			// Get the subnet mask and router settings
			var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
			var routerBytes = await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);
			var routerIp = string.Join('.', routerBytes);
			
			// Calculate the network and broadcast addresses
			var networkAddress = networkUtilityService.CalculateNetworkAddress(routerIp, subnetMask);
			var broadcastAddress = networkUtilityService.CalculateBroadcastAddress(routerIp, subnetMask);
			
			// Check if the requested IP is valid and within range
			if (!networkUtilityService.IsIpInRange(requestedIp, networkAddress, subnetMask) || 
				networkUtilityService.IsReservedIp(requestedIp, networkAddress, broadcastAddress))
			{
				logger.LogWarning("Requested IP {requestedIp} is not in valid range or is reserved", requestedIp);
				return (false, null);
			}
			
			// Check if the requested IP is already in use on the network
			var isInUse = await networkUtilityService.IsIpInUseAsync(requestedIp);
			if (isInUse)
			{
				logger.LogWarning("Requested IP {requestedIp} is already in use on the network", requestedIp);
				return (false, null);
			}
			
			// Check if the IP is available in our system
			var addressInfo = grainFactory.GetGrain<IIpAddressInformationGrain>(requestedIp);
			var addressStatus = await addressInfo.GetStatus();
			
			if (addressStatus is { Status: EIpAddressStatus.Available } ||
				(addressStatus is { Status: EIpAddressStatus.Offered or EIpAddressStatus.Claimed } && 
				 addressStatus.ClientId == clientId))
			{
				await addressInfo.SetStatus(EIpAddressStatus.Offered, clientId);
				
				clientInfo.State.Address = requestedIp;
				clientInfo.State.State = EClientState.Offered;
				await clientInfo.WriteStateAsync();
				
				logger.LogInformation("Create offer for {clientAddress} based on client requested address", requestedIp);
				var offer = await CreateOffer(message, requestedIp);
				return (offer is not null, offer);
			}
			
			logger.LogWarning("Requested IP {requestedIp} is not available in the system", requestedIp);
		}
		
		return (false, null);
	}
	
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromRandomIp(DhcpMessage message, IPersistentState<ClientInfo> clientInfo, string clientId)
	{
		// Get configuration settings
		var minAddress = await settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
		var maxAddress = await settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);
		var routerBytes = (await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))[0..^1];
		var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
		
		var routerBase = string.Join('.', routerBytes);
		
		// Calculate network and broadcast addresses
		var networkAddress = networkUtilityService.CalculateNetworkAddress($"{routerBase}.0", subnetMask);
		var broadcastAddress = networkUtilityService.CalculateBroadcastAddress($"{routerBase}.0", subnetMask);
		
		// Try to allocate IP sequentially - using a random starting point would be a future enhancement
		for (var i = minAddress; i <= maxAddress; i++)
		{
			var ipAddress = $"{string.Join('.', routerBytes)}.{i}";
			
			// Skip if this is a reserved address (network or broadcast)
			if (networkUtilityService.IsReservedIp(ipAddress, networkAddress, broadcastAddress))
			{
				continue;
			}
			
			// Check if the IP is already in use on the network (ARP probe)
			var isInUse = await networkUtilityService.IsIpInUseAsync(ipAddress);
			if (isInUse)
			{
				continue;
			}
			
			var addressInfo = grainFactory.GetGrain<IIpAddressInformationGrain>(ipAddress);
			var addressStatus = await addressInfo.GetStatus();
			
			if (addressStatus is not { Status: EIpAddressStatus.Available })
			{
				continue;
			}

			// Mark the address as offered
			await addressInfo.SetStatus(EIpAddressStatus.Offered, clientId);
			
			// Update client information
			clientInfo.State.Address = ipAddress;
			clientInfo.State.State = EClientState.Offered;
			await clientInfo.WriteStateAsync();

			logger.LogInformation("Create offer for {clientAddress} based on random address", ipAddress);
			var offer = await CreateOffer(message, ipAddress);
			return (offer is not null, offer);
		}
		
		logger.LogWarning("No available IP addresses to offer");
		return (false, null);
	}
	#endregion

	#region helpers
	//TODO: offer builder 
	private async Task<DhcpMessage> CreateOffer(DhcpMessage incomming, string address)
	{
		var localIp = GetLocalIpAddress();

		// Get subnet mask and router settings
		var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
		var routerIp = string.Join('.', await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER));
		
		// Calculate broadcast address
		var broadcastAddress = networkUtilityService.CalculateBroadcastAddress(routerIp, subnetMask);

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
					optionsBuilder.AddSubnetMask(subnetMask);
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
					optionsBuilder.AddBroadcastAddressOption(broadcastAddress);
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
							.Parse("0.0.0.0")
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

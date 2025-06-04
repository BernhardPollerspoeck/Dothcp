using Orleans;
using Orleans.Runtime;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.IpAddress;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Utilities;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpManagerGrain(
	[PersistentState("clientInfo", "File")] IPersistentState<ClientInfo> state,
	ILogger<DhcpManagerGrain> logger,
	IOfferGeneratorService offerGeneratorService,
	ISettingsLoaderService settingsLoader,
	INetworkUtilityService networkUtilityService,
	IReservationService reservationService)
	: Grain, IDhcpManagerGrain
{
	#region IDhcpManagerGrain
	public async Task<DhcpMessage?> HandleMessage(DhcpMessage message)
	{
		var messageType = message.GetMessageType();

		return messageType switch
		{
			EMessageType.Discover => await HandleDiscover(message),
			EMessageType.Request => await HandleRequest(message),
			EMessageType.Decline => await HandleDecline(message),
			EMessageType.Release => await HandleRelease(message),
			EMessageType.Inform => await HandleInform(message),
			_ => null,
		};
	}
	#endregion

	#region message handling
	public async Task<DhcpMessage?> HandleDiscover(DhcpMessage message)
	{
		//check reservations first - highest priority
		var offerFromReservation = await offerGeneratorService.TryCreateOfferFromReservation(message, state, this.GetPrimaryKeyString());
		if (offerFromReservation is { Item1: true, Item2: not null })
		{
			return offerFromReservation.Item2;
		}

		//get previously assigned ip
		var offerFromPreviousIp = await offerGeneratorService.TryCreateOfferFromPreviousIp(message, state, this.GetPrimaryKeyString());
		if (offerFromPreviousIp is { Item1: true, Item2: not null })
		{
			return offerFromPreviousIp.Item2;
		}

		//give the client a requested if available
		if (message.HasOption(EOption.AdressRequest))
		{
			// Use the new method for handling client-requested IPs
			var offerFromRequestedIp = await offerGeneratorService.TryCreateOfferFromRequestedIp(message, state, this.GetPrimaryKeyString());
			if (offerFromRequestedIp is { Item1: true, Item2: not null })
			{
				return offerFromRequestedIp.Item2;
			}
		}

		//get offer by server choosen ip
		var randomIpOffer = await offerGeneratorService.TryCreateOfferFromRandomIp(message, state, this.GetPrimaryKeyString());
		if (randomIpOffer is { Item1: true, Item2: not null })
		{
			return randomIpOffer.Item2;
		}

		//no way to create a offer
		logger.LogWarning("Create NO offer for message {message}", message);
		return null;
	}
	


	// Helper method to create NAK message
	private DhcpMessage CreateNakMessage(DhcpMessage requestMessage, string reason)
	{
		// Log the NAK reason
		logger.LogWarning("Sending DHCPNAK to client {clientId}: {reason}", this.GetPrimaryKeyString(), reason);
		
		// Get the local IP address for the server identifier
		var localIp = OfferGeneratorService.GetLocalIpAddress();
		
		// Create options for the NAK message
		var optionsBuilder = new DhcpOptionsBuilder()
			.AddMessageType(EMessageType.Nak)
			.AddServerIdentifier(localIp);
		
		// Return a properly constructed NAK message
		return new DhcpMessage
		{
			Direction = EMessageDirection.Reply,
			HardwareType = requestMessage.HardwareType,
			ClientIdLength = requestMessage.ClientIdLength,
			Hops = 0,
			TransactionId = requestMessage.TransactionId,
			ResponseCastType = requestMessage.ResponseCastType,
			ClientIpAdress = 0, // NAK should have 0.0.0.0 in ciaddr
			AssigneeAdress = 0, // NAK should have 0.0.0.0 in yiaddr
			ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
			ClientHardwareAdress = requestMessage.ClientHardwareAdress,
			Options = optionsBuilder.Build()
		};
	}

	public async Task<DhcpMessage?> HandleInform(DhcpMessage message)
	{
		logger.LogInformation("Processing DHCP Inform from client {clientId}", this.GetPrimaryKeyString());
		
		// For INFORM messages, clients already have an IP address and just want configuration
		// information, so we don't need to check or assign an IP address
		
		// Get the local IP address for the server identifier
		var localIp = OfferGeneratorService.GetLocalIpAddress();
		
		// Create ACK response with configuration options
		var optionsBuilder = new DhcpOptionsBuilder()
			.AddMessageType(EMessageType.Ack)
			.AddServerIdentifier(localIp)
			.AddTimeOffset(DateTime.Now - DateTime.UtcNow);
		
		// Get network configuration from settings
		var ipRange = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_IP_RANGE);
		var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
		var broadcastAddress = networkUtilityService.CalculateBroadcastAddress(ipRange, subnetMask);
		
		// Add requested parameters
		var parameters = message.GetParameterList();
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
					optionsBuilder.AddBroadcastAddressOption(broadcastAddress);
					break;

				case EOption.NtpServers:
					optionsBuilder.AddNtpServerOptions(await settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_NTP_SERVERS));
					break;
			}
		}
		
		// For INFORM messages, don't include lease time or renewal/rebinding times
		
		logger.LogInformation("Sent configuration information to client {clientId} in response to INFORM", this.GetPrimaryKeyString());
		
		// Create the response message
		return new DhcpMessage
		{
			Direction = EMessageDirection.Reply,
			HardwareType = message.HardwareType,
			ClientIdLength = message.ClientIdLength,
			Hops = 0,
			TransactionId = message.TransactionId,
			ResponseCastType = message.ResponseCastType,
			ClientIpAdress = message.ClientIpAdress, // Keep the client's IP
			AssigneeAdress = 0, // Don't assign an IP for INFORM
			ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
			ClientHardwareAdress = message.ClientHardwareAdress,
			Options = optionsBuilder.Build()
		};
	}

	public async Task<DhcpMessage?> HandleRelease(DhcpMessage message)
	{
		logger.LogInformation("Processing DHCP Release from client {clientId}", this.GetPrimaryKeyString());
		
		// Get the released IP address from the message
		string releasedIp;
		if (message.ClientIpAdress != 0) // Client should use ciaddr field for RELEASE
		{
			var bytes = BitConverter.GetBytes(message.ClientIpAdress);
			releasedIp = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
		}
		else if (message.HasOption(EOption.AdressRequest))
		{
			releasedIp = message.GetRequestedAddress();
		}
		else
		{
			// Unable to determine which IP address the client is releasing
			logger.LogWarning("DHCP Release from client {clientId} doesn't specify an IP address", this.GetPrimaryKeyString());
			return null; // No response needed for invalid RELEASE
		}
		
		// Check if this client actually has this IP assigned
		var ipAddressGrain = GrainFactory.GetGrain<IIpAddressInformationGrain>(releasedIp);
		var ipStatus = await ipAddressGrain.GetStatus();
		
		if (ipStatus.Status == EIpAddressStatus.Claimed && ipStatus.ClientId == this.GetPrimaryKeyString())
		{
			// Mark the IP as available in the IP address manager
			await ipAddressGrain.SetStatus(EIpAddressStatus.Available, null);
			
			// Revoke the lease
			var leaseGrain = GrainFactory.GetGrain<IDhcpLeaseGrain>(releasedIp);
			await leaseGrain.RevokeLease();
			
			// Update client state to indicate no assignment
			state.State.HasAssignedAddress = false;
			state.State.Address = null;
			state.State.State = EClientState.Released;
			await state.WriteStateAsync();
			
			logger.LogInformation("Client {clientId} released IP address {releasedIp}", this.GetPrimaryKeyString(), releasedIp);
		}
		else
		{
			// IP is not claimed by this client
			logger.LogWarning("Client {clientId} attempted to release IP {releasedIp} which is not assigned to them", 
				this.GetPrimaryKeyString(), releasedIp);
		}
		
		// No response is needed for RELEASE messages
		return null;
	}

	public async Task<DhcpMessage?> HandleDecline(DhcpMessage message)
	{
		logger.LogWarning("Processing DHCP Decline from client {clientId}", this.GetPrimaryKeyString());
		
		// Get the declined IP address from the message
		string declinedIp;
		if (message.HasOption(EOption.AdressRequest))
		{
			declinedIp = message.GetRequestedAddress();
		}
		else if (message.ClientIpAdress != 0) // Client is using ciaddr field
		{
			var bytes = BitConverter.GetBytes(message.ClientIpAdress);
			declinedIp = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
		}
		else
		{
			// Unable to determine which IP address the client is declining
			logger.LogWarning("DHCP Decline from client {clientId} doesn't specify an IP address", this.GetPrimaryKeyString());
			return null; // No response needed for invalid DECLINE
		}
		
		// Mark the IP as declined in the IP address manager
		var ipAddressGrain = GrainFactory.GetGrain<IIpAddressInformationGrain>(declinedIp);
		await ipAddressGrain.SetStatus(EIpAddressStatus.Declined, this.GetPrimaryKeyString());
		
		// Log the decline event
		logger.LogWarning("Client {clientId} declined IP address {declinedIp}", this.GetPrimaryKeyString(), declinedIp);
		
		// Update client state to indicate no assignment
		state.State.HasAssignedAddress = false;
		state.State.Address = null;
		state.State.State = EClientState.Declined;
		await state.WriteStateAsync();
		
		// No response is needed for DECLINE messages
		return null;
	}

	public async Task<DhcpMessage?> HandleRequest(DhcpMessage message)
	{
		logger.LogInformation("Processing DHCP Request from client {clientId}", this.GetPrimaryKeyString());
		
		// Get the requested IP address from the message
		string requestedIp;
		if (message.HasOption(EOption.AdressRequest))
		{
			requestedIp = message.GetRequestedAddress();
		}
		else if (message.ClientIpAdress != 0) // Client is using ciaddr field
		{
			var bytes = BitConverter.GetBytes(message.ClientIpAdress);
			requestedIp = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
		}
		else
		{
			// Unable to determine which IP address the client wants
			logger.LogWarning("DHCP Request from client {clientId} doesn't specify an IP address", this.GetPrimaryKeyString());
			return CreateNakMessage(message, "No IP address specified in request");
		}
		
		// Get network configuration from settings
		var ipRange = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_IP_RANGE);
		var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
		
		// Calculate the network address
		var networkAddress = networkUtilityService.CalculateNetworkAddress(ipRange, subnetMask);
		
		// Check if the requested IP is in the correct subnet
		if (!networkUtilityService.IsIpInRange(requestedIp, networkAddress, subnetMask))
		{
			return CreateNakMessage(message, $"Requested IP {requestedIp} is not in the configured subnet");
		}
		
		// Check if the address is a reserved address (network or broadcast)
		var broadcastAddress = networkUtilityService.CalculateBroadcastAddress(ipRange, subnetMask);
		if (networkUtilityService.IsReservedIp(requestedIp, networkAddress, broadcastAddress))
		{
			return CreateNakMessage(message, $"Requested IP {requestedIp} is a reserved address (network or broadcast)");
		}
		
		// Check if this IP is reserved for a different MAC address
		var clientMacAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");
		var existingReservation = await reservationService.GetReservationByIpAsync(IPAddress.Parse(requestedIp));
		if (existingReservation != null && existingReservation.IsActive)
		{
			if (!existingReservation.IsValidForMac(clientMacAddress))
			{
				logger.LogWarning("Client {clientId} with MAC {clientMac} requested IP {requestedIp} which is reserved for MAC {reservedMac}", 
					this.GetPrimaryKeyString(), clientMacAddress, requestedIp, existingReservation.MacAddress);
				return CreateNakMessage(message, $"IP {requestedIp} is reserved for a different client");
			}
		}
		
		// Check if the address is available or already offered to this client
		var ipAddressGrain = GrainFactory.GetGrain<IIpAddressInformationGrain>(requestedIp);
		var ipStatus = await ipAddressGrain.GetStatus();
		
		if (ipStatus.Status == EIpAddressStatus.Available)
		{
			// IP is available but wasn't offered to this client - shouldn't happen in normal flow
			logger.LogWarning("Client {clientId} requested IP {requestedIp} which wasn't offered", this.GetPrimaryKeyString(), requestedIp);
			return CreateNakMessage(message, $"IP {requestedIp} was not offered to this client");
		}
		
		if (ipStatus.Status == EIpAddressStatus.Claimed && ipStatus.ClientId != this.GetPrimaryKeyString())
		{
			// IP is already claimed by another client
			logger.LogWarning("Client {clientId} requested IP {requestedIp} which is claimed by another client", this.GetPrimaryKeyString(), requestedIp);
			return CreateNakMessage(message, $"IP {requestedIp} is already leased to another client");
		}
		
		if (ipStatus.Status == EIpAddressStatus.Offered && ipStatus.ClientId != this.GetPrimaryKeyString())
		{
			// IP was offered to another client
			logger.LogWarning("Client {clientId} requested IP {requestedIp} which was offered to another client", this.GetPrimaryKeyString(), requestedIp);
			return CreateNakMessage(message, $"IP {requestedIp} was offered to another client");
		}
		
		// Update IP status to Claimed
		await ipAddressGrain.SetStatus(EIpAddressStatus.Claimed, this.GetPrimaryKeyString());
		
		// Update client state
		state.State.HasAssignedAddress = true;
		state.State.Address = requestedIp;
		state.State.State = EClientState.Assigned;
		await state.WriteStateAsync();
		
		// Get the lease duration from settings
		var leaseDuration = await settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_TIME);
		
		// Create or update lease in the lease grain directly
		var leaseGrain = GrainFactory.GetGrain<IDhcpLeaseGrain>(requestedIp);
		var macAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");
		var lease = new DhcpLease
		{
			MacAddress = macAddress,
			IpAddress = IPAddress.Parse(requestedIp),
			HostName = message.GetHostname(),
			LeaseDuration = leaseDuration,
			LeaseStart = DateTime.UtcNow,
			Status = LeaseStatus.Active
		};
		
		await leaseGrain.UpdateLease(lease);
		
		logger.LogInformation("IP Address {requestedIp} assigned to client {clientId} with lease duration {leaseDuration}", 
			requestedIp, this.GetPrimaryKeyString(), leaseDuration);
		
		// Create ACK response
		var localIp = OfferGeneratorService.GetLocalIpAddress();
		
		var optionsBuilder = new DhcpOptionsBuilder()
			.AddAddressLeaseTime(leaseDuration)
			.AddMessageType(EMessageType.Ack)
			.AddServerIdentifier(localIp)
			.AddRenewalTime(await settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_RENEWAL))
			.AddRebindingTime(await settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_REBINDING))
			.AddTimeOffset(DateTime.Now - DateTime.UtcNow);
		
		// Add requested parameters
		var parameters = message.GetParameterList();
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
			HardwareType = message.HardwareType,
			ClientIdLength = message.ClientIdLength,
			Hops = 0,
			TransactionId = message.TransactionId,
			ResponseCastType = message.ResponseCastType,
			ClientIpAdress = message.ClientIpAdress, // Use client's provided IP if available
			AssigneeAdress = BitConverter.ToUInt32(IPAddress.Parse(requestedIp).GetAddressBytes()),
			ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
			ClientHardwareAdress = message.ClientHardwareAdress,
			Options = optionsBuilder.Build()
		};
	}
	#endregion


}

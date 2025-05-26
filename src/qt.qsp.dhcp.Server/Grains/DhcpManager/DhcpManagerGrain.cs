using Orleans;
using Orleans.Runtime;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.IpAddress;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using qt.qsp.dhcp.Server.Services;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpManagerGrain(
	[PersistentState("clientInfo", "File")] IPersistentState<ClientInfo> state,
	ILogger<DhcpManagerGrain> logger,
	IOfferGeneratorService offerGeneratorService,
	ISettingsLoaderService settingsLoader)
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
			_ => null,
		};
	}
	#endregion

	#region message handling
	public async Task<DhcpMessage?> HandleDiscover(DhcpMessage message)
	{
		// Check if this client already has a lease by MAC address
		var macAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");
		
		// Use LeaseGrainSearch to find a lease by MAC
		var existingLease = await LeaseGrainSearch.FindLeaseByMac(this.GrainFactory, macAddress);
		
		if (existingLease != null && !existingLease.IsExpired())
		{
			logger.LogInformation("Found existing lease for client with MAC {macAddress} at IP {ipAddress}", 
				macAddress, existingLease.IpAddress);
				
			// Create an offer for the previously leased IP
			var offerMessage = await CreateOfferMessage(message, existingLease.IpAddress.ToString());
			if (offerMessage != null)
			{
				return offerMessage;
			}
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
			var requestedAddress = message.GetRequestedAddress();
			
			// Check if the requested IP is already leased to someone else
			var leaseGrain = this.GrainFactory.GetGrain<IDhcpLeaseGrain>(requestedAddress);
			var lease = await leaseGrain.GetLease();
			var isExpired = await leaseGrain.IsExpired();
			
			if (lease == null || isExpired || lease.MacAddress == macAddress)
			{
				// IP is available or already belongs to this client
				var offerMessage = await CreateOfferMessage(message, requestedAddress);
				if (offerMessage != null)
				{
					return offerMessage;
				}
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
	
	// Helper method to create offer message
	private async Task<DhcpMessage?> CreateOfferMessage(DhcpMessage requestMessage, string ipAddress)
	{
		// This is a simplified placeholder - in reality, you would use logic similar to 
		// what's in offerGeneratorService to create a proper offer message
		// Example implementation would include checking IP availability, creating options, etc.
		// For now, we'll return null to indicate the caller should fall back to the existing implementation
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
			return null;
		}
		
		// Check if the address is available or already offered to this client
		var ipAddressGrain = this.GrainFactory.GetGrain<IIpAddressInformationGrain>(requestedIp);
		var ipStatus = await ipAddressGrain.GetStatus();
		
		if (ipStatus.Status == EIpAddressStatus.Available)
		{
			// IP is available but wasn't offered to this client - shouldn't happen in normal flow
			logger.LogWarning("Client {clientId} requested IP {requestedIp} which wasn't offered", this.GetPrimaryKeyString(), requestedIp);
			return null;
		}
		
		if (ipStatus.Status == EIpAddressStatus.Claimed && ipStatus.ClientId != this.GetPrimaryKeyString())
		{
			// IP is already claimed by another client
			logger.LogWarning("Client {clientId} requested IP {requestedIp} which is claimed by another client", this.GetPrimaryKeyString(), requestedIp);
			return null;
		}
		
		if (ipStatus.Status == EIpAddressStatus.Offered && ipStatus.ClientId != this.GetPrimaryKeyString())
		{
			// IP was offered to another client
			logger.LogWarning("Client {clientId} requested IP {requestedIp} which was offered to another client", this.GetPrimaryKeyString(), requestedIp);
			return null;
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
		var leaseGrain = this.GrainFactory.GetGrain<IDhcpLeaseGrain>(requestedIp);
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

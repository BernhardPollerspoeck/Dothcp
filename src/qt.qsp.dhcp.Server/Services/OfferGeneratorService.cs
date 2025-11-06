using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Services.Core;
using qt.qsp.dhcp.Server.Utilities;
using qt.qsp.dhcp.Server.Data.Repositories;
using System.Net;
using System.Net.Sockets;

namespace qt.qsp.dhcp.Server.Services;

public class OfferGeneratorService(
	ILogger<OfferGeneratorService> logger,
	ISettingsLoaderService settingsLoader,
	IIpAddressService ipAddressService,
	INetworkUtilityService networkUtilityService,
	IReservationService reservationService,
	IClientRepository clientRepository)
	: IOfferGeneratorService
{
	// Semaphore to prevent race conditions during IP allocation
	// Only one thread can allocate/check IP status at a time
	private static readonly SemaphoreSlim _ipAllocationLock = new SemaphoreSlim(1, 1);

	#region IOfferGeneratorService
	
	/// <summary>
	/// Checks if there is an active reservation for the given MAC address and tries to create an offer.
	/// This should be called first in the allocation process to honor reservations.
	/// </summary>
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromReservation(DhcpMessage message, Models.ClientInfo clientInfo, string clientId)
	{
		// Get the client's MAC address from the message
		var macAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");

		// Check if there's an active reservation for this MAC address
		var reservation = await reservationService.GetReservationByMacAsync(macAddress);
		if (reservation != null && reservation.IsActive)
		{
			var reservedIp = reservation.IpAddress.ToString();

			// Thread-safe IP allocation - lock to prevent race conditions
			await _ipAllocationLock.WaitAsync();
			try
			{
				// Check if the reserved IP is available or already assigned to this client
				var addressStatus = await ipAddressService.GetStatusAsync(reservedIp);

				// If IP is available or already assigned to this client, use the reservation
				if (addressStatus == Models.EIpAddressStatus.Available ||
					((addressStatus == Models.EIpAddressStatus.Offered || addressStatus == Models.EIpAddressStatus.Claimed) &&
					 (await ipAddressService.GetIpAddressInfoAsync(reservedIp))?.ClientId == clientId))
				{
					await ipAddressService.SetStatusAsync(reservedIp, Models.EIpAddressStatus.Offered, clientId);

					clientInfo.AssignedIpAddress = reservedIp;
					clientInfo.State = EClientState.Offered.ToString();
					await clientRepository.AddOrUpdateAsync(clientInfo);

					// Mark the reservation as used
					var reservationCore = await reservationService.GetReservationByIpAsync(IPAddress.Parse(reservedIp));
					if (reservationCore != null)
					{
						reservationCore.MarkAsUsed();
						await reservationService.UpdateReservationAsync(reservationCore);
					}

					logger.LogInformation("Create offer for {clientAddress} based on IP reservation for MAC {macAddress}",
						reservedIp, macAddress);
					var offer = await CreateOffer(message, reservedIp);
					return (offer is not null, offer);
				}
				else
				{
					// The reserved IP is claimed by another client - this is a conflict that should be logged
					var otherClientId = (await ipAddressService.GetIpAddressInfoAsync(reservedIp))?.ClientId;
					logger.LogWarning("Reserved IP {reservedIp} for MAC {macAddress} is claimed by another client {otherClientId}",
						reservedIp, macAddress, otherClientId);
				}
			}
			finally
			{
				_ipAllocationLock.Release();
			}
		}

		return (false, null);
	}
	
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromPreviousIp(DhcpMessage message, Models.ClientInfo clientInfo, string clientId)
	{
		if (!string.IsNullOrEmpty(clientInfo.AssignedIpAddress))
		{
			var previousIpAddress = clientInfo.AssignedIpAddress;

			// Thread-safe IP allocation - lock to prevent race conditions
			await _ipAllocationLock.WaitAsync();
			try
			{
				var status = await ipAddressService.GetStatusAsync(previousIpAddress);
				var ipInfo = await ipAddressService.GetIpAddressInfoAsync(previousIpAddress);

				if ((status == Models.EIpAddressStatus.Claimed || status == Models.EIpAddressStatus.Offered)
					&& ipInfo?.ClientId == clientId)
				{
					clientInfo.AssignedIpAddress = previousIpAddress;
					clientInfo.State = EClientState.Offered.ToString();
					await clientRepository.AddOrUpdateAsync(clientInfo);

					await ipAddressService.SetStatusAsync(previousIpAddress, Models.EIpAddressStatus.Offered, clientId);

					logger.LogInformation("Create offer for {clientAddress} based on previously assigned address", previousIpAddress);
					var offer = await CreateOffer(message, previousIpAddress);
					return (offer is not null, offer);
				}
			}
			finally
			{
				_ipAllocationLock.Release();
			}
		}
		return (false, null);
	}
	
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromRequestedIp(DhcpMessage message, Models.ClientInfo clientInfo, string clientId)
	{
		// Check if the client has requested a specific IP
		if (message.RequestedIpAddress != null && !string.IsNullOrEmpty(message.RequestedIpAddress.ToString()))
		{
			var requestedIp = message.RequestedIpAddress.ToString();

			// Get the subnet mask and router settings
			var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
			var routerBytes = await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);

			if (string.IsNullOrEmpty(subnetMask) || routerBytes == null || routerBytes.Length == 0)
			{
				logger.LogError("DHCP subnet or router settings are not configured");
				return (false, null);
			}

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

			// Thread-safe IP allocation - lock to prevent race conditions
			await _ipAllocationLock.WaitAsync();
			try
			{
				// Check if the IP is available in our system
				var addressStatus = await ipAddressService.GetStatusAsync(requestedIp);
				var ipInfo = await ipAddressService.GetIpAddressInfoAsync(requestedIp);

				if (addressStatus == Models.EIpAddressStatus.Available ||
					((addressStatus == Models.EIpAddressStatus.Offered || addressStatus == Models.EIpAddressStatus.Claimed) &&
					 ipInfo?.ClientId == clientId))
				{
					await ipAddressService.SetStatusAsync(requestedIp, Models.EIpAddressStatus.Offered, clientId);

					clientInfo.AssignedIpAddress = requestedIp;
					clientInfo.State = EClientState.Offered.ToString();
					await clientRepository.AddOrUpdateAsync(clientInfo);

					logger.LogInformation("Create offer for {clientAddress} based on client requested address", requestedIp);
					var offer = await CreateOffer(message, requestedIp);
					return (offer is not null, offer);
				}

				logger.LogWarning("Requested IP {requestedIp} is not available in the system", requestedIp);
			}
			finally
			{
				_ipAllocationLock.Release();
			}
		}

		return (false, null);
	}
	
	public async Task<(bool, DhcpMessage?)> TryCreateOfferFromRandomIp(DhcpMessage message, Models.ClientInfo clientInfo, string clientId)
	{
		// Get configuration settings
		var minAddress = await settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW);
		var maxAddress = await settingsLoader.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH);

		var routerBytesRaw = await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);
		if (routerBytesRaw == null || routerBytesRaw.Length == 0)
		{
			logger.LogError("DHCP_LEASE_ROUTER setting is not configured");
			return (false, null);
		}
		var routerBytes = routerBytesRaw[0..^1];
		var subnetMask = await settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
		if (string.IsNullOrEmpty(subnetMask))
		{
			logger.LogError("DHCP_LEASE_SUBNET setting is not configured");
			return (false, null);
		}

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

			// Check if this IP is reserved for a different MAC address
			var existingReservation = await reservationService.GetReservationByIpAsync(IPAddress.Parse(ipAddress));
			if (existingReservation != null && existingReservation.IsActive)
			{
				// This IP is reserved - skip it unless it's reserved for this client
				var clientMacAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");
				if (!existingReservation.IsValidForMac(clientMacAddress))
				{
					continue;
				}
			}

			// Thread-safe IP allocation - lock to prevent race conditions
			await _ipAllocationLock.WaitAsync();
			try
			{
				var addressStatus = await ipAddressService.GetStatusAsync(ipAddress);

				if (addressStatus != Models.EIpAddressStatus.Available)
				{
					continue;
				}

				// Mark the address as offered
				await ipAddressService.SetStatusAsync(ipAddress, Models.EIpAddressStatus.Offered, clientId);

				// Update client information
				clientInfo.AssignedIpAddress = ipAddress;
				clientInfo.State = EClientState.Offered.ToString();
				await clientRepository.AddOrUpdateAsync(clientInfo);

				logger.LogInformation("Create offer for {clientAddress} based on random address", ipAddress);
				var offer = await CreateOffer(message, ipAddress);
				return (offer is not null, offer);
			}
			finally
			{
				_ipAllocationLock.Release();
			}
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
		var routerBytes = await settingsLoader.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER);

		if (string.IsNullOrEmpty(subnetMask) || routerBytes == null || routerBytes.Length == 0)
		{
			throw new InvalidOperationException("DHCP subnet or router settings are not configured. Cannot create DHCP offer.");
		}

		var routerIp = string.Join('.', routerBytes);

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

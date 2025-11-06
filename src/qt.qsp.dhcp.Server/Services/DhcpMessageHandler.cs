using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Models.OptionBuilder;
using qt.qsp.dhcp.Server.Services.Core;
using qt.qsp.dhcp.Server.Utilities;
using System.Net;

namespace qt.qsp.dhcp.Server.Services;

/// <summary>
/// Handles DHCP protocol messages (replaces DhcpManagerGrain)
/// </summary>
public interface IDhcpMessageHandler
{
    Task<DhcpMessage?> HandleMessageAsync(DhcpMessage message, string clientId);
}

public class DhcpMessageHandler : IDhcpMessageHandler
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<DhcpMessageHandler> _logger;
    private readonly IOfferGeneratorService _offerGeneratorService;
    private readonly ISettingsLoaderService _settingsLoader;
    private readonly INetworkUtilityService _networkUtilityService;
    private readonly IReservationService _reservationService;
    private readonly IIpAddressService _ipAddressService;
    private readonly ILeaseService _leaseService;

    public DhcpMessageHandler(
        IClientRepository clientRepository,
        ILogger<DhcpMessageHandler> logger,
        IOfferGeneratorService offerGeneratorService,
        ISettingsLoaderService settingsLoader,
        INetworkUtilityService networkUtilityService,
        IReservationService reservationService,
        IIpAddressService ipAddressService,
        ILeaseService leaseService)
    {
        _clientRepository = clientRepository;
        _logger = logger;
        _offerGeneratorService = offerGeneratorService;
        _settingsLoader = settingsLoader;
        _networkUtilityService = networkUtilityService;
        _reservationService = reservationService;
        _ipAddressService = ipAddressService;
        _leaseService = leaseService;
    }

    public async Task<DhcpMessage?> HandleMessageAsync(DhcpMessage message, string clientId)
    {
        var messageType = message.GetMessageType();

        return messageType switch
        {
            EMessageType.Discover => await HandleDiscoverAsync(message, clientId),
            EMessageType.Request => await HandleRequestAsync(message, clientId),
            EMessageType.Decline => await HandleDeclineAsync(message, clientId),
            EMessageType.Release => await HandleReleaseAsync(message, clientId),
            EMessageType.Inform => await HandleInformAsync(message, clientId),
            _ => null,
        };
    }

    private async Task<DhcpMessage?> HandleDiscoverAsync(DhcpMessage message, string clientId)
    {
        // Get or create client info
        var clientInfo = await _clientRepository.GetByClientIdAsync(clientId) ?? new Models.ClientInfo
        {
            ClientId = clientId,
            LastSeen = DateTime.UtcNow
        };

        clientInfo.LastSeen = DateTime.UtcNow;
        clientInfo.HostName = message.GetHostname();
        clientInfo.DomainName = message.GetDomainName();

        // Check reservations first - highest priority
        var offerFromReservation = await _offerGeneratorService.TryCreateOfferFromReservation(message, clientInfo, clientId);
        if (offerFromReservation is { Item1: true, Item2: not null })
        {
            return offerFromReservation.Item2;
        }

        // Get previously assigned ip
        var offerFromPreviousIp = await _offerGeneratorService.TryCreateOfferFromPreviousIp(message, clientInfo, clientId);
        if (offerFromPreviousIp is { Item1: true, Item2: not null })
        {
            return offerFromPreviousIp.Item2;
        }

        // Give the client a requested if available
        if (message.HasOption(EOption.AdressRequest))
        {
            var offerFromRequestedIp = await _offerGeneratorService.TryCreateOfferFromRequestedIp(message, clientInfo, clientId);
            if (offerFromRequestedIp is { Item1: true, Item2: not null })
            {
                return offerFromRequestedIp.Item2;
            }
        }

        // Get offer by server chosen ip
        var randomIpOffer = await _offerGeneratorService.TryCreateOfferFromRandomIp(message, clientInfo, clientId);
        if (randomIpOffer is { Item1: true, Item2: not null })
        {
            return randomIpOffer.Item2;
        }

        // No way to create an offer
        _logger.LogWarning("Create NO offer for client {clientId}", clientId);
        return null;
    }

    private async Task<DhcpMessage?> HandleRequestAsync(DhcpMessage message, string clientId)
    {
        _logger.LogInformation("Processing DHCP Request from client {clientId}", clientId);

        // Get the requested IP address from the message
        string requestedIp;
        if (message.HasOption(EOption.AdressRequest))
        {
            requestedIp = message.GetRequestedAddress();
        }
        else if (message.ClientIpAdress != 0)
        {
            var bytes = BitConverter.GetBytes(message.ClientIpAdress);
            requestedIp = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
        }
        else
        {
            _logger.LogWarning("DHCP Request from client {clientId} doesn't specify an IP address", clientId);
            return CreateNakMessage(message, clientId, "No IP address specified in request");
        }

        // Get network configuration from settings
        var ipRange = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_IP_RANGE);
        var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);

        // Calculate the network address
        var networkAddress = _networkUtilityService.CalculateNetworkAddress(ipRange, subnetMask);

        // Check if the requested IP is in the correct subnet
        if (!_networkUtilityService.IsIpInRange(requestedIp, networkAddress, subnetMask))
        {
            return CreateNakMessage(message, clientId, $"Requested IP {requestedIp} is not in the configured subnet");
        }

        // Check if the address is a reserved address (network or broadcast)
        var broadcastAddress = _networkUtilityService.CalculateBroadcastAddress(ipRange, subnetMask);
        if (_networkUtilityService.IsReservedIp(requestedIp, networkAddress, broadcastAddress))
        {
            return CreateNakMessage(message, clientId, $"Requested IP {requestedIp} is a reserved address");
        }

        // Check if this IP is reserved for a different MAC address
        var clientMacAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");
        var existingReservation = await _reservationService.GetReservationByIpAsync(IPAddress.Parse(requestedIp));
        if (existingReservation != null && existingReservation.IsActive)
        {
            if (!existingReservation.IsValidForMac(clientMacAddress))
            {
                _logger.LogWarning("Client {clientId} with MAC {clientMac} requested IP {requestedIp} reserved for MAC {reservedMac}",
                    clientId, clientMacAddress, requestedIp, existingReservation.MacAddress);
                return CreateNakMessage(message, clientId, $"IP {requestedIp} is reserved for a different client");
            }
        }

        // Check if the address is available or already offered to this client
        var ipStatus = await _ipAddressService.GetStatusAsync(requestedIp);
        var ipInfo = await _ipAddressService.GetIpAddressInfoAsync(requestedIp);

        if (ipStatus == Models.EIpAddressStatus.Available)
        {
            _logger.LogWarning("Client {clientId} requested IP {requestedIp} which wasn't offered", clientId, requestedIp);
            return CreateNakMessage(message, clientId, $"IP {requestedIp} was not offered to this client");
        }

        if (ipStatus == Models.EIpAddressStatus.Claimed && ipInfo?.ClientId != clientId)
        {
            _logger.LogWarning("Client {clientId} requested IP {requestedIp} claimed by another client", clientId, requestedIp);
            return CreateNakMessage(message, clientId, $"IP {requestedIp} is already leased to another client");
        }

        if (ipStatus == Models.EIpAddressStatus.Offered && ipInfo?.ClientId != clientId)
        {
            _logger.LogWarning("Client {clientId} requested IP {requestedIp} offered to another client", clientId, requestedIp);
            return CreateNakMessage(message, clientId, $"IP {requestedIp} was offered to another client");
        }

        // Update IP status to Claimed
        await _ipAddressService.SetStatusAsync(requestedIp, Models.EIpAddressStatus.Claimed, clientId);

        // Update client state
        var clientInfo = await _clientRepository.GetByClientIdAsync(clientId) ?? new Models.ClientInfo { ClientId = clientId };
        clientInfo.AssignedIpAddress = requestedIp;
        clientInfo.State = EClientState.Assigned.ToString();
        clientInfo.LastSeen = DateTime.UtcNow;
        clientInfo.HostName = message.GetHostname();
        clientInfo.DomainName = message.GetDomainName();
        await _clientRepository.AddOrUpdateAsync(clientInfo);

        // Get the lease duration from settings
        var leaseDuration = await _settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_TIME);

        // Check if this is a renewal of an existing lease
        var macAddress = BitConverter.ToString(message.ClientHardwareAdress).Replace("-", ":");
        var existingLease = await _leaseService.GetLeaseAsync(requestedIp);

        if (existingLease != null && existingLease.MacAddress == macAddress)
        {
            // RENEWAL: Keep the original LeaseStart, update status to Renewed
            existingLease.Status = Models.LeaseStatus.Renewed;
            existingLease.LeaseDuration = leaseDuration;
            existingLease.HostName = message.GetHostname() ?? existingLease.HostName;
            // LeaseStart is NOT changed - this preserves the original lease start time
            await _leaseService.UpdateLeaseAsync(existingLease);

            _logger.LogInformation("Renewed lease for IP {requestedIp} to client {clientId}, original lease start: {leaseStart}",
                requestedIp, clientId, existingLease.LeaseStart);
        }
        else
        {
            // NEW LEASE: Create new lease with current time as LeaseStart
            var newLease = new Models.DhcpLease
            {
                MacAddress = macAddress,
                IpAddressString = requestedIp,
                HostName = message.GetHostname(),
                LeaseDuration = leaseDuration,
                LeaseStart = DateTime.UtcNow,
                Status = Models.LeaseStatus.Active
            };

            await _leaseService.UpdateLeaseAsync(newLease);

            _logger.LogInformation("Created new lease for IP {requestedIp} to client {clientId}",
                requestedIp, clientId);
        }

        // Create ACK response
        return await CreateAckMessage(message, requestedIp);
    }

    private async Task<DhcpMessage?> HandleDeclineAsync(DhcpMessage message, string clientId)
    {
        _logger.LogWarning("Processing DHCP Decline from client {clientId}", clientId);

        // Get the declined IP address from the message
        string? declinedIp = null;
        if (message.HasOption(EOption.AdressRequest))
        {
            declinedIp = message.GetRequestedAddress();
        }
        else if (message.ClientIpAdress != 0)
        {
            var bytes = BitConverter.GetBytes(message.ClientIpAdress);
            declinedIp = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
        }

        if (string.IsNullOrEmpty(declinedIp))
        {
            _logger.LogWarning("DHCP Decline from client {clientId} doesn't specify an IP address", clientId);
            return null;
        }

        // Mark the IP as declined
        await _ipAddressService.SetStatusAsync(declinedIp, Models.EIpAddressStatus.Declined, clientId);

        _logger.LogWarning("Client {clientId} declined IP address {declinedIp}", clientId, declinedIp);

        // Update client state
        var clientInfo = await _clientRepository.GetByClientIdAsync(clientId);
        if (clientInfo != null)
        {
            clientInfo.AssignedIpAddress = null;
            clientInfo.State = EClientState.Declined.ToString();
            clientInfo.LastSeen = DateTime.UtcNow;
            await _clientRepository.AddOrUpdateAsync(clientInfo);
        }

        // No response is needed for DECLINE messages
        return null;
    }

    private async Task<DhcpMessage?> HandleReleaseAsync(DhcpMessage message, string clientId)
    {
        _logger.LogInformation("Processing DHCP Release from client {clientId}", clientId);

        // Get the released IP address from the message
        string? releasedIp = null;
        if (message.ClientIpAdress != 0)
        {
            var bytes = BitConverter.GetBytes(message.ClientIpAdress);
            releasedIp = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
        }
        else if (message.HasOption(EOption.AdressRequest))
        {
            releasedIp = message.GetRequestedAddress();
        }

        if (string.IsNullOrEmpty(releasedIp))
        {
            _logger.LogWarning("DHCP Release from client {clientId} doesn't specify an IP address", clientId);
            return null;
        }

        // Check if this client actually has this IP assigned
        var ipStatus = await _ipAddressService.GetStatusAsync(releasedIp);
        var ipInfo = await _ipAddressService.GetIpAddressInfoAsync(releasedIp);

        if (ipStatus == Models.EIpAddressStatus.Claimed && ipInfo?.ClientId == clientId)
        {
            // Mark the IP as available
            await _ipAddressService.SetStatusAsync(releasedIp, Models.EIpAddressStatus.Available, null);

            // Revoke the lease
            await _leaseService.RevokeLeaseAsync(releasedIp);

            // Update client state
            var clientInfo = await _clientRepository.GetByClientIdAsync(clientId);
            if (clientInfo != null)
            {
                clientInfo.AssignedIpAddress = null;
                clientInfo.State = EClientState.Released.ToString();
                await _clientRepository.AddOrUpdateAsync(clientInfo);
            }

            _logger.LogInformation("Client {clientId} released IP address {releasedIp}", clientId, releasedIp);
        }
        else
        {
            _logger.LogWarning("Client {clientId} attempted to release IP {releasedIp} which is not assigned to them",
                clientId, releasedIp);
        }

        // No response needed for RELEASE messages
        return null;
    }

    private async Task<DhcpMessage?> HandleInformAsync(DhcpMessage message, string clientId)
    {
        _logger.LogInformation("Processing DHCP Inform from client {clientId}", clientId);

        // For INFORM messages, clients already have an IP address and just want configuration
        var localIp = OfferGeneratorService.GetLocalIpAddress();

        var optionsBuilder = new DhcpOptionsBuilder()
            .AddMessageType(EMessageType.Ack)
            .AddServerIdentifier(localIp)
            .AddTimeOffset(DateTime.Now - DateTime.UtcNow);

        // Get network configuration from settings
        var ipRange = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_IP_RANGE);
        var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
        var broadcastAddress = _networkUtilityService.CalculateBroadcastAddress(ipRange, subnetMask);

        // Add requested parameters
        var parameters = message.GetParameterList();
        foreach (var item in parameters.Cast<EOption>())
        {
            switch (item)
            {
                case EOption.SubnetMask:
                    optionsBuilder.AddSubnetMask(await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET));
                    break;
                case EOption.RouterOptions:
                    optionsBuilder.AddRouterOption(await _settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_ROUTER));
                    break;
                case EOption.DnsServerOptions:
                    optionsBuilder.AddDnsServerOptions(await _settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_DNS));
                    break;
                case EOption.BroadcastAddressOption:
                    optionsBuilder.AddBroadcastAddressOption(broadcastAddress);
                    break;
                case EOption.NtpServers:
                    optionsBuilder.AddNtpServerOptions(await _settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_NTP_SERVERS));
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
            ClientIpAdress = message.ClientIpAdress,
            AssigneeAdress = 0,
            ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
            ClientHardwareAdress = message.ClientHardwareAdress,
            Options = optionsBuilder.Build()
        };
    }

    private DhcpMessage CreateNakMessage(DhcpMessage originalMessage, string clientId, string reason)
    {
        _logger.LogWarning("Sending NAK to client {clientId}: {reason}", clientId, reason);

        var localIp = OfferGeneratorService.GetLocalIpAddress();

        var optionsBuilder = new DhcpOptionsBuilder()
            .AddMessageType(EMessageType.Nak)
            .AddServerIdentifier(localIp);

        return new DhcpMessage
        {
            Direction = EMessageDirection.Reply,
            HardwareType = originalMessage.HardwareType,
            ClientIdLength = originalMessage.ClientIdLength,
            Hops = 0,
            TransactionId = originalMessage.TransactionId,
            ResponseCastType = ECastType.Broadcast, // NAKs are always broadcast
            ClientIpAdress = 0,
            AssigneeAdress = 0,
            ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
            ClientHardwareAdress = originalMessage.ClientHardwareAdress,
            Options = optionsBuilder.Build()
        };
    }

    private async Task<DhcpMessage> CreateAckMessage(DhcpMessage originalMessage, string assignedIp)
    {
        var localIp = OfferGeneratorService.GetLocalIpAddress();

        // Get network configuration from settings
        var subnetMask = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET);
        var ipRange = await _settingsLoader.GetSetting<string>(SettingsConstants.DHCP_IP_RANGE);
        var leaseDuration = await _settingsLoader.GetSetting<TimeSpan>(SettingsConstants.DHCP_LEASE_TIME);
        var routers = await _settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_ROUTER);
        var dnsServers = await _settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_DNS);
        var ntpServers = await _settingsLoader.GetSetting<string[]>(SettingsConstants.DHCP_LEASE_NTP_SERVERS);
        var broadcastAddress = _networkUtilityService.CalculateBroadcastAddress(ipRange, subnetMask);

        var optionsBuilder = new DhcpOptionsBuilder()
            .AddMessageType(EMessageType.Ack)
            .AddServerIdentifier(localIp)
            .AddSubnetMask(subnetMask)
            .AddRouterOption(routers)
            .AddDnsServerOptions(dnsServers)
            .AddLeaseTime(leaseDuration)
            .AddBroadcastAddressOption(broadcastAddress)
            .AddTimeOffset(DateTime.Now - DateTime.UtcNow);

        if (ntpServers != null && ntpServers.Length > 0)
        {
            optionsBuilder.AddNtpServerOptions(ntpServers);
        }

        // Parse assigned IP to uint
        var ipBytes = IPAddress.Parse(assignedIp).GetAddressBytes();
        var assignedIpUint = BitConverter.ToUInt32(ipBytes);

        return new DhcpMessage
        {
            Direction = EMessageDirection.Reply,
            HardwareType = originalMessage.HardwareType,
            ClientIdLength = originalMessage.ClientIdLength,
            Hops = 0,
            TransactionId = originalMessage.TransactionId,
            ResponseCastType = originalMessage.ResponseCastType,
            ClientIpAdress = 0,
            AssigneeAdress = assignedIpUint,
            ServerIpAdress = BitConverter.ToUInt32(localIp.GetAddressBytes()),
            ClientHardwareAdress = originalMessage.ClientHardwareAdress,
            Options = optionsBuilder.Build()
        };
    }
}

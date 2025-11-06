using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
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
        // Implementation simplified - this would need the full logic from DhcpManagerGrain
        _logger.LogInformation("Processing DHCP Request from client {clientId}", clientId);

        // TODO: Full implementation needed
        return null;
    }

    private async Task<DhcpMessage?> HandleDeclineAsync(DhcpMessage message, string clientId)
    {
        _logger.LogWarning("Processing DHCP Decline from client {clientId}", clientId);

        // TODO: Full implementation needed
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
}

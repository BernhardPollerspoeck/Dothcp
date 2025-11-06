using Microsoft.Extensions.Logging;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Services.Core;

public class ReservationServiceCore : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<ReservationServiceCore> _logger;

    public ReservationServiceCore(
        IReservationRepository reservationRepository,
        ILogger<ReservationServiceCore> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<List<DhcpReservation>> GetAllReservationsAsync()
    {
        return await _reservationRepository.GetAllAsync();
    }

    public async Task<DhcpReservation?> GetReservationByMacAsync(string macAddress)
    {
        var reservation = await _reservationRepository.GetByMacAddressAsync(macAddress);
        return reservation?.IsValidForMac(macAddress) == true ? reservation : null;
    }

    public async Task<DhcpReservation?> GetReservationByIpAsync(IPAddress ipAddress)
    {
        return await _reservationRepository.GetByIpAddressAsync(ipAddress.ToString());
    }

    public async Task<(bool success, string? errorMessage)> AddReservationAsync(DhcpReservation reservation)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(reservation.MacAddress))
            {
                return (false, "MAC address is required");
            }

            if (reservation.IpAddress == IPAddress.None)
            {
                return (false, "IP address is required");
            }

            // Check for conflicts
            var (hasConflict, conflictReason) = await HasConflictAsync(reservation);
            if (hasConflict)
            {
                _logger.LogWarning("Cannot add reservation due to conflict: {ConflictReason}", conflictReason);
                return (false, conflictReason);
            }

            await _reservationRepository.AddAsync(reservation);

            _logger.LogInformation("Added reservation for MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add reservation for MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string? errorMessage)> UpdateReservationAsync(DhcpReservation reservation)
    {
        try
        {
            // Find existing by IP
            var existing = await _reservationRepository.GetByIpAddressAsync(reservation.IpAddressString);

            if (existing == null)
            {
                return (false, "Reservation not found");
            }

            // Check for conflicts (excluding current reservation)
            var (hasConflict, conflictReason) = await HasConflictAsync(reservation, existing.Id);
            if (hasConflict)
            {
                _logger.LogWarning("Cannot update reservation due to conflict: {ConflictReason}", conflictReason);
                return (false, conflictReason);
            }

            // Update properties
            existing.MacAddress = reservation.MacAddress;
            existing.Description = reservation.Description;
            existing.IsActive = reservation.IsActive;
            existing.LastUsed = reservation.LastUsed;
            existing.SubnetString = reservation.SubnetString;
            existing.RouterString = reservation.RouterString;
            existing.DhcpServerString = reservation.DhcpServerString;
            existing.DnsServerStringsJson = reservation.DnsServerStringsJson;

            await _reservationRepository.UpdateAsync(existing);

            _logger.LogInformation("Updated reservation for MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update reservation for MAC {MacAddress} -> IP {IpAddress}",
                reservation.MacAddress, reservation.IpAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string? errorMessage)> DeleteReservationAsync(IPAddress ipAddress)
    {
        try
        {
            var reservation = await _reservationRepository.GetByIpAddressAsync(ipAddress.ToString());

            if (reservation == null)
            {
                return (false, "Reservation not found");
            }

            await _reservationRepository.DeleteAsync(reservation.Id);

            _logger.LogInformation("Deleted reservation for IP {IpAddress}", ipAddress);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete reservation for IP {IpAddress}", ipAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool hasConflict, string? conflictReason)> HasConflictAsync(
        DhcpReservation reservation,
        int? excludeId = null)
    {
        // Check for IP address conflict
        var existingByIp = await _reservationRepository.GetByIpAddressAsync(reservation.IpAddressString);
        if (existingByIp != null &&
            (!excludeId.HasValue || existingByIp.Id != excludeId.Value) &&
            !string.Equals(existingByIp.MacAddress, reservation.MacAddress, StringComparison.OrdinalIgnoreCase))
        {
            return (true, $"IP address {reservation.IpAddress} is already reserved for MAC {existingByIp.MacAddress}");
        }

        // Check for MAC address conflict
        var existingByMac = await _reservationRepository.GetByMacAddressAsync(reservation.MacAddress);
        if (existingByMac != null &&
            (!excludeId.HasValue || existingByMac.Id != excludeId.Value) &&
            !existingByMac.IpAddress.Equals(reservation.IpAddress))
        {
            return (true, $"MAC address {reservation.MacAddress} is already reserved for IP {existingByMac.IpAddress}");
        }

        return (false, null);
    }
}

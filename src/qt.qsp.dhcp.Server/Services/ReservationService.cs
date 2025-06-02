using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net;

namespace qt.qsp.dhcp.Server.Services;

public class ReservationService : IReservationService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(IGrainFactory grainFactory, ILogger<ReservationService> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public async Task<IList<DhcpReservation>> GetAllReservationsAsync()
    {
        try
        {
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetAllReservations();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all reservations");
            return new List<DhcpReservation>();
        }
    }

    public async Task<DhcpReservation?> GetReservationByMacAsync(string macAddress)
    {
        try
        {
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetReservationByMac(macAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation by MAC {MacAddress}", macAddress);
            return null;
        }
    }

    public async Task<DhcpReservation?> GetReservationByIpAsync(IPAddress ipAddress)
    {
        try
        {
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetReservationByIp(ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation by IP {IpAddress}", ipAddress);
            return null;
        }
    }

    public async Task<(bool success, string? errorMessage)> AddReservationAsync(DhcpReservation reservation)
    {
        try
        {
            // Validate the reservation
            if (string.IsNullOrWhiteSpace(reservation.MacAddress))
            {
                return (false, "MAC address is required");
            }

            if (reservation.IpAddress == IPAddress.None)
            {
                return (false, "IP address is required");
            }

            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            var success = await managerGrain.AddReservation(reservation);
            
            if (success)
            {
                _logger.LogInformation("Successfully added reservation for MAC {MacAddress} -> IP {IpAddress}", 
                    reservation.MacAddress, reservation.IpAddress);
                return (true, null);
            }
            else
            {
                return (false, "Failed to add reservation - check for conflicts");
            }
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
            // Validate the reservation
            if (string.IsNullOrWhiteSpace(reservation.MacAddress))
            {
                return (false, "MAC address is required");
            }

            if (reservation.IpAddress == IPAddress.None)
            {
                return (false, "IP address is required");
            }

            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            var success = await managerGrain.UpdateReservation(reservation);
            
            if (success)
            {
                _logger.LogInformation("Successfully updated reservation for MAC {MacAddress} -> IP {IpAddress}", 
                    reservation.MacAddress, reservation.IpAddress);
                return (true, null);
            }
            else
            {
                return (false, "Failed to update reservation");
            }
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
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            var success = await managerGrain.DeleteReservation(ipAddress);
            
            if (success)
            {
                _logger.LogInformation("Successfully deleted reservation for IP {IpAddress}", ipAddress);
                return (true, null);
            }
            else
            {
                return (false, "Failed to delete reservation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete reservation for IP {IpAddress}", ipAddress);
            return (false, ex.Message);
        }
    }

    public async Task<(bool hasConflict, string? conflictReason)> CheckConflictAsync(DhcpReservation reservation)
    {
        try
        {
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.HasConflict(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check conflict for reservation MAC {MacAddress} -> IP {IpAddress}", 
                reservation.MacAddress, reservation.IpAddress);
            return (true, "Error checking for conflicts");
        }
    }

    public async Task<DhcpReservation?> GetReservationForMacAsync(string macAddress)
    {
        try
        {
            var managerGrain = _grainFactory.GetGrain<IDhcpReservationManagerGrain>(0);
            return await managerGrain.GetReservationForMac(macAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation for MAC {MacAddress}", macAddress);
            return null;
        }
    }
}
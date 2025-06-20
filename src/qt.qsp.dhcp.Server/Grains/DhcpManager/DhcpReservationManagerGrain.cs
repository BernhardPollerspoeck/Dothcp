using Orleans;
using Orleans.Runtime;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpReservationManagerGrain : Grain, IDhcpReservationManagerGrain
{
    private readonly ILogger<DhcpReservationManagerGrain> _logger;
    private readonly IPersistentState<ReservationManagerState> _state;

    public DhcpReservationManagerGrain(
        [PersistentState("reservationManager", "File")] IPersistentState<ReservationManagerState> state,
        ILogger<DhcpReservationManagerGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public async Task<IList<DhcpReservation>> GetAllReservations()
    {
        var reservations = new List<DhcpReservation>();
        
        foreach (var ipAddress in _state.State.ReservationIps)
        {
            var reservationGrain = GrainFactory.GetGrain<IDhcpReservationGrain>(ipAddress);
            var reservation = await reservationGrain.GetReservation();
            if (reservation != null)
            {
                reservations.Add(reservation);
            }
        }
        
        return reservations;
    }

    public async Task<DhcpReservation?> GetReservationByMac(string macAddress)
    {
        foreach (var ipAddress in _state.State.ReservationIps)
        {
            var reservationGrain = GrainFactory.GetGrain<IDhcpReservationGrain>(ipAddress);
            var reservation = await reservationGrain.GetReservation();
            if (reservation != null && reservation.IsValidForMac(macAddress))
            {
                return reservation;
            }
        }
        
        return null;
    }

    public async Task<DhcpReservation?> GetReservationByIp(IPAddress ipAddress)
    {
        var reservationGrain = GrainFactory.GetGrain<IDhcpReservationGrain>(ipAddress.ToString());
        return await reservationGrain.GetReservation();
    }

    public async Task<bool> AddReservation(DhcpReservation reservation)
    {
        try
        {
            // Check for conflicts
            var (hasConflict, conflictReason) = await HasConflict(reservation);
            if (hasConflict)
            {
                _logger.LogWarning("Cannot add reservation due to conflict: {ConflictReason}", conflictReason);
                return false;
            }

            var reservationGrain = GrainFactory.GetGrain<IDhcpReservationGrain>(reservation.IpAddress.ToString());
            var success = await reservationGrain.CreateReservation(reservation);
            
            if (success)
            {
                _state.State.ReservationIps.Add(reservation.IpAddress.ToString());
                await _state.WriteStateAsync();
                
                _logger.LogInformation("Added reservation for MAC {MacAddress} -> IP {IpAddress}", 
                    reservation.MacAddress, reservation.IpAddress);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add reservation for MAC {MacAddress} -> IP {IpAddress}", 
                reservation.MacAddress, reservation.IpAddress);
            return false;
        }
    }

    public async Task<bool> UpdateReservation(DhcpReservation reservation)
    {
        try
        {
            var reservationGrain = GrainFactory.GetGrain<IDhcpReservationGrain>(reservation.IpAddress.ToString());
            var success = await reservationGrain.UpdateReservation(reservation);
            
            if (success)
            {
                // Ensure IP is tracked in our state
                if (!_state.State.ReservationIps.Contains(reservation.IpAddress.ToString()))
                {
                    _state.State.ReservationIps.Add(reservation.IpAddress.ToString());
                    await _state.WriteStateAsync();
                }
                
                _logger.LogInformation("Updated reservation for MAC {MacAddress} -> IP {IpAddress}", 
                    reservation.MacAddress, reservation.IpAddress);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update reservation for MAC {MacAddress} -> IP {IpAddress}", 
                reservation.MacAddress, reservation.IpAddress);
            return false;
        }
    }

    public async Task<bool> DeleteReservation(IPAddress ipAddress)
    {
        try
        {
            var reservationGrain = GrainFactory.GetGrain<IDhcpReservationGrain>(ipAddress.ToString());
            var success = await reservationGrain.DeleteReservation();
            
            if (success)
            {
                _state.State.ReservationIps.Remove(ipAddress.ToString());
                await _state.WriteStateAsync();
                
                _logger.LogInformation("Deleted reservation for IP {IpAddress}", ipAddress);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete reservation for IP {IpAddress}", ipAddress);
            return false;
        }
    }

    public async Task<(bool hasConflict, string? conflictReason)> HasConflict(DhcpReservation reservation)
    {
        // Check for IP address conflict
        var existingByIp = await GetReservationByIp(reservation.IpAddress);
        if (existingByIp != null && !string.Equals(existingByIp.MacAddress, reservation.MacAddress, StringComparison.OrdinalIgnoreCase))
        {
            return (true, $"IP address {reservation.IpAddress} is already reserved for MAC {existingByIp.MacAddress}");
        }

        // Check for MAC address conflict
        var existingByMac = await GetReservationByMac(reservation.MacAddress);
        if (existingByMac != null && !existingByMac.IpAddress.Equals(reservation.IpAddress))
        {
            return (true, $"MAC address {reservation.MacAddress} is already reserved for IP {existingByMac.IpAddress}");
        }

        return (false, null);
    }

    public async Task<DhcpReservation?> GetReservationForMac(string macAddress)
    {
        return await GetReservationByMac(macAddress);
    }
}

[GenerateSerializer]
public class ReservationManagerState
{
    [Id(0)]
    public ISet<string> ReservationIps { get; set; } = new HashSet<string>();
}
using Orleans;
using Orleans.Runtime;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpReservationGrain : Grain, IDhcpReservationGrain
{
    private readonly ILogger<DhcpReservationGrain> _logger;
    private readonly IPersistentState<DhcpReservation> _reservationState;

    public DhcpReservationGrain(
        [PersistentState("reservation", "File")] IPersistentState<DhcpReservation> reservationState,
        ILogger<DhcpReservationGrain> logger)
    {
        _reservationState = reservationState;
        _logger = logger;
    }

    public Task<DhcpReservation?> GetReservation()
    {
        // Return null if the reservation IP doesn't match the grain key (uninitialized)
        if (_reservationState.State.IpAddress == IPAddress.None || 
            _reservationState.State.IpAddress.ToString() != this.GetPrimaryKeyString())
        {
            return Task.FromResult<DhcpReservation?>(null);
        }
        
        return Task.FromResult<DhcpReservation?>(_reservationState.State);
    }

    public async Task<bool> CreateReservation(DhcpReservation reservation)
    {
        try
        {
            // Ensure the IP address matches the grain key
            if (reservation.IpAddress.ToString() != this.GetPrimaryKeyString())
            {
                _logger.LogWarning("Attempted to create reservation with mismatched IP address");
                return false;
            }

            // Check if reservation already exists
            if (_reservationState.State.IpAddress != IPAddress.None)
            {
                _logger.LogWarning("Reservation already exists for IP {IpAddress}", reservation.IpAddress);
                return false;
            }

            _reservationState.State = reservation;
            await _reservationState.WriteStateAsync();
            
            _logger.LogInformation("Created reservation for MAC {MacAddress} -> IP {IpAddress}", 
                reservation.MacAddress, reservation.IpAddress);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create reservation for IP {IpAddress}", reservation.IpAddress);
            return false;
        }
    }

    public async Task<bool> UpdateReservation(DhcpReservation reservation)
    {
        try
        {
            // Ensure the IP address matches the grain key
            if (reservation.IpAddress.ToString() != this.GetPrimaryKeyString())
            {
                _logger.LogWarning("Attempted to update reservation with mismatched IP address");
                return false;
            }

            // Check if reservation exists
            if (_reservationState.State.IpAddress == IPAddress.None)
            {
                _logger.LogWarning("Attempted to update non-existent reservation for IP {IpAddress}", 
                    reservation.IpAddress);
                return false;
            }

            _reservationState.State = reservation;
            await _reservationState.WriteStateAsync();
            
            _logger.LogInformation("Updated reservation for MAC {MacAddress} -> IP {IpAddress}", 
                reservation.MacAddress, reservation.IpAddress);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update reservation for IP {IpAddress}", reservation.IpAddress);
            return false;
        }
    }

    public async Task<bool> DeleteReservation()
    {
        try
        {
            if (_reservationState.State.IpAddress == IPAddress.None)
            {
                return true; // Already deleted/empty
            }

            var ipAddress = _reservationState.State.IpAddress;
            var macAddress = _reservationState.State.MacAddress;
            
            _reservationState.State = new DhcpReservation(); // Reset to empty state
            await _reservationState.WriteStateAsync();
            
            _logger.LogInformation("Deleted reservation for MAC {MacAddress} -> IP {IpAddress}", 
                macAddress, ipAddress);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete reservation");
            return false;
        }
    }

    public Task<bool> IsValidForMac(string macAddress)
    {
        if (_reservationState.State.IpAddress == IPAddress.None)
        {
            return Task.FromResult(false);
        }
        
        return Task.FromResult(_reservationState.State.IsValidForMac(macAddress));
    }

    public async Task MarkAsUsed()
    {
        if (_reservationState.State.IpAddress != IPAddress.None)
        {
            _reservationState.State.MarkAsUsed();
            await _reservationState.WriteStateAsync();
        }
    }
}
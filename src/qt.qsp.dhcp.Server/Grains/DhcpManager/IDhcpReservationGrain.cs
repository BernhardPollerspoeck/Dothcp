using Orleans;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

/// <summary>
/// Grain interface for managing individual DHCP reservations.
/// Identified by IP address.
/// </summary>
[Alias("IDhcpReservationGrain")]
public interface IDhcpReservationGrain : IGrainWithStringKey
{
    [Alias("GetReservation")]
    Task<DhcpReservation?> GetReservation();
    
    [Alias("CreateReservation")]
    Task<bool> CreateReservation(DhcpReservation reservation);
    
    [Alias("UpdateReservation")]
    Task<bool> UpdateReservation(DhcpReservation reservation);
    
    [Alias("DeleteReservation")]
    Task<bool> DeleteReservation();
    
    [Alias("IsValidForMac")]
    Task<bool> IsValidForMac(string macAddress);
    
    [Alias("MarkAsUsed")]
    Task MarkAsUsed();
}
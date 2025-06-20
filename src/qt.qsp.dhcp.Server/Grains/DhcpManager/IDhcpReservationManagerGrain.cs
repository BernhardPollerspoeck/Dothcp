using Orleans;
using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

/// <summary>
/// Grain interface for managing all DHCP reservations.
/// Singleton grain.
/// </summary>
[Alias("IDhcpReservationManagerGrain")]
public interface IDhcpReservationManagerGrain : IGrainWithIntegerKey
{
    [Alias("GetAllReservations")]
    Task<IList<DhcpReservation>> GetAllReservations();
    
    [Alias("GetReservationByMac")]
    Task<DhcpReservation?> GetReservationByMac(string macAddress);
    
    [Alias("GetReservationByIp")]
    Task<DhcpReservation?> GetReservationByIp(IPAddress ipAddress);
    
    [Alias("AddReservation")]
    Task<bool> AddReservation(DhcpReservation reservation);
    
    [Alias("UpdateReservation")]
    Task<bool> UpdateReservation(DhcpReservation reservation);
    
    [Alias("DeleteReservation")]
    Task<bool> DeleteReservation(IPAddress ipAddress);
    
    [Alias("HasConflict")]
    Task<(bool hasConflict, string? conflictReason)> HasConflict(DhcpReservation reservation);
    
    [Alias("GetReservationForMac")]
    Task<DhcpReservation?> GetReservationForMac(string macAddress);
}
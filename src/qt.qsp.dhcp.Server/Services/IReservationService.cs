using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net;

namespace qt.qsp.dhcp.Server.Services;

public interface IReservationService
{
    Task<IList<DhcpReservation>> GetAllReservationsAsync();
    Task<DhcpReservation?> GetReservationByMacAsync(string macAddress);
    Task<DhcpReservation?> GetReservationByIpAsync(IPAddress ipAddress);
    Task<(bool success, string? errorMessage)> AddReservationAsync(DhcpReservation reservation);
    Task<(bool success, string? errorMessage)> UpdateReservationAsync(DhcpReservation reservation);
    Task<(bool success, string? errorMessage)> DeleteReservationAsync(IPAddress ipAddress);
    Task<(bool hasConflict, string? conflictReason)> CheckConflictAsync(DhcpReservation reservation);
    Task<DhcpReservation?> GetReservationForMacAsync(string macAddress);
}
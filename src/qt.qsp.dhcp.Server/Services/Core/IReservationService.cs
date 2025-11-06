using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Services.Core;

public interface IReservationService
{
    Task<List<DhcpReservation>> GetAllReservationsAsync();
    Task<DhcpReservation?> GetReservationByMacAsync(string macAddress);
    Task<DhcpReservation?> GetReservationByIpAsync(IPAddress ipAddress);
    Task<(bool success, string? errorMessage)> AddReservationAsync(DhcpReservation reservation);
    Task<(bool success, string? errorMessage)> UpdateReservationAsync(DhcpReservation reservation);
    Task<(bool success, string? errorMessage)> DeleteReservationAsync(IPAddress ipAddress);
    Task<(bool hasConflict, string? conflictReason)> HasConflictAsync(DhcpReservation reservation, int? excludeId = null);
}

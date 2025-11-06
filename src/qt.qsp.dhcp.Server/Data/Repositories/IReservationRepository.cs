using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public interface IReservationRepository
{
    Task<DhcpReservation?> GetByIdAsync(int id);
    Task<DhcpReservation?> GetByIpAddressAsync(string ipAddress);
    Task<DhcpReservation?> GetByMacAddressAsync(string macAddress);
    Task<List<DhcpReservation>> GetAllAsync();
    Task<List<DhcpReservation>> GetActiveAsync();
    Task<DhcpReservation> AddAsync(DhcpReservation reservation);
    Task UpdateAsync(DhcpReservation reservation);
    Task DeleteAsync(int id);
    Task<bool> HasConflictAsync(string ipAddress, string macAddress, int? excludeId = null);
}

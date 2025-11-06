using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly DhcpDbContext _context;

    public ReservationRepository(DhcpDbContext context)
    {
        _context = context;
    }

    public async Task<DhcpReservation?> GetByIdAsync(int id)
    {
        return await _context.Reservations.FindAsync(id);
    }

    public async Task<DhcpReservation?> GetByIpAddressAsync(string ipAddress)
    {
        return await _context.Reservations
            .FirstOrDefaultAsync(r => r.IpAddressString == ipAddress);
    }

    public async Task<DhcpReservation?> GetByMacAddressAsync(string macAddress)
    {
        return await _context.Reservations
            .FirstOrDefaultAsync(r => r.MacAddress == macAddress);
    }

    public async Task<List<DhcpReservation>> GetAllAsync()
    {
        return await _context.Reservations.ToListAsync();
    }

    public async Task<List<DhcpReservation>> GetActiveAsync()
    {
        return await _context.Reservations
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task<DhcpReservation> AddAsync(DhcpReservation reservation)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task UpdateAsync(DhcpReservation reservation)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var reservation = await GetByIdAsync(id);
        if (reservation != null)
        {
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasConflictAsync(string ipAddress, string macAddress, int? excludeId = null)
    {
        var query = _context.Reservations
            .Where(r => r.IpAddressString == ipAddress || r.MacAddress == macAddress);

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public class LeaseRepository : ILeaseRepository
{
    private readonly DhcpDbContext _context;

    public LeaseRepository(DhcpDbContext context)
    {
        _context = context;
    }

    public async Task<DhcpLease?> GetByIdAsync(int id)
    {
        return await _context.Leases.FindAsync(id);
    }

    public async Task<DhcpLease?> GetByIpAddressAsync(string ipAddress)
    {
        return await _context.Leases
            .FirstOrDefaultAsync(l => l.IpAddressString == ipAddress);
    }

    public async Task<DhcpLease?> GetByMacAddressAsync(string macAddress)
    {
        return await _context.Leases
            .FirstOrDefaultAsync(l => l.MacAddress == macAddress);
    }

    public async Task<List<DhcpLease>> GetAllAsync()
    {
        return await _context.Leases.ToListAsync();
    }

    public async Task<List<DhcpLease>> GetActiveAsync()
    {
        return await _context.Leases
            .Where(l => l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Renewed)
            .ToListAsync();
    }

    public async Task<DhcpLease> AddAsync(DhcpLease lease)
    {
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();
        return lease;
    }

    public async Task UpdateAsync(DhcpLease lease)
    {
        _context.Leases.Update(lease);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var lease = await GetByIdAsync(id);
        if (lease != null)
        {
            _context.Leases.Remove(lease);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string ipAddress)
    {
        return await _context.Leases
            .AnyAsync(l => l.IpAddressString == ipAddress);
    }
}

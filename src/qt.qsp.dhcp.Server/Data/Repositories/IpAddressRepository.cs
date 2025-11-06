using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public class IpAddressRepository : IIpAddressRepository
{
    private readonly DhcpDbContext _context;

    public IpAddressRepository(DhcpDbContext context)
    {
        _context = context;
    }

    public async Task<IpAddressStatus?> GetByIpAddressAsync(string ipAddress)
    {
        return await _context.IpAddressStatuses
            .FirstOrDefaultAsync(s => s.IpAddressString == ipAddress);
    }

    public async Task<List<IpAddressStatus>> GetAllAsync()
    {
        return await _context.IpAddressStatuses.ToListAsync();
    }

    public async Task<IpAddressStatus> AddOrUpdateAsync(IpAddressStatus status)
    {
        if (status == null)
            throw new ArgumentNullException(nameof(status));

        if (string.IsNullOrEmpty(status.IpAddressString))
            throw new ArgumentException("IP address cannot be null or empty", nameof(status));

        var existing = await GetByIpAddressAsync(status.IpAddressString);
        if (existing != null)
        {
            existing.Status = status.Status;
            existing.ClientId = status.ClientId;
            _context.IpAddressStatuses.Update(existing);
        }
        else
        {
            _context.IpAddressStatuses.Add(status);
        }
        await _context.SaveChangesAsync();
        return existing ?? status;
    }

    public async Task DeleteAsync(string ipAddress)
    {
        var status = await GetByIpAddressAsync(ipAddress);
        if (status != null)
        {
            _context.IpAddressStatuses.Remove(status);
            await _context.SaveChangesAsync();
        }
    }
}

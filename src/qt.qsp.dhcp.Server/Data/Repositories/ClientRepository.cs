using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly DhcpDbContext _context;

    public ClientRepository(DhcpDbContext context)
    {
        _context = context;
    }

    public async Task<ClientInfo?> GetByClientIdAsync(string clientId)
    {
        return await _context.ClientInfos.FindAsync(clientId);
    }

    public async Task<List<ClientInfo>> GetAllAsync()
    {
        return await _context.ClientInfos.ToListAsync();
    }

    public async Task<ClientInfo> AddOrUpdateAsync(ClientInfo clientInfo)
    {
        if (clientInfo == null)
            throw new ArgumentNullException(nameof(clientInfo));

        if (string.IsNullOrEmpty(clientInfo.ClientId))
            throw new ArgumentException("ClientId cannot be null or empty", nameof(clientInfo));

        var existing = await GetByClientIdAsync(clientInfo.ClientId);
        if (existing != null)
        {
            existing.AssignedIpAddress = clientInfo.AssignedIpAddress;
            existing.State = clientInfo.State;
            existing.LastSeen = clientInfo.LastSeen;
            existing.HostName = clientInfo.HostName;
            existing.DomainName = clientInfo.DomainName;
            _context.ClientInfos.Update(existing);
        }
        else
        {
            _context.ClientInfos.Add(clientInfo);
        }
        await _context.SaveChangesAsync();
        return existing ?? clientInfo;
    }

    public async Task DeleteAsync(string clientId)
    {
        var clientInfo = await GetByClientIdAsync(clientId);
        if (clientInfo != null)
        {
            _context.ClientInfos.Remove(clientInfo);
            await _context.SaveChangesAsync();
        }
    }
}

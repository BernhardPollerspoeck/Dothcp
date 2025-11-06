using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public interface IClientRepository
{
    Task<ClientInfo?> GetByClientIdAsync(string clientId);
    Task<List<ClientInfo>> GetAllAsync();
    Task<ClientInfo> AddOrUpdateAsync(ClientInfo clientInfo);
    Task DeleteAsync(string clientId);
}

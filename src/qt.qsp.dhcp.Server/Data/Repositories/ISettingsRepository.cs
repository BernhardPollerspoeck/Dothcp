using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public interface ISettingsRepository
{
    Task<AppSetting?> GetByKeyAsync(string key);
    Task<List<AppSetting>> GetAllAsync();
    Task<AppSetting> AddOrUpdateAsync(AppSetting setting);
    Task DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
}

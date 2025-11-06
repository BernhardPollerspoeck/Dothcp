using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly DhcpDbContext _context;

    public SettingsRepository(DhcpDbContext context)
    {
        _context = context;
    }

    public async Task<AppSetting?> GetByKeyAsync(string key)
    {
        return await _context.Settings.FindAsync(key);
    }

    public async Task<List<AppSetting>> GetAllAsync()
    {
        return await _context.Settings.ToListAsync();
    }

    public async Task<AppSetting> AddOrUpdateAsync(AppSetting setting)
    {
        var existing = await GetByKeyAsync(setting.Key);
        if (existing != null)
        {
            existing.Value = setting.Value;
            _context.Settings.Update(existing);
        }
        else
        {
            _context.Settings.Add(setting);
        }
        await _context.SaveChangesAsync();
        return existing ?? setting;
    }

    public async Task DeleteAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        if (setting != null)
        {
            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _context.Settings.AnyAsync(s => s.Key == key);
    }
}

namespace qt.qsp.dhcp.Server.Services.Core;

public interface IConfigurationService
{
    Task<TResult?> GetSettingAsync<TResult>(string key);
    Task SetSettingAsync<TValue>(string key, TValue value);
    Task<bool> HasSettingAsync(string key);
}

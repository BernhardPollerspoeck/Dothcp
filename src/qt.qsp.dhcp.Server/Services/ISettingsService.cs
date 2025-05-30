namespace qt.qsp.dhcp.Server.Services;

public interface ISettingsService
{
	Task<TResult> GetSettingAsync<TResult>(string key);
	Task SetSettingAsync(string key, string value);
	Task<bool> ValidateSettingAsync(string key, string value);
}
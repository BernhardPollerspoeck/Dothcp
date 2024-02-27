namespace qt.qsp.dhcp.Server.Services;

public interface ISettingsLoaderService
{
	Task<TResult> GetSetting<TResult>(string key);
}

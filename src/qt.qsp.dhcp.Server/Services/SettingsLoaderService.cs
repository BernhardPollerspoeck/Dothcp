using qt.qsp.dhcp.Server.Services.Core;

namespace qt.qsp.dhcp.Server.Services;

public class SettingsLoaderService(IConfigurationService configurationService)
	: ISettingsLoaderService
{
	#region ISettingsLoaderService
	public async Task<TResult> GetSetting<TResult>(string key)
	{
		var value = await configurationService.GetSettingAsync<TResult>(key);
		return value ?? default!;
	}
	#endregion
}

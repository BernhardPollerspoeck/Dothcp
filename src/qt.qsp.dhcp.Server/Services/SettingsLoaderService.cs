using qt.qsp.dhcp.Server.Services.Core;
using qt.qsp.dhcp.Server.Constants;

namespace qt.qsp.dhcp.Server.Services;

public class SettingsLoaderService(IConfigurationService configurationService)
	: ISettingsLoaderService
{
	#region ISettingsLoaderService
	public async Task<TResult> GetSetting<TResult>(string key)
	{
		var value = await configurationService.GetSettingAsync<TResult>(key);

		// Allow null for optional settings
		if (value == null)
		{
			// DNS and NTP servers are optional
			if (key == SettingsConstants.DHCP_LEASE_DNS || key == SettingsConstants.DHCP_LEASE_NTP_SERVERS)
			{
				return default!;
			}

			throw new InvalidOperationException($"Required setting '{key}' is not configured. Please configure it in the Settings page.");
		}

		return value;
	}
	#endregion
}

using qt.qsp.dhcp.Server.Grains.Settings;

namespace qt.qsp.dhcp.Server.Services;

public class SettingsLoaderService(IGrainFactory grainFactory)
	: ISettingsLoaderService
{
	#region ISettingsLoaderService
	public Task<TResult> GetSetting<TResult>(string key)
	{
		return grainFactory
			.GetGrain<ISettingsGrain>(key)
			.GetValue<TResult>();
	}
	#endregion
}

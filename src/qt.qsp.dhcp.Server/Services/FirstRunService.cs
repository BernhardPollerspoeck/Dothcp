using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Services.Core;

namespace qt.qsp.dhcp.Server.Services;

public interface IFirstRunService
{
	Task<bool> IsFirstRunAsync();
	Task MarkSetupCompletedAsync();
}

public class FirstRunService(IConfigurationService configurationService) : IFirstRunService
{
	public async Task<bool> IsFirstRunAsync()
	{
		try
		{
			// Check if core required settings exist
			var hasSubnet = await configurationService.HasSettingAsync(SettingsConstants.DHCP_LEASE_SUBNET);
			var hasRouter = await configurationService.HasSettingAsync(SettingsConstants.DHCP_LEASE_ROUTER);
			var hasRangeLow = await configurationService.HasSettingAsync(SettingsConstants.DHCP_RANGE_LOW);
			var hasRangeHigh = await configurationService.HasSettingAsync(SettingsConstants.DHCP_RANGE_HIGH);

			// If any of the core settings are missing, it's a first run
			return !hasSubnet || !hasRouter || !hasRangeLow || !hasRangeHigh;
		}
		catch
		{
			// If there's any error checking settings, assume it's first run
			return true;
		}
	}

	public async Task MarkSetupCompletedAsync()
	{
		// This could be used to mark setup as completed in the future
		// For now, the existence of settings indicates completion
		await Task.CompletedTask;
	}
}
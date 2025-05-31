using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.Settings;

namespace qt.qsp.dhcp.Server.Services;

public interface IFirstRunService
{
	Task<bool> IsFirstRunAsync();
	Task MarkSetupCompletedAsync();
}

public class FirstRunService(IClusterClient clusterClient) : IFirstRunService
{
	public async Task<bool> IsFirstRunAsync()
	{
		try
		{
			// Check if core required settings exist
			var subnetGrain = clusterClient.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET);
			var routerGrain = clusterClient.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER);
			var rangeLowGrain = clusterClient.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW);
			var rangeHighGrain = clusterClient.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH);
			
			// If any of the core settings are missing, it's a first run
			return !await subnetGrain.HasValue() || 
			       !await routerGrain.HasValue() ||
			       !await rangeLowGrain.HasValue() ||
			       !await rangeHighGrain.HasValue();
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
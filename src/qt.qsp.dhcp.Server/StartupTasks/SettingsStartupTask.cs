using Orleans.Runtime;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains;

namespace qt.qsp.dhcp.Server.StartupTasks;

public class SettingsStartupTask(
	IClusterClient clusterClient)
	: IStartupTask
{
	private static readonly List<KeyValuePair<string, string>> _presetValues =
	[
		new(SettingsConstants.DHCP_RANGE_LOW, "100"),
		new(SettingsConstants.DHCP_RANGE_HIGH, "200"),

		new(SettingsConstants.DHCP_LEASE_TIME, "24:00:00"),
		new(SettingsConstants.DHCP_LEASE_RENEWAL, "12:00:00"),
		new(SettingsConstants.DHCP_LEASE_REBINDING, "21:00:00"),

		new(SettingsConstants.DHCP_LEASE_SUBNET, "255.255.255.0"),
		new(SettingsConstants.DHCP_LEASE_ROUTER, "192.168.0.1"),

		new(SettingsConstants.DHCP_LEASE_DNS, "192.168.0.1;8.8.8.8"),
		new(SettingsConstants.DHCP_LEASE_NTP_SERVERS, "141.76.10.160;134.28.202.2")
	];

	public async Task Execute(CancellationToken cancellationToken)
	{
		foreach (var item in _presetValues)
		{
			var grain = clusterClient.GetGrain<ISettingsGrain>(item.Key);
			if (await grain.HasValue())
			{
				continue;
			}
			await grain.SetValue(item.Value);
		}
	}
}

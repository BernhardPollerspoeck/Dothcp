using qt.qsp.dhcp.Server.Grains.Settings;
using qt.qsp.dhcp.Server.Constants;
using System.Net;

namespace qt.qsp.dhcp.Server.Services;

public class SettingsService(IGrainFactory grainFactory) : ISettingsService
{
	public Task<TResult> GetSettingAsync<TResult>(string key)
	{
		return grainFactory
			.GetGrain<ISettingsGrain>(key)
			.GetValue<TResult>();
	}

	public Task SetSettingAsync(string key, string value)
	{
		return grainFactory
			.GetGrain<ISettingsGrain>(key)
			.SetValue(value);
	}

	public Task<bool> ValidateSettingAsync(string key, string value)
	{
		return Task.FromResult(ValidateSetting(key, value));
	}

	private static bool ValidateSetting(string key, string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			// DNS is optional, so empty/null is valid
			return key == SettingsConstants.DHCP_LEASE_DNS;
		}

		return key switch
		{
			SettingsConstants.DHCP_RANGE_LOW => byte.TryParse(value, out var rangeLow) && rangeLow > 0 && rangeLow < 255,
			SettingsConstants.DHCP_RANGE_HIGH => byte.TryParse(value, out var rangeHigh) && rangeHigh > 0 && rangeHigh < 255,
			SettingsConstants.DHCP_LEASE_TIME => TimeSpan.TryParse(value, out var leaseTime) && leaseTime > TimeSpan.Zero,
			SettingsConstants.DHCP_LEASE_RENEWAL => TimeSpan.TryParse(value, out var renewalTime) && renewalTime > TimeSpan.Zero,
			SettingsConstants.DHCP_LEASE_REBINDING => TimeSpan.TryParse(value, out var rebindingTime) && rebindingTime > TimeSpan.Zero,
			SettingsConstants.DHCP_LEASE_SUBNET => IsValidIpAddress(value),
			SettingsConstants.DHCP_LEASE_ROUTER => IsValidIpAddress(value),
			SettingsConstants.DHCP_LEASE_DNS => ValidateDnsServers(value),
			_ => true // Allow unknown settings for extensibility
		};
	}

	private static bool IsValidIpAddress(string ipAddress)
	{
		return IPAddress.TryParse(ipAddress, out _);
	}

	private static bool ValidateDnsServers(string dnsServers)
	{
		if (string.IsNullOrWhiteSpace(dnsServers))
			return true; // DNS is optional

		var servers = dnsServers.Split(';', StringSplitOptions.RemoveEmptyEntries);
		return servers.All(IsValidIpAddress);
	}
}
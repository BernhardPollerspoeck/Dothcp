using qt.qsp.dhcp.Server.Services.Core;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Utilities;
using System.Net;

namespace qt.qsp.dhcp.Server.Services;

public class SettingsService(IConfigurationService configurationService, INetworkUtilityService networkUtilityService) : ISettingsService
{
	public async Task<TResult> GetSettingAsync<TResult>(string key)
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

	public Task SetSettingAsync(string key, string value)
	{
		return configurationService.SetSettingAsync(key, value);
	}

	public Task<bool> ValidateSettingAsync(string key, string value)
	{
		return Task.FromResult(ValidateSetting(key, value));
	}

	private static bool ValidateSetting(string key, string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			// DNS and NTP servers are optional, empty/null is valid
			return key == SettingsConstants.DHCP_LEASE_DNS || key == SettingsConstants.DHCP_LEASE_NTP_SERVERS;
		}

		return key switch
		{
			SettingsConstants.DHCP_RANGE_LOW => byte.TryParse(value, out var rangeLow) && rangeLow > 0 && rangeLow < 255,
			SettingsConstants.DHCP_RANGE_HIGH => byte.TryParse(value, out var rangeHigh) && rangeHigh > 0 && rangeHigh < 255,
			SettingsConstants.DHCP_LEASE_TIME => TimeSpan.TryParse(value, out var leaseTime) && leaseTime > TimeSpan.Zero,
			SettingsConstants.DHCP_LEASE_RENEWAL => TimeSpan.TryParse(value, out var renewalTime) && renewalTime > TimeSpan.Zero,
			SettingsConstants.DHCP_LEASE_REBINDING => TimeSpan.TryParse(value, out var rebindingTime) && rebindingTime > TimeSpan.Zero,
			SettingsConstants.DHCP_LEASE_SUBNET => IsValidSubnetMask(value),
			SettingsConstants.DHCP_LEASE_ROUTER => IsValidIpAddress(value),
			SettingsConstants.DHCP_LEASE_DNS => ValidateDnsServers(value),
			SettingsConstants.DHCP_LEASE_NTP_SERVERS => ValidateNtpServers(value),
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

	private static bool ValidateNtpServers(string ntpServers)
	{
		if (string.IsNullOrWhiteSpace(ntpServers))
			return true; // NTP is optional

		var servers = ntpServers.Split(';', StringSplitOptions.RemoveEmptyEntries);
		return servers.All(IsValidIpAddress);
	}

	private static bool IsValidSubnetMask(string subnetMask)
	{
		if (!IPAddress.TryParse(subnetMask, out var ipAddress))
			return false;
		
		// Convert to bytes and check if it's a valid subnet mask
		var bytes = ipAddress.GetAddressBytes();
		if (bytes.Length != 4)
			return false;
		
		// Convert to 32-bit integer
		var mask = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
		
		// Check if it's a valid subnet mask (contiguous 1s followed by contiguous 0s)
		// Invert the mask and add 1, result should be a power of 2
		var inverted = ~mask + 1;
		return (inverted & (inverted - 1)) == 0;
	}

	public Dictionary<string, string> GetAvailableNetworkInterfaces()
	{
		return networkUtilityService.GetAvailableNetworkInterfaces();
	}
}
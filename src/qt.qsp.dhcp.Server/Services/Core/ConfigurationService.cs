using Microsoft.Extensions.Logging;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using System.Text.Json;

namespace qt.qsp.dhcp.Server.Services.Core;

public class ConfigurationService : IConfigurationService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ISettingsRepository settingsRepository, ILogger<ConfigurationService> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<TResult?> GetSettingAsync<TResult>(string key)
    {
        try
        {
            var setting = await _settingsRepository.GetByKeyAsync(key);

            if (setting == null)
            {
                return default;
            }

            var value = setting.Value;

            // Handle type conversions
            if (typeof(TResult) == typeof(string))
            {
                return (TResult)(object)value;
            }

            if (typeof(TResult) == typeof(byte))
            {
                return (TResult)(object)byte.Parse(value);
            }

            if (typeof(TResult) == typeof(byte[]))
            {
                return (TResult)(object)Convert.FromBase64String(value);
            }

            if (typeof(TResult) == typeof(TimeSpan))
            {
                return (TResult)(object)TimeSpan.Parse(value);
            }

            if (typeof(TResult) == typeof(string[]))
            {
                return (TResult)(object)JsonSerializer.Deserialize<string[]>(value)!;
            }

            // For complex types, use JSON deserialization
            return JsonSerializer.Deserialize<TResult>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting {Key}", key);
            return default;
        }
    }

    public async Task SetSettingAsync<TValue>(string key, TValue value)
    {
        try
        {
            string stringValue;

            if (value is string strValue)
            {
                stringValue = strValue;
            }
            else if (value is byte byteValue)
            {
                stringValue = byteValue.ToString();
            }
            else if (value is byte[] byteArrayValue)
            {
                stringValue = Convert.ToBase64String(byteArrayValue);
            }
            else if (value is TimeSpan timeSpanValue)
            {
                stringValue = timeSpanValue.ToString();
            }
            else
            {
                // For complex types, use JSON serialization
                stringValue = JsonSerializer.Serialize(value);
            }

            var setting = new AppSetting
            {
                Key = key,
                Value = stringValue
            };

            await _settingsRepository.AddOrUpdateAsync(setting);

            _logger.LogDebug("Setting {Key} updated", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting {Key}", key);
            throw;
        }
    }

    public async Task<bool> HasSettingAsync(string key)
    {
        return await _settingsRepository.ExistsAsync(key);
    }
}

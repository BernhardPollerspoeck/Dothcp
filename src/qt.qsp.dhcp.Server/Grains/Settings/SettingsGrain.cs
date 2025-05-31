using Orleans.Runtime;

namespace qt.qsp.dhcp.Server.Grains.Settings;

public class SettingsGrain(
    [PersistentState("setting", "File")] IPersistentState<AppSetting> state,
    ILogger<SettingsGrain> logger)
    : Grain, ISettingsGrain
{

    #region ISettingsGrain
    public Task<bool> HasValue()
    {
        return Task.FromResult(state is { RecordExists: true, State.Value: not null });
    }
    public Task<TResult> GetValue<TResult>()
    {
        var val = state.State.Value;
        
        // Handle null values gracefully - return appropriate defaults
        if (val == null)
        {
            return Task.FromResult(default(TResult)!);
        }
        
        var typeHolder = typeof(TResult).Name;
        return Task.FromResult(
            typeof(TResult).Name switch
            {//TODO: simplify
                "Byte" => (TResult)Convert.ChangeType(byte.Parse(val), typeof(TResult)),
                "Byte[]" => (TResult)Convert.ChangeType(val.Split('.').Select(byte.Parse).ToArray(), typeof(TResult)),

                "String" => (TResult)Convert.ChangeType(val, typeof(TResult)),
                "String[]" => (TResult)Convert.ChangeType(val.Split(';'), typeof(TResult)),

                "TimeSpan" => (TResult)Convert.ChangeType(new TimeSpan(int.Parse(val.Split(':')[0]), int.Parse(val.Split(':')[1]), int.Parse(val.Split(':')[2])), typeof(TResult)),

                _ => default!,//TODO: remove !!!!
            });
    }
    public Task SetValue(string value)
    {
        logger.LogDebug("Set value {value} in {id}", value, this.GetPrimaryKeyString());
        state.State.Value = value;
        return state.WriteStateAsync();
    }
    #endregion
}

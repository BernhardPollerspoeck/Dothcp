using Orleans.Runtime;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;

namespace qt.qsp.dhcp.Server.Grains;

[Alias("ISettingsGrain")]
public interface ISettingsGrain : IGrainWithStringKey
{
	[Alias("SetValue")]
	Task SetValue(string value);

	//[Alias("GetValue")]
	//Task<string?> GetValue();

	[Alias("GetValue")]
	Task<TResult> GetValue<TResult>();

	[Alias("HasValue")]
	Task<bool> HasValue();
}

public class SettingsGrain(
	[PersistentState("setting", "File")]
	IPersistentState<AppSetting> state)
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
		var typeHolder = default(TResult);
		return Task.FromResult(
			typeHolder switch
			{//TODO: simplify
				byte => (TResult)Convert.ChangeType(byte.Parse(val), typeof(TResult)),

				string => (TResult)Convert.ChangeType(val, typeof(TResult)),
				string[] => (TResult)Convert.ChangeType(val.Split(';'), typeof(TResult)),

				TimeSpan => (TResult)Convert.ChangeType(new TimeSpan(int.Parse(val.Split(':')[0]), int.Parse(val.Split(':')[1]), int.Parse(val.Split(':')[2])), typeof(TResult)),

				_ => default!,//TODO: remove !!!!
			});
	}

	public Task SetValue(string value)
	{
		state.State.Value = value;
		return state.WriteStateAsync();
	}
	#endregion
}

public class AppSetting
{
	public string Value { get; set; } = default!;//we asume we only call for settings that are created!

}
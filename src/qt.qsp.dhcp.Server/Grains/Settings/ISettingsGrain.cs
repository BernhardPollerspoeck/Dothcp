namespace qt.qsp.dhcp.Server.Grains.Settings;

[Alias("ISettingsGrain")]
public interface ISettingsGrain : IGrainWithStringKey
{
    [Alias("SetValue")]
    Task SetValue(string value);

    [Alias("GetValue")]
    Task<TResult> GetValue<TResult>();

    [Alias("HasValue")]
    Task<bool> HasValue();
}

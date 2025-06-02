namespace qt.qsp.dhcp.Server.Services;

public interface IDhcpServerService
{
    ServerState CurrentState { get; }
    Task<bool> StartAsync();
    Task<bool> StopAsync();
    Task<bool> RestartAsync();
    bool IsEnabled { get; }
    event EventHandler<ServerState>? StateChanged;
}
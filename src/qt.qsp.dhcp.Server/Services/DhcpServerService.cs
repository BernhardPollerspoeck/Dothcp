namespace qt.qsp.dhcp.Server.Services;

public class DhcpServerService : IDhcpServerService
{
    private volatile bool _isEnabled = false;
    private ServerState _currentState = ServerState.Stopped;
    private readonly ILogger<DhcpServerService> _logger;

    public event EventHandler<ServerState>? StateChanged;

    public DhcpServerService(ILogger<DhcpServerService> logger)
    {
        _logger = logger;
    }

    public ServerState CurrentState => _currentState;
    public bool IsEnabled => _isEnabled;

    public Task<bool> StartAsync()
    {
        try
        {
            if (_currentState == ServerState.Running)
            {
                return Task.FromResult(true);
            }

            _isEnabled = true;
            SetState(ServerState.Running);
            _logger.LogInformation("DHCP Server started");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start DHCP Server");
            SetState(ServerState.Error);
            return Task.FromResult(false);
        }
    }

    public Task<bool> StopAsync()
    {
        try
        {
            if (_currentState == ServerState.Stopped)
            {
                return Task.FromResult(true);
            }

            _isEnabled = false;
            SetState(ServerState.Stopped);
            _logger.LogInformation("DHCP Server stopped");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop DHCP Server");
            SetState(ServerState.Error);
            return Task.FromResult(false);
        }
    }

    public async Task<bool> RestartAsync()
    {
        _logger.LogInformation("Restarting DHCP Server");
        var stopResult = await StopAsync();
        if (!stopResult)
        {
            return false;
        }

        // Brief delay before restart
        await Task.Delay(1000);
        return await StartAsync();
    }

    private void SetState(ServerState newState)
    {
        if (_currentState != newState)
        {
            _currentState = newState;
            StateChanged?.Invoke(this, newState);
        }
    }
}
namespace qt.qsp.dhcp.Server.Services;

public class DhcpServerService : IDhcpServerService
{
    private volatile bool _isEnabled = false;
    private ServerState _currentState = ServerState.Stopped;
    private readonly ILogger<DhcpServerService> _logger;
    private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

    public event EventHandler<ServerState>? StateChanged;

    public DhcpServerService(ILogger<DhcpServerService> logger)
    {
        _logger = logger;
    }

    public ServerState CurrentState => _currentState;
    public bool IsEnabled => _isEnabled;

    public async Task<bool> StartAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_currentState == ServerState.Running)
            {
                return true;
            }

            _isEnabled = true;
            SetState(ServerState.Running);
            _logger.LogInformation("DHCP Server started");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start DHCP Server");
            SetState(ServerState.Error);
            return false;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<bool> StopAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_currentState == ServerState.Stopped)
            {
                return true;
            }

            _isEnabled = false;
            SetState(ServerState.Stopped);
            _logger.LogInformation("DHCP Server stopped");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop DHCP Server");
            SetState(ServerState.Error);
            return false;
        }
        finally
        {
            _stateLock.Release();
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
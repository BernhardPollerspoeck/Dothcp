using System.Net.Sockets;
using System.Net;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Services;

namespace qt.qsp.dhcp.Server.Workers;

public class NetworkListener : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IDhcpServerService _serverService;
	private readonly ILogger<NetworkListener> _logger;

	public NetworkListener(
		IServiceProvider serviceProvider,
		IDhcpServerService serverService,
		ILogger<NetworkListener> logger)
	{
		_serviceProvider = serviceProvider;
		_serverService = serverService;
		_logger = logger;
	}
	#region BackgroundService
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var listener = new UdpClient(67);
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var incommingData = await listener.ReceiveAsync(stoppingToken);

				// Check if the DHCP server is enabled before processing requests
				if (!_serverService.IsEnabled)
				{
					continue;
				}

				var incommingMessage = ParseMessage(incommingData.Buffer);

				var id = incommingMessage.GetClientId();
				if (id is null)
				{
					continue;
				}

				// Create a scope for scoped services
				using var scope = _serviceProvider.CreateScope();
				var dhcpMessageHandler = scope.ServiceProvider.GetRequiredService<IDhcpMessageHandler>();

				var responseMessage = await dhcpMessageHandler.HandleMessageAsync(incommingMessage, id);
				if (responseMessage is null)
				{
					continue;
				}

				await SendResponse(
					responseMessage: responseMessage,
					client: listener,
					responseCastType: incommingMessage.ResponseCastType,
					clientAddress: incommingMessage.ClientIpAdress,
					remoteEndpoint: incommingData.RemoteEndPoint);
			}
			catch (TaskCanceledException)
			{
				_logger.LogInformation("DHCP Network Listener shutting down gracefully");
			}
			catch (SocketException ex)
			{
				_logger.LogError(ex, "Socket error occurred while processing DHCP packets");
			}
		}
	}
	#endregion

	#region data handling
	private DhcpMessage ParseMessage(byte[] buffer)
	{
		return DhcpMessage.Parse(buffer);
	}

	private static Task<int> SendResponse(
		DhcpMessage responseMessage,
		UdpClient client,
		EResponseCastType responseCastType,
		uint clientAddress,
		IPEndPoint? remoteEndpoint)
	{
		var responseData = responseMessage.ToData().ToArray();
		return client.SendAsync(
			responseData,
			responseData.Length,
			responseCastType is EResponseCastType.Broadcast || clientAddress is 0x00000000 || remoteEndpoint is null
				? new IPEndPoint(IPAddress.Parse("255.255.255.255"), 68)
				: remoteEndpoint);
	}
	#endregion
}

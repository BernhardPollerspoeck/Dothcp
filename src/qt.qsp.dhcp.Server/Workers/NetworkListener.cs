using System.Net.Sockets;
using System.Net;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Models.Enumerations;
using qt.qsp.dhcp.Server.Grains.MessageParser;
using qt.qsp.dhcp.Server.Grains.DhcpManager;

namespace qt.qsp.dhcp.Server.Workers;

public class NetworkListener(IGrainFactory grainFactory)
	: BackgroundService
{
	#region BackgroundService
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var listener = new UdpClient(67);
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var incommingData = await listener.ReceiveAsync(stoppingToken);
				var incommingMessage = await ParseMessage(incommingData.Buffer);

				var id = incommingMessage.GetClientId();
				if (id is null)
				{
					continue;
				}

				var responseMessage = await GetResponseMessage(id, incommingMessage);
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
				//TODO: log shutdown
			}
			catch (SocketException)
			{
				//TODO: handle error 
			}
		}
	}
	#endregion

	#region data handling
	private Task<DhcpMessage> ParseMessage(byte[] buffer)
	{
		var parserGrain = grainFactory.GetGrain<IMessageParserGrain>(Guid.NewGuid().ToString());
		return parserGrain.Parse(buffer);
	}

	private Task<DhcpMessage?> GetResponseMessage(string id, DhcpMessage message)
	{
		var leaseGrain = grainFactory.GetGrain<IDhcpManagerGrain>(id);
		return leaseGrain.HandleMessage(message);
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

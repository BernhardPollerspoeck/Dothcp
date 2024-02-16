
using System.Net.Sockets;
using System.Net;
using System.Text;
using qt.qsp.dhcp.Server.Grains;

namespace qt.qsp.dhcp.Server.Workers;

public class NetworkListener(IGrainFactory grainFactory)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var listener = new UdpClient(67);
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var result = await listener.ReceiveAsync(stoppingToken);

					var parserGrain = grainFactory.GetGrain<IMessageParserGrain>(Guid.NewGuid().ToString());
					var message = await parserGrain.Parse(result.Buffer);

					var mac = message.GetMacAddress();
					if (mac is not null)
					{
						var leaseGrain = grainFactory.GetGrain<IDhcpLeaseGrain>(mac);
						var clientResponseMessage = await leaseGrain.HandleMessage(message);
						if (clientResponseMessage is not null)
						{
							//TODO: await listener.SendAsync(clientResponseMessage.Data, clientResponseMessage.Data.Length, result.RemoteEndPoint);
						}

					}
				}
				catch (TaskCanceledException)
				{
					//TODO: log shutdown
				}
				catch (SocketException e)
				{
					//TODO: handle error 
				}
			}
		}
		finally
		{
			listener.Close();
		}
	}
}

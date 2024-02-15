using Orleans.Concurrency;
using Orleans.Runtime;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace qt.qsp.dhcp.Server.Grains;

[Alias("IDhcpLeaseGrain")]
public interface IDhcpLeaseGrain : IGrainWithStringKey
{
	[Alias("HandleMessage")]
	Task<DhcpMessage?> HandleMessage(DhcpMessage message);
}

public class DhcpLeaseGrain
	: Grain, IDhcpLeaseGrain
{
	//private readonly IPersistentState<DhcpLease> _state;

	public DhcpLeaseGrain(
		//[PersistentState("dhcp", "File")]
		//IPersistentState<DhcpLease> state
		)
	{
		//_state = state;
	}

	public Task<DhcpMessage?> HandleMessage(DhcpMessage message)
	{


		return Task.FromResult<DhcpMessage?>(default);
	}
}

[GenerateSerializer]
public class DhcpMessage
{
	[Id(0)]
	public required EMessageDirection Direction { get; init; }
	[Id(1)]
	public required EHardwareType HardwareType { get; init; }
	[Id(2)]
	public required byte ClientIdLength { get; init; }
	[Id(3)]
	public required byte Hops { get; init; }
	[Id(4)]
	public required uint TransactionId { get; init; }
	[Id(5)]
	public required EResponseCastType ResponseCastType { get; init; }

	[Id(6)]
	public required uint ClientIpAdress { get; init; }
	[Id(7)]
	public required uint AssigneeAdress { get; init; }//YIAddr
	[Id(8)]
	public required uint ServerIpAdress { get; init; }
	[Id(9)]
	public required uint[] ClientHardwareAdresses { get; init; }
	[Id(10)]
	public required DhcpOption[] Options { get; init; }

	public string? GetMacAddress()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.ClientId);
		return option is null ? null : BitConverter.ToString(option.Data);
	}
}
[GenerateSerializer]
public class DhcpOption
{
	[Id(0)]
	public required EOption Option { get; init; }
	[Id(1)]
	public required byte[] Data { get; init; }
}
public enum EOption : byte
{
	HostName = 12,
	AdressRequest = 50,
	DhcpMessageType = 53,
	ParameterList = 55,
	DhcpMaxMessageSize = 57,
	ClassId = 60,
	ClientId = 61,
	Unassigned = 110,

}
public enum EMessageDirection : byte
{
	Undefined = 0,
	Request = 1,
	Reply = 2,
}
public enum EHardwareType : byte
{
	Ethernet = 1,
	Ieee802 = 6,
	Arcnet = 7,
	LocalTalk = 11,
	LocalNet = 12,
	Smds = 14,
	FrameRelay = 15,
	Atm1 = 16,
	Hdlc = 17,
	FirbreChannel = 18,
	Atm2 = 19,
	SerialLine = 20,
}
public enum EResponseCastType : ushort
{
	Unicast = 0,
	Broadcast = 1,
}

public interface IMessageParserGrain : IGrainWithStringKey
{
	Task<DhcpMessage> Parse(byte[] buffer);
}
//TODO: I AM ON A BREAK!!!! BRB
public class MessageParserGrain : Grain, IMessageParserGrain
{
	public Task<DhcpMessage> Parse(byte[] buffer)
	{
		return Task.FromResult(new DhcpMessage
		{
			Direction = (EMessageDirection)buffer[0],
			HardwareType = (EHardwareType)buffer[1],
			ClientIdLength = buffer[2],
			Hops = buffer[3],
			TransactionId = BitConverter.ToUInt32(buffer, 4),
			ResponseCastType = (EResponseCastType)BitConverter.ToUInt16(buffer, 8),
			ClientIpAdress = BitConverter.ToUInt32(buffer, 10),
			AssigneeAdress = BitConverter.ToUInt32(buffer, 14),
			ServerIpAdress = BitConverter.ToUInt32(buffer, 18),
			ClientHardwareAdresses = [
				BitConverter.ToUInt32(buffer, 22),
				BitConverter.ToUInt32(buffer, 26),
				BitConverter.ToUInt32(buffer, 30),
				BitConverter.ToUInt32(buffer, 34),
			],
			Options = ReadOptions(buffer[240..^2])
		});
	}

	private DhcpOption[] ReadOptions(byte[] optionsBuffer)
	{
		var bufferQueue = new Queue<byte>(optionsBuffer);
		var options = new List<DhcpOption>();

		while (bufferQueue.Count > 0)
		{
			var option = (EOption)bufferQueue.Dequeue();
			var length = bufferQueue.Dequeue();
			var data = Enumerable
				.Range(1, length)
				.Select(r => bufferQueue.Dequeue())
				.ToArray();
			options.Add(new DhcpOption
			{
				Option = option,
				Data = data,
			});
		}

		return [.. options];
	}
}
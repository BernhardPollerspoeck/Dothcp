namespace qt.qsp.dhcp.Server.Models;

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
	public EMessageType GetMessageType()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.DhcpMessageType);
		return option is null ? EMessageType.Unknown : (EMessageType)option.Data[0];
	}
}

public enum EMessageType : byte
{
	Unknown = 0,
	Discover = 1,//client message to gather offers
	Offer = 2,//server message offer of a lease
	Request = 3,//client requests the ip of an offer

	Ack = 4,//server confirms the request
}
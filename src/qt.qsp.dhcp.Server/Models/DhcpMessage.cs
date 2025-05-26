using System.Net;
using qt.qsp.dhcp.Server.Models.Enumerations;

namespace qt.qsp.dhcp.Server.Models;

[GenerateSerializer]
[Alias("DhcpMessage")]
public class DhcpMessage
{
	#region properties
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
	public required byte[] ClientHardwareAdress { get; init; }
	[Id(10)]
	public required DhcpOption[] Options { get; init; }
	#endregion

	#region data conversion helper
	public bool HasOption(EOption option)
	{
		return Options.Any(o => o.Option == option);
	}
	public string GetRequestedAddress()
	{
		var option = Options
			.First(o => o.Option is EOption.AdressRequest);
		return string.Join('.', option.Data);
	}
	public string GetClientId()
	{
		return BitConverter.ToString(ClientHardwareAdress);
	}
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
	public string? GetHostname()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.HostName);
		if (option == null) return null;
		return System.Text.Encoding.ASCII.GetString(option.Data);
	}
	
	public IEnumerable<byte> GetParameterList()
	{
		return Options
			.FirstOrDefault(o => o.Option is EOption.ParameterList)
			?.Data 
			?? [];
	}

	public IEnumerable<byte> ToData()
	{
		yield return (byte)Direction;
		yield return (byte)HardwareType;
		yield return ClientIdLength;
		yield return Hops;
		foreach (var tid in BitConverter.GetBytes(TransactionId))
		{
			yield return tid;
		}
		foreach (var rct in BitConverter.GetBytes((uint)ResponseCastType))
		{
			yield return rct;
		}
		foreach (var cia in BitConverter.GetBytes(ClientIpAdress))
		{
			yield return cia;
		}
		foreach (var aia in BitConverter.GetBytes(AssigneeAdress))
		{
			yield return aia;
		}
		foreach (var sia in BitConverter.GetBytes(ServerIpAdress))
		{
			yield return sia;
		}
		foreach (var gw in IPAddress.Parse("192.168.0.1").GetAddressBytes())//TODO: correct
		{
			yield return gw;
		}
		foreach (var chia in ClientHardwareAdress)
		{
			yield return chia;
		}
		foreach (var bootp in Enumerable.Range(0, 192))
		{
			yield return 0;
		}
		yield return 0x63;
		yield return 0x82;
		yield return 0x53;
		yield return 0x63;

		foreach (var option in Options)
		{
			yield return (byte)option.Option;
			yield return (byte)option.Data.Length;
			foreach (var data in option.Data)
			{
				yield return data;
			}
		}
	}
	#endregion
}

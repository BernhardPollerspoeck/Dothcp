using System.Net;
using System.Text;
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
	
	// Get the requested IP address from options
	public IPAddress? RequestedIpAddress
	{
		get
		{
			if (!HasOption(EOption.AdressRequest))
			{
				return null;
			}
			
			try
			{
				var option = Options.First(o => o.Option is EOption.AdressRequest);
				var ipString = string.Join('.', option.Data);
				return IPAddress.Parse(ipString);
			}
			catch
			{
				return null;
			}
		}
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
	
	public string? GetDomainName()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.DomainName);
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

	public IPAddress[]? GetNtpServers()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.NtpServers);
		if (option == null) return null;

		try
		{
			var ipCount = option.Data.Length / 4;
			var result = new IPAddress[ipCount];

			for (var i = 0; i < ipCount; i++)
			{
				result[i] = new IPAddress(option.Data.Skip(i * 4).Take(4).ToArray());
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	public IPAddress[]? GetNetBiosNameServers()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.NetBiosNameServer);
		if (option == null) return null;

		try
		{
			var ipCount = option.Data.Length / 4;
			var result = new IPAddress[ipCount];

			for (var i = 0; i < ipCount; i++)
			{
				result[i] = new IPAddress(option.Data.Skip(i * 4).Take(4).ToArray());
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	public byte? GetNetBiosNodeType()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.NetBiosNodeType);
		if (option == null || option.Data.Length < 1) return null;
		return option.Data[0];
	}

	public string? GetNetBiosScope()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.NetBiosScope);
		if (option == null) return null;
		return System.Text.Encoding.ASCII.GetString(option.Data);
	}

	public byte[]? GetRelayAgentInfo()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.RelayAgentInfo);
		if (option == null) return null;
		return option.Data;
	}

	public byte[]? GetVendorSpecificInfo()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.VendorSpecificInfo);
		if (option == null) return null;
		return option.Data;
	}

	public Dictionary<(byte prefixLength, byte[] networkPrefix), IPAddress>? GetClasslessStaticRoutes()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.ClasslessStaticRoute);
		if (option == null) return null;

		try
		{
			var routes = new Dictionary<(byte prefixLength, byte[] networkPrefix), IPAddress>();
			var index = 0;

			while (index < option.Data.Length)
			{
				// Get the prefix length
				byte prefixLength = option.Data[index++];
				
				// Calculate the number of bytes needed to represent the network prefix
				int networkPrefixBytes = (int)Math.Ceiling(prefixLength / 8.0);
				
				// Extract the network prefix
				var networkPrefix = new byte[networkPrefixBytes];
				Array.Copy(option.Data, index, networkPrefix, 0, networkPrefixBytes);
				index += networkPrefixBytes;
				
				// Extract the router address (always 4 bytes)
				var routerBytes = new byte[4];
				if (index + 3 < option.Data.Length)
				{
					Array.Copy(option.Data, index, routerBytes, 0, 4);
					index += 4;
					
					var routerAddress = new IPAddress(routerBytes);
					routes.Add((prefixLength, networkPrefix), routerAddress);
				}
				else
				{
					// Malformed option data
					break;
				}
			}
			
			return routes;
		}
		catch
		{
			return null;
		}
	}

	public string[]? GetDnsSearchList()
	{
		var option = Options
			.FirstOrDefault(o => o.Option is EOption.DnsSearchList);
		if (option == null) return null;

		try
		{
			var result = new List<string>();
			int position = 0;
			
			// Parse all domain names in the search list
			while (position < option.Data.Length)
			{
				string domain = ParseDnsName(option.Data, ref position);
				if (!string.IsNullOrEmpty(domain))
				{
					result.Add(domain);
				}
				
				// If we're at a zero byte and not at the end yet, skip it as it may be
				// the start of the next domain name
				if (position < option.Data.Length && option.Data[position] == 0)
				{
					position++;
				}
			}
			
			return result.ToArray();
		}
		catch
		{
			return null;
		}
	}
	
	private string ParseDnsName(byte[] data, ref int position)
	{
		if (position >= data.Length)
		{
			return string.Empty;
		}
		
		var domainParts = new List<string>();
		bool doneReading = false;
		int maxJumps = 128; // Prevent infinite loops due to malformed data
		
		// Keep track of where we were in case we need to jump for compression
		int savedPosition = -1;
		
		while (!doneReading && maxJumps-- > 0)
		{
			if (position >= data.Length)
			{
				break;
			}
			
			byte b = data[position++];
			
			// Check if this is a pointer (compression)
			if ((b & 0xC0) == 0xC0)
			{
				// This is a compression pointer (high 2 bits set)
				if (position >= data.Length)
				{
					break; // Malformed - not enough data for pointer
				}
				
				// Save current position if this is our first jump
				if (savedPosition == -1)
				{
					savedPosition = position + 1; // +1 to skip the second byte of the pointer
				}
				
				// Calculate offset from 14 bits (the 2 MSB bits are the marker)
				int offset = ((b & 0x3F) << 8) | data[position++];
				position = offset; // Jump to the offset
			}
			else if (b == 0)
			{
				// End of domain name
				doneReading = true;
			}
			else
			{
				// Regular label
				int labelLength = b;
				if (position + labelLength > data.Length)
				{
					break; // Malformed - not enough data for label
				}
				
				var labelBytes = new byte[labelLength];
				Array.Copy(data, position, labelBytes, 0, labelLength);
				domainParts.Add(Encoding.ASCII.GetString(labelBytes));
				position += labelLength;
			}
		}
		
		// If we followed compression pointers, restore the position
		if (savedPosition != -1)
		{
			position = savedPosition;
		}
		
		return string.Join(".", domainParts);
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
		foreach (var gw in IPAddress.Parse("0.0.0.0").GetAddressBytes())
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

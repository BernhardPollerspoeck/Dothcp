using System.Net;
using System.Text;
using qt.qsp.dhcp.Server.Models.Enumerations;

namespace qt.qsp.dhcp.Server.Models.OptionBuilder;

public class DhcpOptionsBuilder
{
	#region fields
	private readonly List<DhcpOption> _options = [];
	#endregion

	#region builder methods
	public DhcpOptionsBuilder AddMessageType(EMessageType type)
	{
		_options.Add(new()
		{
			Option = EOption.DhcpMessageType,
			Data = [(byte)type],
		});
		return this;
	}
	public DhcpOptionsBuilder AddServerIdentifier(IPAddress serverIdentifier)
	{
		_options.Add(new()
		{
			Option = EOption.DhcpServerIdentifier,
			Data = serverIdentifier.GetAddressBytes(),
		});
		return this;
	}
	public DhcpOptionsBuilder AddAddressLeaseTime(TimeSpan timeSpan)
	{
		_options.Add(new()
		{
			Option = EOption.AddressTime,
			Data = BitConverter.IsLittleEndian
				? BitConverter.GetBytes((uint)timeSpan.TotalSeconds).Reverse().ToArray()
				: BitConverter.GetBytes((uint)timeSpan.TotalSeconds),
		});
		return this;
	}
	public DhcpOptionsBuilder AddRenewalTime(TimeSpan timeSpan)
	{
		_options.Add(new()
		{
			Option = EOption.RenewalTime,
			Data = BitConverter.IsLittleEndian
				? BitConverter.GetBytes((uint)timeSpan.TotalSeconds).Reverse().ToArray()
				: BitConverter.GetBytes((uint)timeSpan.TotalSeconds),
		});
		return this;
	}
	public DhcpOptionsBuilder AddRebindingTime(TimeSpan timeSpan)
	{
		_options.Add(new()
		{
			Option = EOption.RebindingTime,
			Data = BitConverter.IsLittleEndian
				? BitConverter.GetBytes((uint)timeSpan.TotalSeconds).Reverse().ToArray()
				: BitConverter.GetBytes((uint)timeSpan.TotalSeconds),
		});
		return this;
	}
	public DhcpOptionsBuilder AddSubnetMask(string subnetMask)
	{
		_options.Add(new()
		{
			Option = EOption.SubnetMask,
			Data = IPAddress.Parse(subnetMask).GetAddressBytes(),
		});
		return this;
	}
	public DhcpOptionsBuilder AddBroadcastAddressOption(string broadcastAddress)
	{
		_options.Add(new()
		{
			Option = EOption.BroadcastAddressOption,
			Data = IPAddress.Parse(broadcastAddress).GetAddressBytes(),
		});
		return this;
	}
	public DhcpOptionsBuilder AddRouterOption(string[] routerOptions)
	{
		if (routerOptions.Length < 1)
		{
			throw new InvalidDataException("At least 1 router is required");
		}
		_options.Add(new()
		{
			Option = EOption.RouterOptions,
			Data = routerOptions.Select(o => IPAddress.Parse(o).GetAddressBytes()).SelectMany(o => o).ToArray(),
		});
		return this;
	}
	public DhcpOptionsBuilder AddDnsServerOptions(string[] dnsServers)
	{
		if (dnsServers.Length < 1)
		{
			return this;
		}
		_options.Add(new()
		{
			Option = EOption.DnsServerOptions,
			Data = dnsServers.Select(o => IPAddress.Parse(o).GetAddressBytes()).SelectMany(o => o).ToArray(),
		});
		return this;
	}
	public DhcpOptionsBuilder AddVendorSpecificInfo(byte[] vendorInfo)
	{
		_options.Add(new()
		{
			Option = EOption.VendorSpecificInfo,
			Data = vendorInfo,
		});
		return this;
	}

	public DhcpOptionsBuilder AddNetBiosNameServers(string[] nameServers)
	{
		if (nameServers.Length < 1)
		{
			return this;
		}
		_options.Add(new()
		{
			Option = EOption.NetBiosNameServer,
			Data = nameServers.Select(o => IPAddress.Parse(o).GetAddressBytes()).SelectMany(o => o).ToArray(),
		});
		return this;
	}

	public DhcpOptionsBuilder AddNetBiosNodeType(byte nodeType)
	{
		_options.Add(new()
		{
			Option = EOption.NetBiosNodeType,
			Data = [nodeType],
		});
		return this;
	}

	public DhcpOptionsBuilder AddNetBiosScope(string scope)
	{
		_options.Add(new()
		{
			Option = EOption.NetBiosScope,
			Data = Encoding.ASCII.GetBytes(scope),
		});
		return this;
	}

	public DhcpOptionsBuilder AddNtpServerOptions(string[] ntpServers)
	{
		if (ntpServers.Length < 1)
		{
			return this;
		}
		_options.Add(new()
		{
			Option = EOption.NtpServers,
			Data = ntpServers.Select(o => IPAddress.Parse(o).GetAddressBytes()).SelectMany(o => o).ToArray(),
		});
		return this;
	}
	public DhcpOptionsBuilder AddInterfaceMtuOption(ushort mtu)
	{
		_options.Add(new()
		{
			Option = EOption.MtuInterface,
			Data = BitConverter.IsLittleEndian
				? BitConverter.GetBytes(mtu).Reverse().ToArray()
				: BitConverter.GetBytes(mtu),
		});
		return this;
	}
	public DhcpOptionsBuilder AddTimeOffset(TimeSpan timeOffset)
	{
		_options.Add(new()
		{
			Option = EOption.TimeOffset,
			Data = BitConverter.IsLittleEndian
				? BitConverter.GetBytes((int)timeOffset.TotalSeconds).Reverse().ToArray()
				: BitConverter.GetBytes((int)timeOffset.TotalSeconds),
		});
		return this;
	}
	public DhcpOptionsBuilder AddHostName(string hostName)
	{
		_options.Add(new()
		{
			Option = EOption.HostName,
			Data = Encoding.UTF8.GetBytes(hostName),
		});
		return this;
	}
	public DhcpOptionsBuilder AddDomainName(string domainName)
	{
		_options.Add(new()
		{
			Option = EOption.DomainName,
			Data = Encoding.UTF8.GetBytes(domainName),
		});
		return this;
	}

	public DhcpOptionsBuilder AddRelayAgentInfo(byte[] relayInfo)
	{
		_options.Add(new()
		{
			Option = EOption.RelayAgentInfo,
			Data = relayInfo,
		});
		return this;
	}

	public DhcpOptionsBuilder AddDnsSearchList(string[] searchDomains)
	{
		if (searchDomains.Length < 1)
		{
			return this;
		}

		// Simplified DNS name encoding - each domain is encoded as a series of length-prefixed labels
		var data = new List<byte>();
		
		foreach (var domain in searchDomains)
		{
			var labels = domain.Split('.');
			foreach (var label in labels)
			{
				data.Add((byte)label.Length);
				data.AddRange(Encoding.ASCII.GetBytes(label));
			}
			data.Add(0); // Null terminator for the domain
		}
		
		_options.Add(new()
		{
			Option = EOption.DnsSearchList,
			Data = data.ToArray(),
		});
		return this;
	}

	public DhcpOptionsBuilder AddClasslessStaticRoutes(Dictionary<(byte prefixLength, byte[] networkPrefix), IPAddress> routes)
	{
		if (routes.Count < 1)
		{
			return this;
		}

		var data = new List<byte>();
		
		foreach (var route in routes)
		{
			data.Add(route.Key.prefixLength);  // Prefix length
			data.AddRange(route.Key.networkPrefix);  // Network prefix
			data.AddRange(route.Value.GetAddressBytes());  // Router address
		}
		
		_options.Add(new()
		{
			Option = EOption.ClasslessStaticRoute,
			Data = data.ToArray(),
		});
		return this;
	}
	#endregion

	#region build
	public DhcpOption[] Build()
	{
		_options.Add(new()
		{
			Option = EOption.End,
			Data = [],
		});
		return [.. _options];
	}
	#endregion
}

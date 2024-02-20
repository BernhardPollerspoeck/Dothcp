﻿using qt.qsp.dhcp.Server.Models.Enumerations;

namespace qt.qsp.dhcp.Server.Models;

[GenerateSerializer]
[Alias("DhcpOption")]
public class DhcpOption
{
	[Id(0)]
	public required EOption Option { get; init; }
	[Id(1)]
	public required byte[] Data { get; init; }

	#region object
	public override string ToString()
	{
		return $"{Option}: {BitConverter.ToString(Data)}";
	}
	#endregion
}

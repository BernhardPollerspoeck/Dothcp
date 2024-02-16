namespace qt.qsp.dhcp.Server.Models;

[GenerateSerializer]
public class DhcpOption
{
	[Id(0)]
	public required EOption Option { get; init; }
	[Id(1)]
	public required byte[] Data { get; init; }

	public override string ToString()
	{
		return $"{Option}: {BitConverter.ToString(Data)}";

	}
}

using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Grains;

[Alias("IMessageParserGrain")]
public interface IMessageParserGrain : IGrainWithStringKey
{
	[Alias("Parse")]
	Task<DhcpMessage> Parse(byte[] buffer);
}

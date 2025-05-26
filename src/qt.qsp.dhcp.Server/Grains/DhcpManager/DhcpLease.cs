using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpLease
{


	public IPAddress? Subnet { get; set; }//TODO: how to get this value
	public IPAddress? Router { get; set; }//TODO: how to get this value
	public TimeSpan? LeaseDuration { get; set; }//TODO: default 1 day
	public DateTime? LeaseTime { get; set; }//TODO: from when on do we need to count?
	public IPAddress? DhcpServer { get; set; }//TODO: will be us, but we store in case of different user configuration

	public IList<IPAddress> DnsServers { get; set; } = [];


}

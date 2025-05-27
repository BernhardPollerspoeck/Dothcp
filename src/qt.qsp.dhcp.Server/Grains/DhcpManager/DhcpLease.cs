using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[GenerateSerializer]
public class DhcpLease
{
	// Client identification
	  [Id(0)]
    public string MacAddress { get; set; } = string.Empty;
    [Id(1)]
    public IPAddress IpAddress { get; set; } = IPAddress.None;
    [Id(2)]
    public string? HostName { get; set; }

    // Lease timing
    [Id(3)]
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromDays(1);
    [Id(4)]
    public DateTime LeaseStart { get; set; } = DateTime.UtcNow;
	public DateTime LeaseExpiration => LeaseStart.Add(LeaseDuration);
	
	// Lease status
	  [Id(5)]
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    // Network configuration
    [Id(6)]
    public IPAddress? Subnet { get; set; }
    [Id(7)]
    public IPAddress? Router { get; set; }
    [Id(8)]
    public IPAddress? DhcpServer { get; set; }
    [Id(9)]
    public IList<IPAddress> DnsServers { get; set; } = [];

	// Methods for lease management
	public bool IsExpired() => DateTime.UtcNow > LeaseExpiration;

	public void Renew(TimeSpan? newDuration = null)
	{
		LeaseStart = DateTime.UtcNow;
		if (newDuration.HasValue)
		{
			LeaseDuration = newDuration.Value;
		}
		Status = LeaseStatus.Renewed;
	}

	public void Expire()
	{
		Status = LeaseStatus.Expired;
	}
}

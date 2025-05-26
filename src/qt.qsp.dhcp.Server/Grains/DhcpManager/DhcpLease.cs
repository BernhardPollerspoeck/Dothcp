using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public class DhcpLease
{
	// Client identification
	public string MacAddress { get; set; } = string.Empty;
	public IPAddress IpAddress { get; set; } = IPAddress.None;
	public string? HostName { get; set; }

	// Lease timing
	public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromDays(1);
	public DateTime LeaseStart { get; set; } = DateTime.UtcNow;
	public DateTime LeaseExpiration => LeaseStart.Add(LeaseDuration);
	
	// Lease status
	public LeaseStatus Status { get; set; } = LeaseStatus.Active;

	// Network configuration
	public IPAddress? Subnet { get; set; }
	public IPAddress? Router { get; set; }
	public IPAddress? DhcpServer { get; set; }
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

public enum LeaseStatus
{
	Active,
	Expired,
	Renewed
}

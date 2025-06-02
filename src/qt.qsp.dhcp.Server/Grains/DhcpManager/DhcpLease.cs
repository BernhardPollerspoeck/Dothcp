using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[GenerateSerializer]
public class DhcpLease
{
	// Client identification
	  [Id(0)]
    public string MacAddress { get; set; } = string.Empty;
    [Id(1)]
    public string IpAddressString { get; set; } = string.Empty;
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
    public string? SubnetString { get; set; }
    [Id(7)]
    public string? RouterString { get; set; }
    [Id(8)]
    public string? DhcpServerString { get; set; }
    [Id(9)]
    public IList<string> DnsServerStrings { get; set; } = [];

    // Non-serialized convenience properties
    public IPAddress IpAddress 
    { 
        get => string.IsNullOrEmpty(IpAddressString) ? IPAddress.None : IPAddress.Parse(IpAddressString);
        set => IpAddressString = value.ToString();
    }
    
    public IPAddress? Subnet 
    { 
        get => string.IsNullOrEmpty(SubnetString) ? null : IPAddress.Parse(SubnetString);
        set => SubnetString = value?.ToString();
    }
    
    public IPAddress? Router 
    { 
        get => string.IsNullOrEmpty(RouterString) ? null : IPAddress.Parse(RouterString);
        set => RouterString = value?.ToString();
    }
    
    public IPAddress? DhcpServer 
    { 
        get => string.IsNullOrEmpty(DhcpServerString) ? null : IPAddress.Parse(DhcpServerString);
        set => DhcpServerString = value?.ToString();
    }
    
    public IList<IPAddress> DnsServers 
    { 
        get => DnsServerStrings.Where(s => !string.IsNullOrEmpty(s)).Select(IPAddress.Parse).ToList();
        set => DnsServerStrings = value.Select(ip => ip.ToString()).ToList();
    }

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

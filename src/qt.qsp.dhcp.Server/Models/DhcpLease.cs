using System.Net;
using System.ComponentModel.DataAnnotations;

namespace qt.qsp.dhcp.Server.Models;

public class DhcpLease
{
    [Key]
    public int Id { get; set; }

    // Client identification
    [Required]
    [MaxLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddressString { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? HostName { get; set; }

    // Lease timing
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromDays(1);
    public DateTime LeaseStart { get; set; } = DateTime.UtcNow;
    public DateTime LeaseExpiration => LeaseStart.Add(LeaseDuration);

    // Lease status
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    // Network configuration
    [MaxLength(45)]
    public string? SubnetString { get; set; }

    [MaxLength(45)]
    public string? RouterString { get; set; }

    [MaxLength(45)]
    public string? DhcpServerString { get; set; }

    [MaxLength(1000)]
    public string? DnsServerStringsJson { get; set; }

    // Non-persisted convenience properties
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
        get
        {
            if (string.IsNullOrEmpty(DnsServerStringsJson))
                return new List<IPAddress>();

            var strings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(DnsServerStringsJson) ?? new List<string>();
            return strings.Where(s => !string.IsNullOrEmpty(s)).Select(IPAddress.Parse).ToList();
        }
        set
        {
            var strings = value.Select(ip => ip.ToString()).ToList();
            DnsServerStringsJson = System.Text.Json.JsonSerializer.Serialize(strings);
        }
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

public enum LeaseStatus
{
    Active,
    Renewed,
    Expired,
    Released
}

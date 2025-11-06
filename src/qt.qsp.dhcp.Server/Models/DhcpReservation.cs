using System.Net;
using System.ComponentModel.DataAnnotations;

namespace qt.qsp.dhcp.Server.Models;

public class DhcpReservation
{
    [Key]
    public int Id { get; set; }

    // Reservation identification
    [Required]
    [MaxLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddressString { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsed { get; set; }

    // Network configuration (optional overrides)
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

    // Methods for reservation management
    public bool IsValidForMac(string macAddress)
    {
        return IsActive && string.Equals(MacAddress, macAddress, StringComparison.OrdinalIgnoreCase);
    }

    public void MarkAsUsed()
    {
        LastUsed = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[GenerateSerializer]
public class DhcpReservation
{
    // Reservation identification
    [Id(0)]
    public string MacAddress { get; set; } = string.Empty;
    
    [Id(1)]
    public string IpAddressString { get; set; } = string.Empty;
    
    [Id(2)]
    public string Description { get; set; } = string.Empty;
    
    [Id(3)]
    public bool IsActive { get; set; } = true;
    
    // Metadata
    [Id(4)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Id(5)]
    public DateTime? LastUsed { get; set; }
    
    // Network configuration (optional overrides)
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
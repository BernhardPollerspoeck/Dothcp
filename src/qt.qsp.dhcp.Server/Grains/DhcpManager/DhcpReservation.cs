using System.Net;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

[GenerateSerializer]
public class DhcpReservation
{
    // Reservation identification
    [Id(0)]
    public string MacAddress { get; set; } = string.Empty;
    
    [Id(1)]
    public IPAddress IpAddress { get; set; } = IPAddress.None;
    
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
    public IPAddress? Subnet { get; set; }
    
    [Id(7)]
    public IPAddress? Router { get; set; }
    
    [Id(8)]
    public IPAddress? DhcpServer { get; set; }
    
    [Id(9)]
    public IList<IPAddress> DnsServers { get; set; } = [];

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
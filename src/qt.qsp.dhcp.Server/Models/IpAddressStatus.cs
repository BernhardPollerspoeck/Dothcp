using System.ComponentModel.DataAnnotations;

namespace qt.qsp.dhcp.Server.Models;

public class IpAddressStatus
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(45)]
    public string IpAddressString { get; set; } = string.Empty;

    public EIpAddressStatus Status { get; set; }

    [MaxLength(100)]
    public string? ClientId { get; set; }
}

public enum EIpAddressStatus
{
    Available,
    Offered,
    Claimed,
    Declined
}

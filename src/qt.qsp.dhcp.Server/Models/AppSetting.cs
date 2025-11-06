using System.ComponentModel.DataAnnotations;

namespace qt.qsp.dhcp.Server.Models;

public class AppSetting
{
    [Key]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace qt.qsp.dhcp.Server.Models;

public class EditableSettings
{
	[Display(Name = "Range Low")]
	[Range(1, 254, ErrorMessage = "Range Low must be between 1 and 254")]
	public int RangeLow { get; set; }

	[Display(Name = "Range High")]
	[Range(1, 254, ErrorMessage = "Range High must be between 1 and 254")]
	public int RangeHigh { get; set; }

	[Display(Name = "Lease Time (HH:MM:SS)")]
	[Required(ErrorMessage = "Lease Time is required")]
	public string LeaseTime { get; set; } = string.Empty;

	[Display(Name = "Renewal Time (HH:MM:SS)")]
	[Required(ErrorMessage = "Renewal Time is required")]
	public string RenewalTime { get; set; } = string.Empty;

	[Display(Name = "Rebinding Time (HH:MM:SS)")]
	[Required(ErrorMessage = "Rebinding Time is required")]
	public string RebindingTime { get; set; } = string.Empty;

	[Display(Name = "Subnet Mask")]
	[Required(ErrorMessage = "Subnet Mask is required")]
	public string Subnet { get; set; } = string.Empty;

	[Display(Name = "Router")]
	[Required(ErrorMessage = "Router is required")]
	public string Router { get; set; } = string.Empty;

	[Display(Name = "DNS Servers (semicolon separated)")]
	public string Dns { get; set; } = string.Empty;

	public bool IsModified { get; set; }
}
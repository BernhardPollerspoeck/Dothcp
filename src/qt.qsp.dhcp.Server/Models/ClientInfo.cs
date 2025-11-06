namespace qt.qsp.dhcp.Server.Models;

public class ClientInfo
{
	public string ClientId { get; set; } = string.Empty;
	public string? AssignedIpAddress { get; set; }
	public string State { get; set; } = EClientState.Unknown.ToString();
	public DateTime LastSeen { get; set; }
	public string? HostName { get; set; }
	public string? DomainName { get; set; }
}

public enum EClientState
{
	Unknown,
	Offered,
	Assigned,
	Declined,
	Released,
}
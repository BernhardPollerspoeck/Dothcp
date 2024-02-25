namespace qt.qsp.dhcp.Server.Models;

public class ClientInfo
{
	public bool HasAssignedAddress { get; set; }
	public string? Address { get; set; }
	public EClientState State { get; set; }
}

public enum EClientState
{
	Unknown,
	Offered,
	Assigned,
}
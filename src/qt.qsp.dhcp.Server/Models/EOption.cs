namespace qt.qsp.dhcp.Server.Models;

/// <summary>
/// https://www.iana.org/assignments/bootp-dhcp-parameters/bootp-dhcp-parameters.xhtml
/// https://www.rfc-editor.org/rfc/rfc2132.html
/// </summary>
public enum EOption : byte
{
	/// <summary>
	/// 3.3. Subnet Mask
	/// </summary>
	SubnetMask = 1,

	/// <summary>
	/// 3.4. Time Offset
	/// </summary>
	TimeOffset = 2,

	/// <summary>
	/// 3.5. Router Option
	/// </summary>
	RouterOptions = 3,

	/// <summary>
	/// 3.8. Domain Name Server Option
	/// </summary>
	DnsServerOptions = 6,

	HostName = 12,

	/// <summary>
	/// 5.1. Interface MTU Option
	/// </summary>
	MtuInterface = 26,

	/// <summary>
	/// 5.3. Broadcast Address Option
	/// </summary>
	BroadcastAddressOption = 28,

	AdressRequest = 50,

	/// <summary>
	/// The time how long the lease it valid
	/// Length = 4,
	/// </summary>
	AddressTime = 51,

	/// <summary>
	/// Defines the type of the message (discover offer request ack)
	/// Length = 1
	/// </summary>
	DhcpMessageType = 53,

	/// <summary>
	/// Length = 4
	/// </summary>
	DhcpServerIdentifier = 54,

	ParameterList = 55,


	DhcpMaxMessageSize = 57,

	/// <summary>
	/// 9.11. Renewal (T1) Time Value
	/// </summary>
	RenewalTime = 58,

	/// <summary>
	/// 9.12. Rebinding (T2) Time Value
	/// </summary>
	RebindingTime = 59,

	ClassId = 60,
	ClientId = 61,
	Unassigned = 110,


	End = 255
}

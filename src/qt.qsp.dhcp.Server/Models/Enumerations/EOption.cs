namespace qt.qsp.dhcp.Server.Models.Enumerations;

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

	/// <summary>
	/// 3.14. Host Name Option
	/// </summary>
	HostName = 12,

	/// <summary>
	/// 3.17. Domain Name
	/// </summary>
	DomainName = 15,


	/// <summary>
	/// 5.1. Interface MTU Option
	/// </summary>
	MtuInterface = 26,

	/// <summary>
	/// 5.3. Broadcast Address Option
	/// </summary>
	BroadcastAddressOption = 28,

	/// <summary>
	/// 8.3. Network Time Protocol Servers Option
	/// </summary>
	NtpServers = 42,

	/// <summary>
	/// 8.4. Vendor Specific Information
	/// </summary>
	VendorSpecificInfo = 43,

	/// <summary>
	/// 8.5. NetBIOS over TCP/IP Name Server Option
	/// </summary>
	NetBiosNameServer = 44,

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

	/// <summary>
	/// 8.7. NetBIOS over TCP/IP Node Type Option
	/// </summary>
	NetBiosNodeType = 46,

	/// <summary>
	/// 8.8. NetBIOS over TCP/IP Scope Option
	/// </summary>
	NetBiosScope = 47,

	/// <summary>
	/// 9.13. Relay Agent Information
	/// RFC 3046
	/// </summary>
	RelayAgentInfo = 82,

	/// <summary>
	/// DNS Search List
	/// RFC 3397
	/// </summary>
	DnsSearchList = 119,

	/// <summary>
	/// Classless Static Route
	/// RFC 3442
	/// </summary>
	ClasslessStaticRoute = 121,

	Unassigned = 110,


	End = 255
}

using Orleans.Runtime;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public interface IOfferGeneratorService
{
	Task<(bool, DhcpMessage?)> TryCreateOfferFromReservation(
		DhcpMessage message,
		IPersistentState<ClientInfo> clientInfo,
		string clientId);
		
	Task<(bool, DhcpMessage?)> TryCreateOfferFromPreviousIp(
		DhcpMessage message,
		IPersistentState<ClientInfo> clientInfo,
		string clientId);
		
	Task<(bool, DhcpMessage?)> TryCreateOfferFromRequestedIp(
		DhcpMessage message,
		IPersistentState<ClientInfo> clientInfo,
		string clientId);
		
	Task<(bool, DhcpMessage?)> TryCreateOfferFromRandomIp(
		DhcpMessage message,
		IPersistentState<ClientInfo> clientInfo,
		string clientId);
}

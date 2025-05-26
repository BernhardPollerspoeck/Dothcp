using Microsoft.AspNetCore.Components.Routing;
using Orleans;
using qt.qsp.dhcp.Server.Models;
using System.Net.Mail;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

/// <summary>
/// Identified by client address
/// </summary>
[Alias("IDhcpManagerGrain")]
public interface IDhcpManagerGrain : IGrainWithStringKey
{
	[Alias("HandleMessage")]
	Task<DhcpMessage?> HandleMessage(DhcpMessage message);
}

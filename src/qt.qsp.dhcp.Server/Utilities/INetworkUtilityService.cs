using System;

namespace qt.qsp.dhcp.Server.Utilities;

public interface INetworkUtilityService
{
    /// <summary>
    /// Calculates the broadcast address for a network by applying the subnet mask to the IP address range
    /// </summary>
    /// <param name="ipRange">An IP address within the network range (can be any address in the subnet)</param>
    /// <param name="subnetMask">The subnet mask for the network</param>
    /// <returns>The broadcast address for the network</returns>
    string CalculateBroadcastAddress(string ipRange, string subnetMask);
    
    /// <summary>
    /// Calculates the network address for a network by applying the subnet mask to the IP address range
    /// </summary>
    /// <param name="ipRange">An IP address within the network range (can be any address in the subnet)</param>
    /// <param name="subnetMask">The subnet mask for the network</param>
    /// <returns>The network address for the subnet</returns>
    string CalculateNetworkAddress(string ipRange, string subnetMask);
}
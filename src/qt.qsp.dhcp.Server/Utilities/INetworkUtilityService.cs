using System;
using System.Net;

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
    
    /// <summary>
    /// Checks if the provided IP address is within the specified network range
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="networkAddress">The network address of the subnet</param>
    /// <param name="subnetMask">The subnet mask</param>
    /// <returns>True if the IP is within range, false otherwise</returns>
    bool IsIpInRange(string ipAddress, string networkAddress, string subnetMask);
    
    /// <summary>
    /// Checks if the provided IP address is a reserved address (network or broadcast address)
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="networkAddress">The network address of the subnet</param>
    /// <param name="broadcastAddress">The broadcast address of the subnet</param>
    /// <returns>True if the IP is reserved, false otherwise</returns>
    bool IsReservedIp(string ipAddress, string networkAddress, string broadcastAddress);
    
    /// <summary>
    /// Returns the first usable IP address in the network range (network address + 1)
    /// </summary>
    /// <param name="networkAddress">The network address of the subnet</param>
    /// <returns>The first usable IP address in the subnet</returns>
    string GetFirstUsableIp(string networkAddress);
    
    /// <summary>
    /// Returns the last usable IP address in the network range (broadcast address - 1)
    /// </summary>
    /// <param name="broadcastAddress">The broadcast address of the subnet</param>
    /// <returns>The last usable IP address in the subnet</returns>
    string GetLastUsableIp(string broadcastAddress);
    
    /// <summary>
    /// Checks if the specified IP address is in use on the network using ARP
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="timeout">Timeout for the ARP probe in milliseconds</param>
    /// <returns>True if the IP is in use, false otherwise</returns>
    Task<bool> IsIpInUseAsync(string ipAddress, int timeout = 200);
    
    /// <summary>
    /// Gets available network interfaces with their IP addresses
    /// </summary>
    /// <returns>Dictionary with interface name as key and IP address as value</returns>
    Dictionary<string, string> GetAvailableNetworkInterfaces();
}
using System.Net;
using System.Net.NetworkInformation;

namespace qt.qsp.dhcp.Server.Utilities;

public class NetworkUtilityService : INetworkUtilityService
{
    /// <summary>
    /// Calculates the broadcast address for a network by applying the subnet mask to the IP address range
    /// </summary>
    /// <param name="ipRange">An IP address within the network range (can be any address in the subnet)</param>
    /// <param name="subnetMask">The subnet mask for the network</param>
    /// <returns>The broadcast address for the network</returns>
    public string CalculateBroadcastAddress(string ipRange, string subnetMask)
    {
        if (string.IsNullOrEmpty(ipRange))
            throw new ArgumentNullException(nameof(ipRange), "IP range cannot be null or empty");
            
        if (string.IsNullOrEmpty(subnetMask))
            throw new ArgumentNullException(nameof(subnetMask), "Subnet mask cannot be null or empty");

        // Parse the IP address and subnet mask
        IPAddress ipAddress = IPAddress.Parse(ipRange);
        IPAddress subnet = IPAddress.Parse(subnetMask);
        
        // Get the byte arrays for the IP address and subnet mask
        var ipBytes = ipAddress.GetAddressBytes();
        var subnetBytes = subnet.GetAddressBytes();
        
        if (ipBytes.Length != 4 || subnetBytes.Length != 4)
            throw new ArgumentException("Only IPv4 addresses are supported");
        
        // Calculate the broadcast address by applying the bitwise OR between the IP and inverted subnet mask
        var broadcastBytes = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            broadcastBytes[i] = (byte)(ipBytes[i] | ~subnetBytes[i]);
        }
        
        // Convert the broadcast address to an IPAddress and return it as a string
        return new IPAddress(broadcastBytes).ToString();
    }

    /// <summary>
    /// Calculates the network address for a network by applying the subnet mask to the IP address range
    /// </summary>
    /// <param name="ipRange">An IP address within the network range (can be any address in the subnet)</param>
    /// <param name="subnetMask">The subnet mask for the network</param>
    /// <returns>The network address for the subnet</returns>
    public string CalculateNetworkAddress(string ipRange, string subnetMask)
    {
        if (string.IsNullOrEmpty(ipRange))
            throw new ArgumentNullException(nameof(ipRange), "IP range cannot be null or empty");
            
        if (string.IsNullOrEmpty(subnetMask))
            throw new ArgumentNullException(nameof(subnetMask), "Subnet mask cannot be null or empty");

        // Parse the IP address and subnet mask
        IPAddress ipAddress = IPAddress.Parse(ipRange);
        IPAddress subnet = IPAddress.Parse(subnetMask);
        
        // Get the byte arrays for the IP address and subnet mask
        var ipBytes = ipAddress.GetAddressBytes();
        var subnetBytes = subnet.GetAddressBytes();
        
        if (ipBytes.Length != 4 || subnetBytes.Length != 4)
            throw new ArgumentException("Only IPv4 addresses are supported");
        
        // Calculate the network address by applying the bitwise AND between the IP and subnet mask
        var networkBytes = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & subnetBytes[i]);
        }
        
        // Convert the network address to an IPAddress and return it as a string
        return new IPAddress(networkBytes).ToString();
    }
    
    /// <summary>
    /// Checks if the provided IP address is within the specified network range
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="networkAddress">The network address of the subnet</param>
    /// <param name="subnetMask">The subnet mask</param>
    /// <returns>True if the IP is within range, false otherwise</returns>
    public bool IsIpInRange(string ipAddress, string networkAddress, string subnetMask)
    {
        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(networkAddress) || string.IsNullOrEmpty(subnetMask))
            return false;

        try
        {
            // Calculate the network address of the IP address
            var ipNetworkAddress = CalculateNetworkAddress(ipAddress, subnetMask);
            
            // If the network address of the IP matches the provided network address, then the IP is in range
            return ipNetworkAddress == networkAddress;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Checks if the provided IP address is a reserved address (network or broadcast address)
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="networkAddress">The network address of the subnet</param>
    /// <param name="broadcastAddress">The broadcast address of the subnet</param>
    /// <returns>True if the IP is reserved, false otherwise</returns>
    public bool IsReservedIp(string ipAddress, string networkAddress, string broadcastAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(networkAddress) || string.IsNullOrEmpty(broadcastAddress))
            return false;

        try
        {
            return ipAddress == networkAddress || ipAddress == broadcastAddress;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Returns the first usable IP address in the network range (network address + 1)
    /// </summary>
    /// <param name="networkAddress">The network address of the subnet</param>
    /// <returns>The first usable IP address in the subnet</returns>
    public string GetFirstUsableIp(string networkAddress)
    {
        if (string.IsNullOrEmpty(networkAddress))
            throw new ArgumentNullException(nameof(networkAddress), "Network address cannot be null or empty");
            
        // Parse the network address
        IPAddress network = IPAddress.Parse(networkAddress);
        var ipBytes = network.GetAddressBytes();
        
        if (ipBytes.Length != 4)
            throw new ArgumentException("Only IPv4 addresses are supported");
        
        // Increment the last octet to get the first usable IP
        ipBytes[3] += 1;
        
        return new IPAddress(ipBytes).ToString();
    }
    
    /// <summary>
    /// Returns the last usable IP address in the network range (broadcast address - 1)
    /// </summary>
    /// <param name="broadcastAddress">The broadcast address of the subnet</param>
    /// <returns>The last usable IP address in the subnet</returns>
    public string GetLastUsableIp(string broadcastAddress)
    {
        if (string.IsNullOrEmpty(broadcastAddress))
            throw new ArgumentNullException(nameof(broadcastAddress), "Broadcast address cannot be null or empty");
            
        // Parse the broadcast address
        IPAddress broadcast = IPAddress.Parse(broadcastAddress);
        var ipBytes = broadcast.GetAddressBytes();
        
        if (ipBytes.Length != 4)
            throw new ArgumentException("Only IPv4 addresses are supported");
        
        // Decrement the last octet to get the last usable IP
        ipBytes[3] -= 1;
        
        return new IPAddress(ipBytes).ToString();
    }
    
    /// <summary>
    /// Checks if the specified IP address is in use on the network using ARP
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="timeout">Timeout for the ARP probe in milliseconds</param>
    /// <returns>True if the IP is in use, false otherwise</returns>
    public async Task<bool> IsIpInUseAsync(string ipAddress, int timeout = 200)
    {
        if (string.IsNullOrEmpty(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress), "IP address cannot be null or empty");
        
        try
        {
            // Use ping to check if the IP is in use
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(ipAddress, timeout);
            
            // If we get a response, the IP is in use
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            // If there's an error, assume the IP is not in use
            return false;
        }
    }
}
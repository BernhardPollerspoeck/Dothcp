using System.Net;

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
        byte[] ipBytes = ipAddress.GetAddressBytes();
        byte[] subnetBytes = subnet.GetAddressBytes();
        
        if (ipBytes.Length != 4 || subnetBytes.Length != 4)
            throw new ArgumentException("Only IPv4 addresses are supported");
        
        // Calculate the broadcast address by applying the bitwise OR between the IP and inverted subnet mask
        byte[] broadcastBytes = new byte[4];
        for (int i = 0; i < 4; i++)
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
        byte[] ipBytes = ipAddress.GetAddressBytes();
        byte[] subnetBytes = subnet.GetAddressBytes();
        
        if (ipBytes.Length != 4 || subnetBytes.Length != 4)
            throw new ArgumentException("Only IPv4 addresses are supported");
        
        // Calculate the network address by applying the bitwise AND between the IP and subnet mask
        byte[] networkBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & subnetBytes[i]);
        }
        
        // Convert the network address to an IPAddress and return it as a string
        return new IPAddress(networkBytes).ToString();
    }
}
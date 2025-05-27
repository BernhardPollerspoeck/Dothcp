using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace qt.qsp.dhcp.Server.Grains.DhcpManager;

public static class DhcpLeaseExtensions
{
    // Convert byte array MAC address to string format
    public static string ToMacAddressString(this byte[] macBytes)
    {
        var sb = new StringBuilder(macBytes.Length * 3 - 1);
        for (int i = 0; i < macBytes.Length; i++)
        {
            sb.Append(macBytes[i].ToString("X2"));
            if (i < macBytes.Length - 1)
            {
                sb.Append(':');
            }
        }
        return sb.ToString();
    }
    
    // Convert string MAC address to byte array
    public static byte[] ToMacAddressBytes(this string macAddress)
    {
        var parts = macAddress.Split(':');
        var bytes = new byte[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            bytes[i] = Convert.ToByte(parts[i], 16);
        }
        return bytes;
    }
    
    // Convert uint IP address to string format
    public static string ToIpAddressString(this uint ipAddress)
    {
        var bytes = BitConverter.GetBytes(ipAddress);
        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
    }
    
    // Convert PhysicalAddress to string format
    public static string ToMacAddressString(this PhysicalAddress physicalAddress)
    {
        return BitConverter.ToString(physicalAddress.GetAddressBytes()).Replace("-", ":");
    }
    
    // Create a grain key from MAC address
    public static string ToGrainKey(this string macAddress)
    {
        return macAddress.Replace(":", "").ToLower();
    }
}
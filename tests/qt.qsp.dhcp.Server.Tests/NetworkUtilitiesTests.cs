using qt.qsp.dhcp.Server.Utilities;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class NetworkUtilitiesTests
{
    private readonly INetworkUtilityService _networkUtilityService;
    
    public NetworkUtilitiesTests()
    {
        _networkUtilityService = new NetworkUtilityService();
    }
    
    [Theory]
    [InlineData("192.168.1.1", "255.255.255.0", "192.168.1.255")]
    [InlineData("10.0.0.1", "255.0.0.0", "10.255.255.255")]
    [InlineData("172.16.1.1", "255.255.0.0", "172.16.255.255")]
    [InlineData("192.168.1.15", "255.255.255.240", "192.168.1.15")]
    public void CalculateBroadcastAddress_ShouldReturnCorrectAddress(string ipAddress, string subnetMask, string expectedBroadcast)
    {
        // Act
        var result = _networkUtilityService.CalculateBroadcastAddress(ipAddress, subnetMask);
        
        // Assert
        Assert.Equal(expectedBroadcast, result);
    }

    [Theory]
    [InlineData("192.168.1.15", "255.255.255.0", "192.168.1.0")]
    [InlineData("10.10.10.10", "255.0.0.0", "10.0.0.0")]
    [InlineData("172.16.5.100", "255.255.0.0", "172.16.0.0")]
    [InlineData("192.168.1.15", "255.255.255.240", "192.168.1.0")]
    public void CalculateNetworkAddress_ShouldReturnCorrectAddress(string ipAddress, string subnetMask, string expectedNetwork)
    {
        // Act
        var result = _networkUtilityService.CalculateNetworkAddress(ipAddress, subnetMask);
        
        // Assert
        Assert.Equal(expectedNetwork, result);
    }
    
    [Theory]
    [InlineData("192.168.1.10", "192.168.1.0", "255.255.255.0", true)]
    [InlineData("192.168.2.10", "192.168.1.0", "255.255.255.0", false)]
    [InlineData("10.0.0.5", "10.0.0.0", "255.0.0.0", true)]
    [InlineData("11.0.0.5", "10.0.0.0", "255.0.0.0", false)]
    public void IsIpInRange_ShouldReturnCorrectResult(string ipAddress, string networkAddress, string subnetMask, bool expectedResult)
    {
        // Act
        var result = _networkUtilityService.IsIpInRange(ipAddress, networkAddress, subnetMask);
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData("192.168.1.0", "192.168.1.0", "192.168.1.255", true)]  // Network address
    [InlineData("192.168.1.255", "192.168.1.0", "192.168.1.255", true)]  // Broadcast address
    [InlineData("192.168.1.10", "192.168.1.0", "192.168.1.255", false)]  // Regular IP
    [InlineData("10.0.0.0", "10.0.0.0", "10.255.255.255", true)]  // Network address
    [InlineData("10.255.255.255", "10.0.0.0", "10.255.255.255", true)]  // Broadcast address
    public void IsReservedIp_ShouldReturnCorrectResult(string ipAddress, string networkAddress, string broadcastAddress, bool expectedResult)
    {
        // Act
        var result = _networkUtilityService.IsReservedIp(ipAddress, networkAddress, broadcastAddress);
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData("192.168.1.0", "192.168.1.1")]
    [InlineData("10.0.0.0", "10.0.0.1")]
    [InlineData("172.16.0.0", "172.16.0.1")]
    public void GetFirstUsableIp_ShouldReturnCorrectAddress(string networkAddress, string expectedFirstIp)
    {
        // Act
        var result = _networkUtilityService.GetFirstUsableIp(networkAddress);
        
        // Assert
        Assert.Equal(expectedFirstIp, result);
    }
    
    [Theory]
    [InlineData("192.168.1.255", "192.168.1.254")]
    [InlineData("10.255.255.255", "10.255.255.254")]
    [InlineData("172.16.255.255", "172.16.255.254")]
    public void GetLastUsableIp_ShouldReturnCorrectAddress(string broadcastAddress, string expectedLastIp)
    {
        // Act
        var result = _networkUtilityService.GetLastUsableIp(broadcastAddress);
        
        // Assert
        Assert.Equal(expectedLastIp, result);
    }
}
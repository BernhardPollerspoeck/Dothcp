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
}
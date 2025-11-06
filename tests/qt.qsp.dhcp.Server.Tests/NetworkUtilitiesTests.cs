using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Utilities;

namespace qt.qsp.dhcp.Server.Tests;

[TestClass]
public class NetworkUtilitiesTests
{
    private NetworkUtilityService _networkUtilityService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _networkUtilityService = new NetworkUtilityService();
    }

    [DataTestMethod]
    [DataRow("192.168.1.1", "255.255.255.0", "192.168.1.255")]
    [DataRow("10.0.0.1", "255.0.0.0", "10.255.255.255")]
    [DataRow("172.16.1.1", "255.255.0.0", "172.16.255.255")]
    [DataRow("192.168.1.15", "255.255.255.240", "192.168.1.15")]
    public void CalculateBroadcastAddress_ShouldReturnCorrectAddress(string ipAddress, string subnetMask, string expectedBroadcast)
    {
        // Act
        var result = _networkUtilityService.CalculateBroadcastAddress(ipAddress, subnetMask);

        // Assert
        Assert.AreEqual(expectedBroadcast, result);
    }

    [DataTestMethod]
    [DataRow("192.168.1.15", "255.255.255.0", "192.168.1.0")]
    [DataRow("10.10.10.10", "255.0.0.0", "10.0.0.0")]
    [DataRow("172.16.5.100", "255.255.0.0", "172.16.0.0")]
    [DataRow("192.168.1.15", "255.255.255.240", "192.168.1.0")]
    public void CalculateNetworkAddress_ShouldReturnCorrectAddress(string ipAddress, string subnetMask, string expectedNetwork)
    {
        // Act
        var result = _networkUtilityService.CalculateNetworkAddress(ipAddress, subnetMask);

        // Assert
        Assert.AreEqual(expectedNetwork, result);
    }

    [DataTestMethod]
    [DataRow("192.168.1.10", "192.168.1.0", "255.255.255.0", true)]
    [DataRow("192.168.2.10", "192.168.1.0", "255.255.255.0", false)]
    [DataRow("10.0.0.5", "10.0.0.0", "255.0.0.0", true)]
    [DataRow("11.0.0.5", "10.0.0.0", "255.0.0.0", false)]
    public void IsIpInRange_ShouldReturnCorrectResult(string ipAddress, string networkAddress, string subnetMask, bool expectedResult)
    {
        // Act
        var result = _networkUtilityService.IsIpInRange(ipAddress, networkAddress, subnetMask);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [DataTestMethod]
    [DataRow("192.168.1.0", "192.168.1.0", "192.168.1.255", true)]  // Network address
    [DataRow("192.168.1.255", "192.168.1.0", "192.168.1.255", true)]  // Broadcast address
    [DataRow("192.168.1.10", "192.168.1.0", "192.168.1.255", false)]  // Regular IP
    [DataRow("10.0.0.0", "10.0.0.0", "10.255.255.255", true)]  // Network address
    [DataRow("10.255.255.255", "10.0.0.0", "10.255.255.255", true)]  // Broadcast address
    public void IsReservedIp_ShouldReturnCorrectResult(string ipAddress, string networkAddress, string broadcastAddress, bool expectedResult)
    {
        // Act
        var result = _networkUtilityService.IsReservedIp(ipAddress, networkAddress, broadcastAddress);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [DataTestMethod]
    [DataRow("192.168.1.0", "192.168.1.1")]
    [DataRow("10.0.0.0", "10.0.0.1")]
    [DataRow("172.16.0.0", "172.16.0.1")]
    public void GetFirstUsableIp_ShouldReturnCorrectAddress(string networkAddress, string expectedFirstIp)
    {
        // Act
        var result = _networkUtilityService.GetFirstUsableIp(networkAddress);

        // Assert
        Assert.AreEqual(expectedFirstIp, result);
    }

    [DataTestMethod]
    [DataRow("192.168.1.255", "192.168.1.254")]
    [DataRow("10.255.255.255", "10.255.255.254")]
    [DataRow("172.16.255.255", "172.16.255.254")]
    public void GetLastUsableIp_ShouldReturnCorrectAddress(string broadcastAddress, string expectedLastIp)
    {
        // Act
        var result = _networkUtilityService.GetLastUsableIp(broadcastAddress);

        // Assert
        Assert.AreEqual(expectedLastIp, result);
    }
}

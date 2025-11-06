using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Tests;

[TestClass]
public class DhcpLeaseTests
{
    [TestMethod]
    public void NewLease_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var lease = new DhcpLease();

        // Assert
        Assert.AreEqual(string.Empty, lease.MacAddress);
        Assert.AreEqual(LeaseStatus.Active, lease.Status);
        Assert.AreEqual(TimeSpan.FromDays(1), lease.LeaseDuration);
        Assert.IsTrue(DateTime.UtcNow >= lease.LeaseStart);
        Assert.IsTrue(DateTime.UtcNow <= lease.LeaseStart.AddSeconds(1));
    }

    [TestMethod]
    public void IsExpired_ReturnsFalse_ForNewLease()
    {
        // Arrange
        var lease = new DhcpLease
        {
            LeaseStart = DateTime.UtcNow,
            LeaseDuration = TimeSpan.FromHours(1)
        };

        // Act
        var isExpired = lease.IsExpired();

        // Assert
        Assert.IsFalse(isExpired);
    }

    [TestMethod]
    public void IsExpired_ReturnsTrue_ForExpiredLease()
    {
        // Arrange
        var lease = new DhcpLease
        {
            LeaseStart = DateTime.UtcNow.AddHours(-2),
            LeaseDuration = TimeSpan.FromHours(1)
        };

        // Act
        var isExpired = lease.IsExpired();

        // Assert
        Assert.IsTrue(isExpired);
    }

    [TestMethod]
    public void LeaseExpiration_CalculatesCorrectly()
    {
        // Arrange
        var leaseStart = DateTime.UtcNow;
        var leaseDuration = TimeSpan.FromHours(2);
        var lease = new DhcpLease
        {
            LeaseStart = leaseStart,
            LeaseDuration = leaseDuration
        };

        // Act
        var expiration = lease.LeaseExpiration;

        // Assert
        Assert.AreEqual(leaseStart + leaseDuration, expiration);
    }

    [TestMethod]
    public void IpAddress_ParsesCorrectly()
    {
        // Arrange
        var ipString = "192.168.1.100";
        var lease = new DhcpLease
        {
            IpAddressString = ipString
        };

        // Act
        var ipAddress = lease.IpAddress;

        // Assert
        Assert.AreEqual(IPAddress.Parse(ipString), ipAddress);
    }

    [TestMethod]
    public void DnsServers_ParsesFromJson()
    {
        // Arrange
        var dnsServers = new List<string> { "8.8.8.8", "8.8.4.4" };
        var lease = new DhcpLease
        {
            DnsServerStringsJson = System.Text.Json.JsonSerializer.Serialize(dnsServers)
        };

        // Act
        var parsedServers = lease.DnsServers;

        // Assert
        Assert.IsNotNull(parsedServers);
        Assert.AreEqual(2, parsedServers.Count);
        Assert.AreEqual(IPAddress.Parse("8.8.8.8"), parsedServers[0]);
        Assert.AreEqual(IPAddress.Parse("8.8.4.4"), parsedServers[1]);
    }
}

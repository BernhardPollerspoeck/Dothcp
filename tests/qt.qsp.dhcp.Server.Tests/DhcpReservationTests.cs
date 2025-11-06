using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Models;
using System.Net;

namespace qt.qsp.dhcp.Server.Tests;

[TestClass]
public class DhcpReservationTests
{
    [TestMethod]
    public void DhcpReservation_CreatedWithDefaults_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var reservation = new DhcpReservation();

        // Assert
        Assert.AreEqual(string.Empty, reservation.MacAddress);
        Assert.AreEqual(string.Empty, reservation.Description);
        Assert.IsTrue(reservation.IsActive);
        Assert.IsTrue(reservation.CreatedAt <= DateTime.UtcNow);
        Assert.IsNull(reservation.LastUsed);
    }

    [TestMethod]
    public void DhcpReservation_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var ipString = "192.168.1.100";
        var macAddress = "00:11:22:33:44:55";
        var description = "Test Server";
        var now = DateTime.UtcNow;

        // Act
        var reservation = new DhcpReservation
        {
            IpAddressString = ipString,
            MacAddress = macAddress,
            Description = description,
            IsActive = false,
            CreatedAt = now,
            LastUsed = now
        };

        // Assert
        Assert.AreEqual(IPAddress.Parse(ipString), reservation.IpAddress);
        Assert.AreEqual(macAddress, reservation.MacAddress);
        Assert.AreEqual(description, reservation.Description);
        Assert.IsFalse(reservation.IsActive);
        Assert.AreEqual(now, reservation.CreatedAt);
        Assert.AreEqual(now, reservation.LastUsed);
    }

    [DataTestMethod]
    [DataRow("00:11:22:33:44:55", "00:11:22:33:44:55", true)]
    [DataRow("00:11:22:33:44:55", "00:11:22:33:44:56", false)]
    [DataRow("AA:BB:CC:DD:EE:FF", "aa:bb:cc:dd:ee:ff", true)] // Case insensitive
    public void IsValidForMac_ActiveReservation_ShouldReturnExpectedResult(string reservationMac, string testMac, bool expected)
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            MacAddress = reservationMac,
            IsActive = true
        };

        // Act
        var result = reservation.IsValidForMac(testMac);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void IsValidForMac_InactiveReservation_ShouldReturnFalse()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            MacAddress = "00:11:22:33:44:55",
            IsActive = false
        };

        // Act
        var result = reservation.IsValidForMac("00:11:22:33:44:55");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void MarkAsUsed_ShouldSetLastUsedToCurrentTime()
    {
        // Arrange
        var reservation = new DhcpReservation();
        var beforeCall = DateTime.UtcNow;

        // Act
        reservation.MarkAsUsed();

        // Assert
        var afterCall = DateTime.UtcNow;
        Assert.IsNotNull(reservation.LastUsed);
        Assert.IsTrue(reservation.LastUsed >= beforeCall);
        Assert.IsTrue(reservation.LastUsed <= afterCall);
    }

    [TestMethod]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var reservation = new DhcpReservation { IsActive = false };

        // Act
        reservation.Activate();

        // Assert
        Assert.IsTrue(reservation.IsActive);
    }

    [TestMethod]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var reservation = new DhcpReservation { IsActive = true };

        // Act
        reservation.Deactivate();

        // Assert
        Assert.IsFalse(reservation.IsActive);
    }
}

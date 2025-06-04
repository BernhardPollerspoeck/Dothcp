using System.Net;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class DhcpReservationTests
{
    [Fact]
    public void DhcpReservation_CreatedWithDefaults_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var reservation = new DhcpReservation();

        // Assert
        Assert.Equal(IPAddress.None, reservation.IpAddress);
        Assert.Equal(string.Empty, reservation.MacAddress);
        Assert.Equal(string.Empty, reservation.Description);
        Assert.True(reservation.IsActive);
        Assert.True(reservation.CreatedAt <= DateTime.UtcNow);
        Assert.Null(reservation.LastUsed);
        Assert.Empty(reservation.DnsServers);
    }

    [Fact]
    public void DhcpReservation_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var ipAddress = IPAddress.Parse("192.168.1.100");
        var macAddress = "00:11:22:33:44:55";
        var description = "Test Server";
        var now = DateTime.UtcNow;

        // Act
        var reservation = new DhcpReservation
        {
            IpAddress = ipAddress,
            MacAddress = macAddress,
            Description = description,
            IsActive = false,
            CreatedAt = now,
            LastUsed = now
        };

        // Assert
        Assert.Equal(ipAddress, reservation.IpAddress);
        Assert.Equal(macAddress, reservation.MacAddress);
        Assert.Equal(description, reservation.Description);
        Assert.False(reservation.IsActive);
        Assert.Equal(now, reservation.CreatedAt);
        Assert.Equal(now, reservation.LastUsed);
    }

    [Theory]
    [InlineData("00:11:22:33:44:55", "00:11:22:33:44:55", true)]
    [InlineData("00:11:22:33:44:55", "00:11:22:33:44:56", false)]
    [InlineData("AA:BB:CC:DD:EE:FF", "aa:bb:cc:dd:ee:ff", true)] // Case insensitive
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
        Assert.Equal(expected, result);
    }

    [Fact]
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
        Assert.False(result);
    }

    [Fact]
    public void MarkAsUsed_ShouldSetLastUsedToCurrentTime()
    {
        // Arrange
        var reservation = new DhcpReservation();
        var beforeCall = DateTime.UtcNow;

        // Act
        reservation.MarkAsUsed();

        // Assert
        var afterCall = DateTime.UtcNow;
        Assert.NotNull(reservation.LastUsed);
        Assert.True(reservation.LastUsed >= beforeCall);
        Assert.True(reservation.LastUsed <= afterCall);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var reservation = new DhcpReservation { IsActive = false };

        // Act
        reservation.Activate();

        // Assert
        Assert.True(reservation.IsActive);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var reservation = new DhcpReservation { IsActive = true };

        // Act
        reservation.Deactivate();

        // Assert
        Assert.False(reservation.IsActive);
    }
}
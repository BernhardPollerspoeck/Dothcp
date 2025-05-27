using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class DhcpLeaseTests
{
    [Fact]
    public void NewLease_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var lease = new DhcpLease();
        
        // Assert
        Assert.Equal(string.Empty, lease.MacAddress);
        Assert.Equal(IPAddress.None, lease.IpAddress);
        Assert.Equal(LeaseStatus.Active, lease.Status);
        Assert.Equal(TimeSpan.FromDays(1), lease.LeaseDuration);
        Assert.True(DateTime.UtcNow >= lease.LeaseStart);
        Assert.True(DateTime.UtcNow <= lease.LeaseStart.AddSeconds(1));
    }
    
    [Fact]
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
        Assert.False(isExpired);
    }
    
    [Fact]
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
        Assert.True(isExpired);
    }
    
    [Fact]
    public void Renew_UpdatesLeaseStartAndStatus()
    {
        // Arrange
        var originalStart = DateTime.UtcNow.AddHours(-1);
        var lease = new DhcpLease
        {
            LeaseStart = originalStart,
            LeaseDuration = TimeSpan.FromHours(2),
            Status = LeaseStatus.Active
        };
        
        // Act
        lease.Renew();
        
        // Assert
        Assert.NotEqual(originalStart, lease.LeaseStart);
        Assert.Equal(LeaseStatus.Renewed, lease.Status);
        Assert.True(DateTime.UtcNow >= lease.LeaseStart);
        Assert.True(DateTime.UtcNow <= lease.LeaseStart.AddSeconds(1));
    }
    
    [Fact]
    public void Renew_WithNewDuration_UpdatesDuration()
    {
        // Arrange
        var originalDuration = TimeSpan.FromHours(2);
        var newDuration = TimeSpan.FromHours(4);
        var lease = new DhcpLease
        {
            LeaseDuration = originalDuration
        };
        
        // Act
        lease.Renew(newDuration);
        
        // Assert
        Assert.Equal(newDuration, lease.LeaseDuration);
    }
    
    [Fact]
    public void Expire_SetsStatusToExpired()
    {
        // Arrange
        var lease = new DhcpLease
        {
            Status = LeaseStatus.Active
        };
        
        // Act
        lease.Expire();
        
        // Assert
        Assert.Equal(LeaseStatus.Expired, lease.Status);
    }
}
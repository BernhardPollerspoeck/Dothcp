using qt.qsp.dhcp.Server.Grains.DhcpManager;
using System.Net;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class LeaseDatabaseTests
{
    [Fact]
    public void AddOrUpdateLease_AddsNewLease()
    {
        // Arrange
        var db = new LeaseDatabase();
        var lease = new DhcpLease
        {
            MacAddress = "00:11:22:33:44:55",
            IpAddress = IPAddress.Parse("192.168.1.100")
        };
        
        // Act
        db.AddOrUpdateLease(lease);
        
        // Assert
        var retrievedLease = db.GetLeaseByIp("192.168.1.100");
        Assert.NotNull(retrievedLease);
        Assert.Equal("00:11:22:33:44:55", retrievedLease.MacAddress);
        Assert.Equal("192.168.1.100", retrievedLease.IpAddress.ToString());
    }
    
    [Fact]
    public void GetLeaseByMac_ReturnsCorrectLease()
    {
        // Arrange
        var db = new LeaseDatabase();
        var lease = new DhcpLease
        {
            MacAddress = "00:11:22:33:44:55",
            IpAddress = IPAddress.Parse("192.168.1.100")
        };
        db.AddOrUpdateLease(lease);
        
        // Act
        var retrievedLease = db.GetLeaseByMac("00:11:22:33:44:55");
        
        // Assert
        Assert.NotNull(retrievedLease);
        Assert.Equal("192.168.1.100", retrievedLease.IpAddress.ToString());
    }
    
    [Fact]
    public void RemoveLease_RemovesLeaseAndMapping()
    {
        // Arrange
        var db = new LeaseDatabase();
        var lease = new DhcpLease
        {
            MacAddress = "00:11:22:33:44:55",
            IpAddress = IPAddress.Parse("192.168.1.100")
        };
        db.AddOrUpdateLease(lease);
        
        // Act
        var result = db.RemoveLease("192.168.1.100");
        
        // Assert
        Assert.True(result);
        Assert.Null(db.GetLeaseByIp("192.168.1.100"));
        Assert.Null(db.GetLeaseByMac("00:11:22:33:44:55"));
    }
    
    [Fact]
    public void GetLeasesByStatus_ReturnsMatchingLeases()
    {
        // Arrange
        var db = new LeaseDatabase();
        var lease1 = new DhcpLease
        {
            MacAddress = "00:11:22:33:44:55",
            IpAddress = IPAddress.Parse("192.168.1.100"),
            Status = LeaseStatus.Active
        };
        var lease2 = new DhcpLease
        {
            MacAddress = "66:77:88:99:AA:BB",
            IpAddress = IPAddress.Parse("192.168.1.101"),
            Status = LeaseStatus.Expired
        };
        db.AddOrUpdateLease(lease1);
        db.AddOrUpdateLease(lease2);
        
        // Act
        var activeLeases = db.GetLeasesByStatus(LeaseStatus.Active).ToList();
        var expiredLeases = db.GetLeasesByStatus(LeaseStatus.Expired).ToList();
        
        // Assert
        Assert.Single(activeLeases);
        Assert.Equal("00:11:22:33:44:55", activeLeases[0].MacAddress);
        
        Assert.Single(expiredLeases);
        Assert.Equal("66:77:88:99:AA:BB", expiredLeases[0].MacAddress);
    }
    
    [Fact]
    public void CheckExpiredLeases_UpdatesExpiredStatus()
    {
        // Arrange
        var db = new LeaseDatabase();
        var lease = new DhcpLease
        {
            MacAddress = "00:11:22:33:44:55",
            IpAddress = IPAddress.Parse("192.168.1.100"),
            Status = LeaseStatus.Active,
            LeaseStart = DateTime.UtcNow.AddHours(-2),
            LeaseDuration = TimeSpan.FromHours(1)
        };
        db.AddOrUpdateLease(lease);
        
        // Act
        db.CheckExpiredLeases();
        
        // Assert
        var updatedLease = db.GetLeaseByIp("192.168.1.100");
        Assert.NotNull(updatedLease);
        Assert.Equal(LeaseStatus.Expired, updatedLease.Status);
    }
}
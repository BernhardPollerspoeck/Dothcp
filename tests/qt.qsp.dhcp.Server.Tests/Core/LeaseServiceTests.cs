using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Data;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Services.Core;

namespace qt.qsp.dhcp.Server.Tests.Core;

[TestClass]
public class LeaseServiceTests
{
    private DhcpDbContext _dbContext = null!;
    private LeaseService _leaseService = null!;
    private LeaseRepository _leaseRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<DhcpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DhcpDbContext(options);
        _leaseRepository = new LeaseRepository(_dbContext);
        _leaseService = new LeaseService(_leaseRepository);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetLeaseAsync_ExistingLease_ShouldReturnLease()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var lease = new DhcpLease
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            LeaseStart = DateTime.UtcNow,
            LeaseDuration = TimeSpan.FromHours(24),
            Status = LeaseStatus.Active
        };
        await _leaseRepository.AddLeaseAsync(lease);

        // Act
        var result = await _leaseService.GetLeaseAsync(ipAddress);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ipAddress, result.IpAddressString);
        Assert.AreEqual("00:11:22:33:44:55", result.MacAddress);
    }

    [TestMethod]
    public async Task GetLeaseAsync_NonExistingLease_ShouldReturnNull()
    {
        // Act
        var result = await _leaseService.GetLeaseAsync("192.168.1.200");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetLeaseByMacAsync_ExistingMac_ShouldReturnLease()
    {
        // Arrange
        var macAddress = "00:11:22:33:44:55";
        var lease = new DhcpLease
        {
            IpAddressString = "192.168.1.100",
            MacAddress = macAddress,
            LeaseStart = DateTime.UtcNow,
            LeaseDuration = TimeSpan.FromHours(24),
            Status = LeaseStatus.Active
        };
        await _leaseRepository.AddLeaseAsync(lease);

        // Act
        var result = await _leaseService.GetLeaseByMacAsync(macAddress);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(macAddress, result.MacAddress);
        Assert.AreEqual("192.168.1.100", result.IpAddressString);
    }

    [TestMethod]
    public async Task UpdateLeaseAsync_ShouldUpdateLease()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var lease = new DhcpLease
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            LeaseStart = DateTime.UtcNow,
            LeaseDuration = TimeSpan.FromHours(24),
            Status = LeaseStatus.Active
        };
        await _leaseRepository.AddLeaseAsync(lease);

        // Act
        lease.Status = LeaseStatus.Renewed;
        await _leaseService.UpdateLeaseAsync(lease);

        // Assert
        var updated = await _leaseRepository.GetLeaseByIpAsync(ipAddress);
        Assert.IsNotNull(updated);
        Assert.AreEqual(LeaseStatus.Renewed, updated.Status);
    }

    [TestMethod]
    public async Task RevokeLeaseAsync_ShouldDeleteLease()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var lease = new DhcpLease
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            LeaseStart = DateTime.UtcNow,
            LeaseDuration = TimeSpan.FromHours(24),
            Status = LeaseStatus.Active
        };
        await _leaseRepository.AddLeaseAsync(lease);

        // Act
        await _leaseService.RevokeLeaseAsync(ipAddress);

        // Assert
        var revoked = await _leaseRepository.GetLeaseByIpAsync(ipAddress);
        Assert.IsNull(revoked);
    }

    [TestMethod]
    public async Task IsExpiredAsync_ActiveLease_ShouldReturnFalse()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var lease = new DhcpLease
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            LeaseStart = DateTime.UtcNow,
            LeaseDuration = TimeSpan.FromHours(24),
            Status = LeaseStatus.Active
        };
        await _leaseRepository.AddLeaseAsync(lease);

        // Act
        var isExpired = await _leaseService.IsExpiredAsync(ipAddress);

        // Assert
        Assert.IsFalse(isExpired);
    }

    [TestMethod]
    public async Task IsExpiredAsync_ExpiredLease_ShouldReturnTrue()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var lease = new DhcpLease
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            LeaseStart = DateTime.UtcNow.AddHours(-48),
            LeaseDuration = TimeSpan.FromHours(24),
            Status = LeaseStatus.Active
        };
        await _leaseRepository.AddLeaseAsync(lease);

        // Act
        var isExpired = await _leaseService.IsExpiredAsync(ipAddress);

        // Assert
        Assert.IsTrue(isExpired);
    }
}

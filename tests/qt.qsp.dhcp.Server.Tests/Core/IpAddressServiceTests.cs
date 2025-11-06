using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Data;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Services.Core;

namespace qt.qsp.dhcp.Server.Tests.Core;

[TestClass]
public class IpAddressServiceTests
{
    private DhcpDbContext _dbContext = null!;
    private IpAddressService _ipAddressService = null!;
    private IpAddressRepository _ipAddressRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<DhcpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DhcpDbContext(options);
        _ipAddressRepository = new IpAddressRepository(_dbContext);
        _ipAddressService = new IpAddressService(_ipAddressRepository);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetStatusAsync_NewIp_ShouldReturnAvailable()
    {
        // Act
        var status = await _ipAddressService.GetStatusAsync("192.168.1.100");

        // Assert
        Assert.AreEqual(EIpAddressStatus.Available, status);
    }

    [TestMethod]
    public async Task SetStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Offered);
        var status = await _ipAddressService.GetStatusAsync(ipAddress);

        // Assert
        Assert.AreEqual(EIpAddressStatus.Offered, status);
    }

    [TestMethod]
    public async Task SetStatusAsync_MultipleTimes_ShouldKeepLatestStatus()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Offered);
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Claimed);
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Available);
        var status = await _ipAddressService.GetStatusAsync(ipAddress);

        // Assert
        Assert.AreEqual(EIpAddressStatus.Available, status);
    }

    [TestMethod]
    public async Task GetIpAddressInfoAsync_ExistingIp_ShouldReturnInfo()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Offered);

        // Act
        var info = await _ipAddressService.GetIpAddressInfoAsync(ipAddress);

        // Assert
        Assert.IsNotNull(info);
        Assert.AreEqual(ipAddress, info.IpAddressString);
        Assert.AreEqual(EIpAddressStatus.Offered, info.Status);
    }

    [TestMethod]
    public async Task GetIpAddressInfoAsync_NonExistingIp_ShouldReturnNull()
    {
        // Act
        var info = await _ipAddressService.GetIpAddressInfoAsync("192.168.1.200");

        // Assert
        Assert.IsNull(info);
    }

    [TestMethod]
    public async Task SetStatusAsync_ShouldUpdateLastStatusChange()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var before = DateTime.UtcNow;

        // Act
        await _ipAddressService.SetStatusAsync(ipAddress, EIpAddressStatus.Offered);
        var info = await _ipAddressService.GetIpAddressInfoAsync(ipAddress);
        var after = DateTime.UtcNow;

        // Assert
        Assert.IsNotNull(info);
        Assert.IsTrue(info.LastStatusChange >= before);
        Assert.IsTrue(info.LastStatusChange <= after);
    }

    [DataTestMethod]
    [DataRow(EIpAddressStatus.Available)]
    [DataRow(EIpAddressStatus.Offered)]
    [DataRow(EIpAddressStatus.Claimed)]
    [DataRow(EIpAddressStatus.Declined)]
    public async Task SetStatusAsync_AllStatuses_ShouldWork(EIpAddressStatus status)
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        await _ipAddressService.SetStatusAsync(ipAddress, status);
        var actualStatus = await _ipAddressService.GetStatusAsync(ipAddress);

        // Assert
        Assert.AreEqual(status, actualStatus);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Data;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Models;
using qt.qsp.dhcp.Server.Services.Core;

namespace qt.qsp.dhcp.Server.Tests.Core;

[TestClass]
public class ReservationServiceCoreTests
{
    private DhcpDbContext _dbContext = null!;
    private ReservationServiceCore _reservationService = null!;
    private ReservationRepository _reservationRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<DhcpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DhcpDbContext(options);
        _reservationRepository = new ReservationRepository(_dbContext);
        _reservationService = new ReservationServiceCore(_reservationRepository);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task AddReservationAsync_ValidReservation_ShouldSucceed()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            IpAddressString = "192.168.1.100",
            MacAddress = "00:11:22:33:44:55",
            Description = "Test Server",
            IsActive = true
        };

        // Act
        await _reservationService.AddReservationAsync(reservation);

        // Assert
        var all = await _reservationService.GetAllReservationsAsync();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual(reservation.MacAddress, all[0].MacAddress);
    }

    [TestMethod]
    public async Task GetReservationByMacAsync_ExistingMac_ShouldReturnReservation()
    {
        // Arrange
        var macAddress = "00:11:22:33:44:55";
        var reservation = new DhcpReservation
        {
            IpAddressString = "192.168.1.100",
            MacAddress = macAddress,
            Description = "Test Server",
            IsActive = true
        };
        await _reservationService.AddReservationAsync(reservation);

        // Act
        var result = await _reservationService.GetReservationByMacAsync(macAddress);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(macAddress, result.MacAddress);
        Assert.AreEqual("192.168.1.100", result.IpAddressString);
    }

    [TestMethod]
    public async Task GetReservationByIpAsync_ExistingIp_ShouldReturnReservation()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var reservation = new DhcpReservation
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            Description = "Test Server",
            IsActive = true
        };
        await _reservationService.AddReservationAsync(reservation);

        // Act
        var result = await _reservationService.GetReservationByIpAsync(ipAddress);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ipAddress, result.IpAddressString);
        Assert.AreEqual("00:11:22:33:44:55", result.MacAddress);
    }

    [TestMethod]
    public async Task UpdateReservationAsync_ShouldUpdateExistingReservation()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            IpAddressString = "192.168.1.100",
            MacAddress = "00:11:22:33:44:55",
            Description = "Original Description",
            IsActive = true
        };
        await _reservationService.AddReservationAsync(reservation);

        // Act
        reservation.Description = "Updated Description";
        await _reservationService.UpdateReservationAsync(reservation);

        // Assert
        var updated = await _reservationService.GetReservationByMacAsync("00:11:22:33:44:55");
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Description", updated.Description);
    }

    [TestMethod]
    public async Task DeleteReservationAsync_ShouldRemoveReservation()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var reservation = new DhcpReservation
        {
            IpAddressString = ipAddress,
            MacAddress = "00:11:22:33:44:55",
            Description = "Test Server",
            IsActive = true
        };
        await _reservationService.AddReservationAsync(reservation);

        // Act
        await _reservationService.DeleteReservationAsync(ipAddress);

        // Assert
        var deleted = await _reservationService.GetReservationByIpAsync(ipAddress);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task HasConflictAsync_NoConflict_ShouldReturnFalse()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            IpAddressString = "192.168.1.100",
            MacAddress = "00:11:22:33:44:55",
            Description = "Test Server",
            IsActive = true
        };

        // Act
        var hasConflict = await _reservationService.HasConflictAsync(reservation);

        // Assert
        Assert.IsFalse(hasConflict);
    }

    [TestMethod]
    public async Task HasConflictAsync_IpConflict_ShouldReturnTrue()
    {
        // Arrange
        var existing = new DhcpReservation
        {
            IpAddressString = "192.168.1.100",
            MacAddress = "00:11:22:33:44:55",
            Description = "Existing Server",
            IsActive = true
        };
        await _reservationService.AddReservationAsync(existing);

        var newReservation = new DhcpReservation
        {
            IpAddressString = "192.168.1.100", // Same IP
            MacAddress = "AA:BB:CC:DD:EE:FF", // Different MAC
            Description = "New Server",
            IsActive = true
        };

        // Act
        var hasConflict = await _reservationService.HasConflictAsync(newReservation);

        // Assert
        Assert.IsTrue(hasConflict);
    }

    [TestMethod]
    public async Task HasConflictAsync_MacConflict_ShouldReturnTrue()
    {
        // Arrange
        var existing = new DhcpReservation
        {
            IpAddressString = "192.168.1.100",
            MacAddress = "00:11:22:33:44:55",
            Description = "Existing Server",
            IsActive = true
        };
        await _reservationService.AddReservationAsync(existing);

        var newReservation = new DhcpReservation
        {
            IpAddressString = "192.168.1.101", // Different IP
            MacAddress = "00:11:22:33:44:55", // Same MAC
            Description = "New Server",
            IsActive = true
        };

        // Act
        var hasConflict = await _reservationService.HasConflictAsync(newReservation);

        // Assert
        Assert.IsTrue(hasConflict);
    }

    [TestMethod]
    public async Task GetAllReservationsAsync_MultipleReservations_ShouldReturnAll()
    {
        // Arrange
        var reservations = new[]
        {
            new DhcpReservation { IpAddressString = "192.168.1.100", MacAddress = "00:11:22:33:44:55", IsActive = true },
            new DhcpReservation { IpAddressString = "192.168.1.101", MacAddress = "00:11:22:33:44:56", IsActive = true },
            new DhcpReservation { IpAddressString = "192.168.1.102", MacAddress = "00:11:22:33:44:57", IsActive = true }
        };

        foreach (var res in reservations)
        {
            await _reservationService.AddReservationAsync(res);
        }

        // Act
        var all = await _reservationService.GetAllReservationsAsync();

        // Assert
        Assert.AreEqual(3, all.Count);
    }
}

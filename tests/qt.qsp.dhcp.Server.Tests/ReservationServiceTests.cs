using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using qt.qsp.dhcp.Server.Services;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class ReservationServiceTests
{
    private readonly Mock<IGrainFactory> _grainFactoryMock;
    private readonly Mock<ILogger<ReservationService>> _loggerMock;
    private readonly ReservationService _reservationService;

    public ReservationServiceTests()
    {
        _grainFactoryMock = new Mock<IGrainFactory>();
        _loggerMock = new Mock<ILogger<ReservationService>>();
        _reservationService = new ReservationService(_grainFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddReservationAsync_ValidReservation_ShouldReturnSuccess()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            IpAddress = IPAddress.Parse("192.168.1.100"),
            MacAddress = "00:11:22:33:44:55",
            Description = "Test Server",
            IsActive = true
        };

        var managerGrainMock = new Mock<IDhcpReservationManagerGrain>();
        managerGrainMock.Setup(x => x.AddReservation(It.IsAny<DhcpReservation>()))
                       .ReturnsAsync(true);

        _grainFactoryMock.Setup(x => x.GetGrain<IDhcpReservationManagerGrain>(0, null))
                        .Returns(managerGrainMock.Object);

        // Act
        var result = await _reservationService.AddReservationAsync(reservation);

        // Assert
        Assert.True(result.success);
        Assert.Null(result.errorMessage);
        managerGrainMock.Verify(x => x.AddReservation(reservation), Times.Once);
    }

    [Fact]
    public async Task AddReservationAsync_EmptyMacAddress_ShouldReturnFailure()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            IpAddress = IPAddress.Parse("192.168.1.100"),
            MacAddress = string.Empty,
            Description = "Test Server",
            IsActive = true
        };

        // Act
        var result = await _reservationService.AddReservationAsync(reservation);

        // Assert
        Assert.False(result.success);
        Assert.Equal("MAC address is required", result.errorMessage);
        _grainFactoryMock.Verify(x => x.GetGrain<IDhcpReservationManagerGrain>(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddReservationAsync_NoneIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var reservation = new DhcpReservation
        {
            IpAddress = IPAddress.None,
            MacAddress = "00:11:22:33:44:55",
            Description = "Test Server",
            IsActive = true
        };

        // Act
        var result = await _reservationService.AddReservationAsync(reservation);

        // Assert
        Assert.False(result.success);
        Assert.Equal("IP address is required", result.errorMessage);
        _grainFactoryMock.Verify(x => x.GetGrain<IDhcpReservationManagerGrain>(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllReservationsAsync_WhenCalled_ShouldReturnReservationsList()
    {
        // Arrange
        var expectedReservations = new List<DhcpReservation>
        {
            new() { IpAddress = IPAddress.Parse("192.168.1.100"), MacAddress = "00:11:22:33:44:55" },
            new() { IpAddress = IPAddress.Parse("192.168.1.101"), MacAddress = "00:11:22:33:44:56" }
        };

        var managerGrainMock = new Mock<IDhcpReservationManagerGrain>();
        managerGrainMock.Setup(x => x.GetAllReservations())
                       .ReturnsAsync(expectedReservations);

        _grainFactoryMock.Setup(x => x.GetGrain<IDhcpReservationManagerGrain>(0, null))
                        .Returns(managerGrainMock.Object);

        // Act
        var result = await _reservationService.GetAllReservationsAsync();

        // Assert
        Assert.Equal(expectedReservations.Count, result.Count);
        Assert.Equal(expectedReservations[0].MacAddress, result[0].MacAddress);
        Assert.Equal(expectedReservations[1].MacAddress, result[1].MacAddress);
    }

    [Fact]
    public async Task GetReservationByMacAsync_ExistingMac_ShouldReturnReservation()
    {
        // Arrange
        var macAddress = "00:11:22:33:44:55";
        var expectedReservation = new DhcpReservation
        {
            IpAddress = IPAddress.Parse("192.168.1.100"),
            MacAddress = macAddress
        };

        var managerGrainMock = new Mock<IDhcpReservationManagerGrain>();
        managerGrainMock.Setup(x => x.GetReservationByMac(macAddress))
                       .ReturnsAsync(expectedReservation);

        _grainFactoryMock.Setup(x => x.GetGrain<IDhcpReservationManagerGrain>(0, null))
                        .Returns(managerGrainMock.Object);

        // Act
        var result = await _reservationService.GetReservationByMacAsync(macAddress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedReservation.MacAddress, result.MacAddress);
        Assert.Equal(expectedReservation.IpAddress, result.IpAddress);
    }

    [Fact]
    public async Task DeleteReservationAsync_ValidIp_ShouldReturnSuccess()
    {
        // Arrange
        var ipAddress = IPAddress.Parse("192.168.1.100");

        var managerGrainMock = new Mock<IDhcpReservationManagerGrain>();
        managerGrainMock.Setup(x => x.DeleteReservation(ipAddress))
                       .ReturnsAsync(true);

        _grainFactoryMock.Setup(x => x.GetGrain<IDhcpReservationManagerGrain>(0, null))
                        .Returns(managerGrainMock.Object);

        // Act
        var result = await _reservationService.DeleteReservationAsync(ipAddress);

        // Assert
        Assert.True(result.success);
        Assert.Null(result.errorMessage);
        managerGrainMock.Verify(x => x.DeleteReservation(ipAddress), Times.Once);
    }
}
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using qt.qsp.dhcp.Server.Services;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnDashboardData()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var logger = NullLogger<DashboardService>.Instance;
        
        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger);

        // Act
        var result = await dashboardService.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ServerStatus);
        Assert.NotNull(result.LeaseStatistics);
        Assert.NotNull(result.RecentLeases);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnServerStatusWithUptime()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var logger = NullLogger<DashboardService>.Instance;
        
        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger);

        // Act
        var result = await dashboardService.GetDashboardDataAsync();

        // Assert
        Assert.Equal(ServerState.Running, result.ServerStatus.State);
        Assert.NotNull(result.ServerStatus.NetworkInterfaces);
        // Check uptime is not negative (allow for very small negative values due to timing)
        Assert.True(result.ServerStatus.Uptime.TotalMilliseconds >= -100);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnLeaseStatistics()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var logger = NullLogger<DashboardService>.Instance;
        
        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger);

        // Act
        var result = await dashboardService.GetDashboardDataAsync();

        // Assert
        Assert.True(result.LeaseStatistics.TotalAddresses > 0);
        Assert.True(result.LeaseStatistics.LeasedAddresses >= 0);
        Assert.True(result.LeaseStatistics.ReservedAddresses >= 0);
        Assert.True(result.LeaseStatistics.AvailableAddresses >= 0);
        Assert.True(result.LeaseStatistics.UtilizationPercentage >= 0 && 
                   result.LeaseStatistics.UtilizationPercentage <= 100);
    }
}
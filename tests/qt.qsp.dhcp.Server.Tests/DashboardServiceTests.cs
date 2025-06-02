using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Utilities;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class DashboardServiceTests
{
    private DashboardService CreateDashboardService()
    {
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var mockSettingsLoader = new Mock<ISettingsLoaderService>();
        var mockNetworkUtility = new Mock<INetworkUtilityService>();
        var mockDhcpServerService = new Mock<IDhcpServerService>();
        var logger = NullLogger<DashboardService>.Instance;

        // Setup default values for settings
        mockSettingsLoader.Setup(x => x.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW))
            .ReturnsAsync((byte)10);
        mockSettingsLoader.Setup(x => x.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH))
            .ReturnsAsync((byte)100);
        mockSettingsLoader.Setup(x => x.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))
            .ReturnsAsync(new byte[] { 192, 168, 1, 1 });
        mockSettingsLoader.Setup(x => x.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET))
            .ReturnsAsync("255.255.255.0");

        // Setup network utility mock methods
        mockNetworkUtility.Setup(x => x.CalculateNetworkAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("192.168.1.0");
        mockNetworkUtility.Setup(x => x.CalculateBroadcastAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("192.168.1.255");
        mockNetworkUtility.Setup(x => x.IsReservedIp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string ip, string network, string broadcast) => 
                ip == network || ip == broadcast);

        // Setup lease grain mock
        var mockLeaseGrain = new Mock<IDhcpLeaseGrain>();
        mockLeaseGrain.Setup(x => x.GetLease()).ReturnsAsync((DhcpLease?)null);
        mockGrainFactory.Setup(x => x.GetGrain<IDhcpLeaseGrain>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockLeaseGrain.Object);

        // Setup DHCP server service mock
        mockDhcpServerService.Setup(x => x.CurrentState).Returns(ServerState.Running);

        return new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger,
            mockSettingsLoader.Object,
            mockNetworkUtility.Object,
            mockDhcpServerService.Object);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnDashboardData()
    {
        // Arrange
        var dashboardService = CreateDashboardService();

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
        var dashboardService = CreateDashboardService();

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
        var dashboardService = CreateDashboardService();

        // Act
        var result = await dashboardService.GetDashboardDataAsync();

        // Assert
        Assert.True(result.LeaseStatistics.TotalAddresses >= 0);
        Assert.True(result.LeaseStatistics.LeasedAddresses >= 0);
        Assert.True(result.LeaseStatistics.ReservedAddresses >= 0);
        Assert.True(result.LeaseStatistics.AvailableAddresses >= 0);
        Assert.True(result.LeaseStatistics.UtilizationPercentage >= 0 && 
                   result.LeaseStatistics.UtilizationPercentage <= 100);
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithNullSettings_ShouldNotCrashAndReturnEmptyData()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var mockSettingsLoader = new Mock<ISettingsLoaderService>();
        var mockNetworkUtility = new Mock<INetworkUtilityService>();
        var mockDhcpServerService = new Mock<IDhcpServerService>();
        var logger = NullLogger<DashboardService>.Instance;

        // Setup settings to return null values (simulating fresh startup without settings)
        mockSettingsLoader.Setup(x => x.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))
            .ReturnsAsync(null as byte[]);
        mockSettingsLoader.Setup(x => x.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET))
            .ReturnsAsync(null as string);
        mockSettingsLoader.Setup(x => x.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW))
            .ReturnsAsync((byte)0);
        mockSettingsLoader.Setup(x => x.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH))
            .ReturnsAsync((byte)0);

        // Setup DHCP server service mock
        mockDhcpServerService.Setup(x => x.CurrentState).Returns(ServerState.Stopped);

        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger,
            mockSettingsLoader.Object,
            mockNetworkUtility.Object,
            mockDhcpServerService.Object);

        // Act & Assert - should not throw exception
        var result = await dashboardService.GetDashboardDataAsync();

        // Assert - should return valid but empty data
        Assert.NotNull(result);
        Assert.NotNull(result.DhcpNetwork);
        Assert.NotNull(result.ServerStatus);
        Assert.NotNull(result.LeaseStatistics);
        Assert.NotNull(result.RecentLeases);
        
        // DHCP network info should be empty when settings don't exist
        Assert.True(string.IsNullOrEmpty(result.DhcpNetwork.NetworkAddress) || 
                    result.DhcpNetwork.NetworkAddress == "0.0.0.0");
        Assert.True(string.IsNullOrEmpty(result.DhcpNetwork.RouterAddress) || 
                    result.DhcpNetwork.RouterAddress == "0.0.0.0");
        
        // Lease statistics should be zero when settings don't exist
        Assert.Equal(0, result.LeaseStatistics.TotalAddresses);
        Assert.Equal(0, result.LeaseStatistics.LeasedAddresses);
        Assert.Equal(0, result.LeaseStatistics.ReservedAddresses);
        
        // Recent leases should be empty
        Assert.Empty(result.RecentLeases);
    }
}
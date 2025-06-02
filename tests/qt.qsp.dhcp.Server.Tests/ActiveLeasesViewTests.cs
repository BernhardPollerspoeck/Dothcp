using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Orleans;
using qt.qsp.dhcp.Server.Grains.DhcpManager;
using qt.qsp.dhcp.Server.Services;
using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Utilities;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class ActiveLeasesViewTests
{
    [Fact]
    public async Task GetAllLeasesAsync_WithDevelopmentEnvironment_ShouldReturnTestLeases()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var mockSettingsLoader = new Mock<ISettingsLoaderService>();
        var mockNetworkUtility = new Mock<INetworkUtilityService>();
        var mockDhcpServerService = new Mock<IDhcpServerService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var logger = NullLogger<DashboardService>.Instance;

        // Setup settings to return null values (simulating fresh startup without settings)
        mockSettingsLoader.Setup(x => x.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))
            .ReturnsAsync(null as byte[]);
        mockSettingsLoader.Setup(x => x.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET))
            .ReturnsAsync(null as string);

        // Setup environment to be development
        mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger,
            mockSettingsLoader.Object,
            mockNetworkUtility.Object,
            mockDhcpServerService.Object,
            mockEnvironment.Object);

        // Act
        var result = await dashboardService.GetAllLeasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(8, result.Count); // Should return 8 test leases
        
        // Verify we have different lease statuses for testing
        Assert.Contains(result, l => l.Status == LeaseStatus.Active);
        Assert.Contains(result, l => l.Status == LeaseStatus.Expired);
        Assert.Contains(result, l => l.Status == LeaseStatus.Renewed);
        
        // Verify lease data is realistic
        Assert.All(result, lease =>
        {
            Assert.False(string.IsNullOrEmpty(lease.MacAddress));
            Assert.NotNull(lease.IpAddress);
            Assert.False(string.IsNullOrEmpty(lease.HostName));
        });
    }

    [Fact]
    public async Task GetAllLeasesAsync_WithProductionEnvironment_ShouldNotReturnTestLeases()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var mockSettingsLoader = new Mock<ISettingsLoaderService>();
        var mockNetworkUtility = new Mock<INetworkUtilityService>();
        var mockDhcpServerService = new Mock<IDhcpServerService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var logger = NullLogger<DashboardService>.Instance;

        // Setup settings to return null values (simulating fresh startup without settings)
        mockSettingsLoader.Setup(x => x.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))
            .ReturnsAsync(null as byte[]);
        mockSettingsLoader.Setup(x => x.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET))
            .ReturnsAsync(null as string);

        // Setup environment to be production
        mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger,
            mockSettingsLoader.Object,
            mockNetworkUtility.Object,
            mockDhcpServerService.Object,
            mockEnvironment.Object);

        // Act
        var result = await dashboardService.GetAllLeasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // Should return empty list in production with no settings
    }

    [Fact]
    public async Task GetAllLeasesAsync_WithValidSettings_ShouldQueryGrains()
    {
        // Arrange
        var mockGrainFactory = new Mock<IGrainFactory>();
        var mockLeaseSearchService = new Mock<ILeaseGrainSearchService>();
        var mockSettingsLoader = new Mock<ISettingsLoaderService>();
        var mockNetworkUtility = new Mock<INetworkUtilityService>();
        var mockDhcpServerService = new Mock<IDhcpServerService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var mockLeaseGrain = new Mock<IDhcpLeaseGrain>();
        var logger = NullLogger<DashboardService>.Instance;

        // Setup valid settings
        mockSettingsLoader.Setup(x => x.GetSetting<byte>(SettingsConstants.DHCP_RANGE_LOW))
            .ReturnsAsync((byte)10);
        mockSettingsLoader.Setup(x => x.GetSetting<byte>(SettingsConstants.DHCP_RANGE_HIGH))
            .ReturnsAsync((byte)12);
        mockSettingsLoader.Setup(x => x.GetSetting<byte[]>(SettingsConstants.DHCP_LEASE_ROUTER))
            .ReturnsAsync(new byte[] { 192, 168, 1, 1 });
        mockSettingsLoader.Setup(x => x.GetSetting<string>(SettingsConstants.DHCP_LEASE_SUBNET))
            .ReturnsAsync("255.255.255.0");

        // Setup network utility mocks
        mockNetworkUtility.Setup(x => x.CalculateNetworkAddress("192.168.1.0", "255.255.255.0"))
            .Returns("192.168.1.0");
        mockNetworkUtility.Setup(x => x.CalculateBroadcastAddress("192.168.1.0", "255.255.255.0"))
            .Returns("192.168.1.255");
        mockNetworkUtility.Setup(x => x.IsReservedIp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Setup lease grain to return null (no leases)
        mockLeaseGrain.Setup(x => x.GetLease()).ReturnsAsync((DhcpLease?)null);
        mockGrainFactory.Setup(x => x.GetGrain<IDhcpLeaseGrain>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockLeaseGrain.Object);

        // Setup environment to not be development
        mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

        var dashboardService = new DashboardService(
            mockGrainFactory.Object,
            mockLeaseSearchService.Object,
            logger,
            mockSettingsLoader.Object,
            mockNetworkUtility.Object,
            mockDhcpServerService.Object,
            mockEnvironment.Object);

        // Act
        var result = await dashboardService.GetAllLeasesAsync();

        // Assert
        Assert.NotNull(result);
        
        // Verify grains were queried for each IP in range (10, 11, 12)
        mockGrainFactory.Verify(x => x.GetGrain<IDhcpLeaseGrain>("192.168.1.10", It.IsAny<string>()), Times.Once);
        mockGrainFactory.Verify(x => x.GetGrain<IDhcpLeaseGrain>("192.168.1.11", It.IsAny<string>()), Times.Once);
        mockGrainFactory.Verify(x => x.GetGrain<IDhcpLeaseGrain>("192.168.1.12", It.IsAny<string>()), Times.Once);
    }
}
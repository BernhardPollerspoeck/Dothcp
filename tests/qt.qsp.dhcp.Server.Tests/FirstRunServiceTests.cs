using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.Settings;
using qt.qsp.dhcp.Server.Services;
using Moq;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class FirstRunServiceTests
{
	private readonly Mock<IClusterClient> _mockClusterClient;
	private readonly Mock<ISettingsGrain> _mockSettingsGrain;
	private readonly FirstRunService _firstRunService;

	public FirstRunServiceTests()
	{
		_mockClusterClient = new Mock<IClusterClient>();
		_mockSettingsGrain = new Mock<ISettingsGrain>();
		_firstRunService = new FirstRunService(_mockClusterClient.Object);

		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(It.IsAny<string>(), null))
			.Returns(_mockSettingsGrain.Object);
	}

	[Fact]
	public async Task IsFirstRunAsync_WhenAllSettingsExist_ReturnsFalse()
	{
		// Arrange
		_mockSettingsGrain.Setup(g => g.HasValue()).ReturnsAsync(true);

		// Act
		var result = await _firstRunService.IsFirstRunAsync();

		// Assert
		Assert.False(result);
		
		// Verify all required settings were checked
		_mockClusterClient.Verify(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET, null), Times.Once);
		_mockClusterClient.Verify(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER, null), Times.Once);
		_mockClusterClient.Verify(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW, null), Times.Once);
		_mockClusterClient.Verify(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH, null), Times.Once);
	}

	[Fact]
	public async Task IsFirstRunAsync_WhenSubnetMissing_ReturnsTrue()
	{
		// Arrange
		var subnetGrain = new Mock<ISettingsGrain>();
		var otherGrain = new Mock<ISettingsGrain>();
		
		subnetGrain.Setup(g => g.HasValue()).ReturnsAsync(false);
		otherGrain.Setup(g => g.HasValue()).ReturnsAsync(true);
		
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET, null))
			.Returns(subnetGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH, null))
			.Returns(otherGrain.Object);

		// Act
		var result = await _firstRunService.IsFirstRunAsync();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task IsFirstRunAsync_WhenRouterMissing_ReturnsTrue()
	{
		// Arrange
		var routerGrain = new Mock<ISettingsGrain>();
		var otherGrain = new Mock<ISettingsGrain>();
		
		routerGrain.Setup(g => g.HasValue()).ReturnsAsync(false);
		otherGrain.Setup(g => g.HasValue()).ReturnsAsync(true);
		
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER, null))
			.Returns(routerGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH, null))
			.Returns(otherGrain.Object);

		// Act
		var result = await _firstRunService.IsFirstRunAsync();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task IsFirstRunAsync_WhenRangeLowMissing_ReturnsTrue()
	{
		// Arrange
		var rangeLowGrain = new Mock<ISettingsGrain>();
		var otherGrain = new Mock<ISettingsGrain>();
		
		rangeLowGrain.Setup(g => g.HasValue()).ReturnsAsync(false);
		otherGrain.Setup(g => g.HasValue()).ReturnsAsync(true);
		
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW, null))
			.Returns(rangeLowGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH, null))
			.Returns(otherGrain.Object);

		// Act
		var result = await _firstRunService.IsFirstRunAsync();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task IsFirstRunAsync_WhenRangeHighMissing_ReturnsTrue()
	{
		// Arrange
		var rangeHighGrain = new Mock<ISettingsGrain>();
		var otherGrain = new Mock<ISettingsGrain>();
		
		rangeHighGrain.Setup(g => g.HasValue()).ReturnsAsync(false);
		otherGrain.Setup(g => g.HasValue()).ReturnsAsync(true);
		
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_SUBNET, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_LEASE_ROUTER, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_LOW, null))
			.Returns(otherGrain.Object);
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(SettingsConstants.DHCP_RANGE_HIGH, null))
			.Returns(rangeHighGrain.Object);

		// Act
		var result = await _firstRunService.IsFirstRunAsync();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task IsFirstRunAsync_WhenExceptionThrown_ReturnsTrue()
	{
		// Arrange
		_mockClusterClient
			.Setup(c => c.GetGrain<ISettingsGrain>(It.IsAny<string>(), null))
			.Throws(new Exception("Test exception"));

		// Act
		var result = await _firstRunService.IsFirstRunAsync();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task MarkSetupCompletedAsync_CompletesSuccessfully()
	{
		// Act & Assert
		await _firstRunService.MarkSetupCompletedAsync();
		
		// Should complete without throwing
	}
}
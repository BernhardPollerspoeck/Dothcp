using qt.qsp.dhcp.Server.Constants;
using qt.qsp.dhcp.Server.Grains.Settings;
using qt.qsp.dhcp.Server.Services;
using Moq;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class SettingsServiceTests
{
	private readonly Mock<IGrainFactory> _mockGrainFactory;
	private readonly Mock<ISettingsGrain> _mockSettingsGrain;
	private readonly SettingsService _settingsService;

	public SettingsServiceTests()
	{
		_mockGrainFactory = new Mock<IGrainFactory>();
		_mockSettingsGrain = new Mock<ISettingsGrain>();
		_settingsService = new SettingsService(_mockGrainFactory.Object);

		_mockGrainFactory
			.Setup(f => f.GetGrain<ISettingsGrain>(It.IsAny<string>(), It.IsAny<string>()))
			.Returns(_mockSettingsGrain.Object);
	}

	[Fact]
	public async Task GetSettingAsync_ShouldCallGrainGetValue()
	{
		// Arrange
		const string key = "test-key";
		const string expectedValue = "test-value";
		_mockSettingsGrain.Setup(g => g.GetValue<string>()).ReturnsAsync(expectedValue);

		// Act
		var result = await _settingsService.GetSettingAsync<string>(key);

		// Assert
		Assert.Equal(expectedValue, result);
		_mockGrainFactory.Verify(f => f.GetGrain<ISettingsGrain>(key, null), Times.Once);
		_mockSettingsGrain.Verify(g => g.GetValue<string>(), Times.Once);
	}

	[Fact]
	public async Task SetSettingAsync_ShouldCallGrainSetValue()
	{
		// Arrange
		const string key = "test-key";
		const string value = "test-value";
		_mockSettingsGrain.Setup(g => g.SetValue(value)).Returns(Task.CompletedTask);

		// Act
		await _settingsService.SetSettingAsync(key, value);

		// Assert
		_mockGrainFactory.Verify(f => f.GetGrain<ISettingsGrain>(key, null), Times.Once);
		_mockSettingsGrain.Verify(g => g.SetValue(value), Times.Once);
	}

	[Theory]
	[InlineData(SettingsConstants.DHCP_RANGE_LOW, "10", true)]
	[InlineData(SettingsConstants.DHCP_RANGE_LOW, "255", false)]
	[InlineData(SettingsConstants.DHCP_RANGE_LOW, "0", false)]
	[InlineData(SettingsConstants.DHCP_RANGE_LOW, "abc", false)]
	[InlineData(SettingsConstants.DHCP_RANGE_HIGH, "254", true)]
	[InlineData(SettingsConstants.DHCP_RANGE_HIGH, "255", false)]
	[InlineData(SettingsConstants.DHCP_RANGE_HIGH, "0", false)]
	public async Task ValidateSettingAsync_ShouldValidateRangeValues(string key, string value, bool expectedValid)
	{
		// Act
		var result = await _settingsService.ValidateSettingAsync(key, value);

		// Assert
		Assert.Equal(expectedValid, result);
	}

	[Theory]
	[InlineData(SettingsConstants.DHCP_LEASE_TIME, "24:00:00", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_TIME, "00:30:00", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_TIME, "00:00:00", false)]
	[InlineData(SettingsConstants.DHCP_LEASE_TIME, "invalid", false)]
	public async Task ValidateSettingAsync_ShouldValidateTimeSpanValues(string key, string value, bool expectedValid)
	{
		// Act
		var result = await _settingsService.ValidateSettingAsync(key, value);

		// Assert
		Assert.Equal(expectedValid, result);
	}

	[Theory]
	[InlineData(SettingsConstants.DHCP_LEASE_SUBNET, "192.168.1.0", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_SUBNET, "10.0.0.0", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_SUBNET, "192.168.1.256", false)]
	[InlineData(SettingsConstants.DHCP_LEASE_SUBNET, "invalid-ip", false)]
	[InlineData(SettingsConstants.DHCP_LEASE_ROUTER, "192.168.1.1", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_ROUTER, "invalid-ip", false)]
	public async Task ValidateSettingAsync_ShouldValidateIpAddresses(string key, string value, bool expectedValid)
	{
		// Act
		var result = await _settingsService.ValidateSettingAsync(key, value);

		// Assert
		Assert.Equal(expectedValid, result);
	}

	[Theory]
	[InlineData(SettingsConstants.DHCP_LEASE_DNS, "8.8.8.8", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_DNS, "8.8.8.8;8.8.4.4", true)]
	[InlineData(SettingsConstants.DHCP_LEASE_DNS, "", true)] // DNS is optional
	[InlineData(SettingsConstants.DHCP_LEASE_DNS, "8.8.8.8;invalid-ip", false)]
	[InlineData(SettingsConstants.DHCP_LEASE_DNS, "invalid-ip", false)]
	public async Task ValidateSettingAsync_ShouldValidateDnsServers(string key, string value, bool expectedValid)
	{
		// Act
		var result = await _settingsService.ValidateSettingAsync(key, value);

		// Assert
		Assert.Equal(expectedValid, result);
	}

	[Fact]
	public async Task ValidateSettingAsync_ShouldReturnFalseForNullOrWhiteSpace()
	{
		// Act & Assert
		Assert.False(await _settingsService.ValidateSettingAsync(SettingsConstants.DHCP_RANGE_LOW, null!));
		Assert.False(await _settingsService.ValidateSettingAsync(SettingsConstants.DHCP_RANGE_LOW, ""));
		Assert.False(await _settingsService.ValidateSettingAsync(SettingsConstants.DHCP_RANGE_LOW, "   "));
	}

	[Fact]
	public async Task ValidateSettingAsync_ShouldReturnTrueForUnknownSettings()
	{
		// Act
		var result = await _settingsService.ValidateSettingAsync("unknown-setting", "any-value");

		// Assert
		Assert.True(result);
	}
}
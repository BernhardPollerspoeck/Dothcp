using qt.qsp.dhcp.Server.Grains.Settings;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Moq;
using Xunit;

namespace qt.qsp.dhcp.Server.Tests;

public class SettingsGrainTests
{
	private readonly Mock<IPersistentState<AppSetting>> _mockPersistentState;
	private readonly Mock<ILogger<SettingsGrain>> _mockLogger;
	private readonly SettingsGrain _settingsGrain;

	public SettingsGrainTests()
	{
		_mockPersistentState = new Mock<IPersistentState<AppSetting>>();
		_mockLogger = new Mock<ILogger<SettingsGrain>>();
		_settingsGrain = new SettingsGrain(_mockPersistentState.Object, _mockLogger.Object);
	}

	[Fact]
	public async Task GetValue_WithNullValue_ReturnsDefaultString()
	{
		// Arrange
		var appSetting = new AppSetting { Value = null! };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);

		// Act
		var result = await _settingsGrain.GetValue<string>();

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetValue_WithNullValue_ReturnsDefaultByte()
	{
		// Arrange
		var appSetting = new AppSetting { Value = null! };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);

		// Act
		var result = await _settingsGrain.GetValue<byte>();

		// Assert
		Assert.Equal(default(byte), result);
	}

	[Fact]
	public async Task GetValue_WithNullValue_ReturnsDefaultTimeSpan()
	{
		// Arrange
		var appSetting = new AppSetting { Value = null! };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);

		// Act
		var result = await _settingsGrain.GetValue<TimeSpan>();

		// Assert
		Assert.Equal(default(TimeSpan), result);
	}

	[Fact]
	public async Task GetValue_WithValidStringValue_ReturnsValue()
	{
		// Arrange
		var expectedValue = "test-value";
		var appSetting = new AppSetting { Value = expectedValue };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);

		// Act
		var result = await _settingsGrain.GetValue<string>();

		// Assert
		Assert.Equal(expectedValue, result);
	}

	[Fact]
	public async Task GetValue_WithValidByteValue_ReturnsValue()
	{
		// Arrange
		byte expectedValue = 192;
		var appSetting = new AppSetting { Value = expectedValue.ToString() };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);

		// Act
		var result = await _settingsGrain.GetValue<byte>();

		// Assert
		Assert.Equal(expectedValue, result);
	}

	[Fact]
	public async Task HasValue_WithNullValue_ReturnsFalse()
	{
		// Arrange
		var appSetting = new AppSetting { Value = null! };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);
		_mockPersistentState.Setup(s => s.RecordExists).Returns(false);

		// Act
		var result = await _settingsGrain.HasValue();

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task HasValue_WithValidValue_ReturnsTrue()
	{
		// Arrange
		var appSetting = new AppSetting { Value = "test-value" };
		_mockPersistentState.Setup(s => s.State).Returns(appSetting);
		_mockPersistentState.Setup(s => s.RecordExists).Returns(true);

		// Act
		var result = await _settingsGrain.HasValue();

		// Assert
		Assert.True(result);
	}


}
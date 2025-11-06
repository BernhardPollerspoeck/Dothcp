using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qt.qsp.dhcp.Server.Data;
using qt.qsp.dhcp.Server.Data.Repositories;
using qt.qsp.dhcp.Server.Services.Core;

namespace qt.qsp.dhcp.Server.Tests.Core;

[TestClass]
public class ConfigurationServiceTests
{
    private DhcpDbContext _dbContext = null!;
    private ConfigurationService _configurationService = null!;
    private SettingsRepository _settingsRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<DhcpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DhcpDbContext(options);
        _settingsRepository = new SettingsRepository(_dbContext);
        _configurationService = new ConfigurationService(_settingsRepository);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task SetAndGetSettingAsync_String_ShouldReturnSameValue()
    {
        // Arrange
        var key = "test.string";
        var value = "test value";

        // Act
        await _configurationService.SetSettingAsync(key, value);
        var result = await _configurationService.GetSettingAsync<string>(key);

        // Assert
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task SetAndGetSettingAsync_Byte_ShouldReturnSameValue()
    {
        // Arrange
        var key = "test.byte";
        byte value = 42;

        // Act
        await _configurationService.SetSettingAsync(key, value);
        var result = await _configurationService.GetSettingAsync<byte>(key);

        // Assert
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task SetAndGetSettingAsync_TimeSpan_ShouldReturnSameValue()
    {
        // Arrange
        var key = "test.timespan";
        var value = TimeSpan.FromHours(24);

        // Act
        await _configurationService.SetSettingAsync(key, value);
        var result = await _configurationService.GetSettingAsync<TimeSpan>(key);

        // Assert
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task SetAndGetSettingAsync_StringArray_ShouldReturnSameValue()
    {
        // Arrange
        var key = "test.stringarray";
        var value = new[] { "8.8.8.8", "8.8.4.4" };

        // Act
        await _configurationService.SetSettingAsync(key, value);
        var result = await _configurationService.GetSettingAsync<string[]>(key);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(value.Length, result.Length);
        CollectionAssert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task SetAndGetSettingAsync_ByteArray_ShouldReturnSameValue()
    {
        // Arrange
        var key = "test.bytearray";
        var value = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        await _configurationService.SetSettingAsync(key, value);
        var result = await _configurationService.GetSettingAsync<byte[]>(key);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(value.Length, result.Length);
        CollectionAssert.AreEqual(value, result);
    }

    [TestMethod]
    public async Task GetSettingAsync_NonExistentKey_ShouldReturnNull()
    {
        // Act
        var result = await _configurationService.GetSettingAsync<string>("nonexistent.key");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task HasSettingAsync_ExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var key = "test.exists";
        await _configurationService.SetSettingAsync(key, "value");

        // Act
        var result = await _configurationService.HasSettingAsync(key);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasSettingAsync_NonExistentKey_ShouldReturnFalse()
    {
        // Act
        var result = await _configurationService.HasSettingAsync("nonexistent.key");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UpdateSetting_ShouldOverwriteExisting()
    {
        // Arrange
        var key = "test.update";
        await _configurationService.SetSettingAsync(key, "original");

        // Act
        await _configurationService.SetSettingAsync(key, "updated");
        var result = await _configurationService.GetSettingAsync<string>(key);

        // Assert
        Assert.AreEqual("updated", result);
    }
}

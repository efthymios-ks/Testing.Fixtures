using Microsoft.Extensions.Logging.Abstractions;
using Testing.Fixtures.Core;
using Testing.Fixtures.Redis;
using Testing.Fixtures.Settings;
using Testing.Fixtures.Sql;

namespace Testing.Fixtures.Tests;

public sealed class FixtureOptionsTests
{
    [Fact]
    public void RedisOptions_WhenUnset_ShouldPinTheImageAndMatchTheDefaultParallelism()
    {
        // Arrange
        var options = new RedisFixtureOptions();

        // Act & Assert
        Assert.DoesNotContain("latest", options.Image, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(16, options.DatabaseCount);
        Assert.Equal("REDIS_CONNECTION_STRING", options.SettingKey);
    }

    [Fact]
    public void RedisOptions_WhenTheDatabaseCountIsBelowOne_ShouldThrow()
    {
        // Arrange
        var options = new RedisFixtureOptions();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DatabaseCount = 0);
    }

    [Fact]
    public void SqlOptions_WhenUnset_ShouldPinTheImage()
    {
        // Arrange
        var options = new SqlServerFixtureOptions();

        // Act & Assert
        Assert.DoesNotContain("latest", options.Image, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SqlDatabaseOptions_WhenTheDatabaseCountIsBelowOne_ShouldThrow()
    {
        // Arrange
        var options = new SqlDatabaseFixtureOptions();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DatabaseCount = 0);
    }

    [Fact]
    public void SqlDatabaseOptions_WhenTheDataDirectoryEndsWithASlash_ShouldStillBuildOneBackupPath()
    {
        // Arrange
        var options = new SqlDatabaseFixtureOptions
        {
            DataDirectory = "/var/opt/mssql/data/",
            TemplateDatabaseName = "Template"
        };

        // Act & Assert
        Assert.Equal("/var/opt/mssql/data/Template.bak", options.BackupPath);
    }

    [Fact]
    public void RedisLease_WhenBuilt_ShouldPointTheConnectionStringAtItsDatabase()
    {
        // Arrange
        var lease = new RedisDatabaseLease("localhost:6379", 7);

        // Act & Assert
        Assert.Equal("localhost:6379,defaultDatabase=7", lease.ScenarioConnectionString);
    }

    [Fact]
    public void Settings_WhenUnset_ShouldQuietenTheHostLog()
    {
        // Arrange
        var configuration = new SettingsTestConfiguration();
        var settings = new Dictionary<string, string?>();

        // Act
        configuration.Configure(new ScenarioTestContext(NullLogger.Instance), settings);

        // Assert
        Assert.Equal("Warning", settings["Logging:LogLevel:Default"]);
    }

    [Fact]
    public void Settings_WhenGivenTheirOwn_ShouldApplyThoseInstead()
    {
        // Arrange
        var configuration = new SettingsTestConfiguration(new Dictionary<string, string?> { ["Feature:Enabled"] = "true" });
        var settings = new Dictionary<string, string?>();

        // Act
        configuration.Configure(new ScenarioTestContext(NullLogger.Instance), settings);

        // Assert
        Assert.Equal("true", settings["Feature:Enabled"]);
        Assert.DoesNotContain("Logging:LogLevel:Default", settings.Keys);
    }

    [Fact]
    public void Settings_WhenAKeyIsAlreadyThere_ShouldOverwriteIt()
    {
        // Arrange
        var configuration = new SettingsTestConfiguration(new Dictionary<string, string?> { ["Key"] = "mine" });
        var settings = new Dictionary<string, string?> { ["Key"] = "theirs" };

        // Act
        configuration.Configure(new ScenarioTestContext(NullLogger.Instance), settings);

        // Assert
        Assert.Equal("mine", settings["Key"]);
    }
}

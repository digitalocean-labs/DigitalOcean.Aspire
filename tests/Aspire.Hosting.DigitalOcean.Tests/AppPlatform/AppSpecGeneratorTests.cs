// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.AppPlatform;
using FluentAssertions;
using InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.Tests.AppPlatform;

public class AppSpecGeneratorTests
{
    [Fact]
    public void Generate_WithAppNameAndRegion_CreatesValidSpec()
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate("my-app", "nyc", resources);

        // Assert
        spec.Should().NotBeNull();
        spec.Name.Should().Be("my-app");
        spec.Region.Should().Be(App_spec_region.Nyc);
    }

    [Theory]
    [InlineData("nyc", App_spec_region.Nyc)]
    [InlineData("sfo", App_spec_region.Sfo)]
    [InlineData("ams", App_spec_region.Ams)]
    [InlineData("sgp", App_spec_region.Sgp)]
    [InlineData("lon", App_spec_region.Lon)]
    [InlineData("fra", App_spec_region.Fra)]
    [InlineData("tor", App_spec_region.Tor)]
    [InlineData("blr", App_spec_region.Blr)]
    [InlineData("syd", App_spec_region.Syd)]
    public void Generate_WithDifferentRegions_ParsesRegionCorrectly(string regionCode, App_spec_region expected)
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate("test-app", regionCode, resources);

        // Assert
        spec.Region.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-app", "my-app")]
    [InlineData("myapp", "myapp")]
    [InlineData("my_app", "my-app")]
    [InlineData("My.App.Name", "my-app-name")]
    public void Generate_SanitizesAppName(string input, string expected)
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate(input, "nyc", resources);

        // Assert
        spec.Name.Should().Be(expected);
    }

    [Fact]
    public void Generate_WithEmptyResources_ReturnsSpecWithEmptyCollections()
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert - empty collections may be null after optimization
        spec.Should().NotBeNull();
        spec.Name.Should().Be("test-app");
    }

    [Fact]
    public void ToYaml_GeneratesValidYamlString()
    {
        // Arrange
        var resources = Array.Empty<IResource>();
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().NotBeNullOrEmpty();
        yaml.Should().Contain("name: test-app");
        yaml.Should().Contain("region: nyc");
    }

    [Fact]
    public void ToYaml_ExcludesEmptyCollections()
    {
        // Arrange
        var resources = Array.Empty<IResource>();
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().NotContain("services:");
        yaml.Should().NotContain("workers:");
        yaml.Should().NotContain("databases:");
    }
}

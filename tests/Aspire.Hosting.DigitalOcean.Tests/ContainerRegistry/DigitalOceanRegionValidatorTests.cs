// Licensed under the MIT License.

using Aspire.Hosting.DigitalOcean;
using Aspire.Hosting.DigitalOcean.ContainerRegistry;
using FluentAssertions;

namespace Aspire.Hosting.DigitalOcean.Tests.ContainerRegistry;

public class DigitalOceanRegionValidatorTests
{
    [Theory]
    [InlineData("nyc1")]
    [InlineData("nyc2")]
    [InlineData("nyc3")]
    [InlineData("sfo1")]
    [InlineData("sfo2")]
    [InlineData("sfo3")]
    [InlineData("ams2")]
    [InlineData("ams3")]
    [InlineData("sgp1")]
    [InlineData("lon1")]
    [InlineData("fra1")]
    [InlineData("tor1")]
    [InlineData("blr1")]
    [InlineData("syd1")]
    public void ValidateRegion_WithKnownRegion_ReturnsTrue(string region)
    {
        // Act
        var result = DigitalOceanRegionValidator.ValidateRegion(region);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("NYC1")]
    [InlineData("Nyc1")]
    [InlineData("SFO1")]
    public void ValidateRegion_IsCaseInsensitive(string region)
    {
        // Act
        var result = DigitalOceanRegionValidator.ValidateRegion(region);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("us-east-1")]
    [InlineData("xyz123")]
    [InlineData("")]
    public void ValidateRegion_WithUnknownRegion_ReturnsFalse(string region)
    {
        // Act
        var result = DigitalOceanRegionValidator.ValidateRegion(region);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetKnownRegions_ReturnsAllRegions()
    {
        // Act
        var regions = DigitalOceanRegionValidator.GetKnownRegions();

        // Assert
        regions.Should().NotBeEmpty();
        regions.Should().Contain("nyc1");
        regions.Should().Contain("sfo1");
        regions.Should().Contain("ams2");
    }
}

public class DigitalOceanRegionsTests
{
    [Fact]
    public void RegionConstants_HaveCorrectValues()
    {
        DigitalOceanRegions.NYC1.Should().Be("nyc1");
        DigitalOceanRegions.NYC2.Should().Be("nyc2");
        DigitalOceanRegions.NYC3.Should().Be("nyc3");
        DigitalOceanRegions.SFO1.Should().Be("sfo1");
        DigitalOceanRegions.SFO2.Should().Be("sfo2");
        DigitalOceanRegions.SFO3.Should().Be("sfo3");
        DigitalOceanRegions.AMS2.Should().Be("ams2");
        DigitalOceanRegions.AMS3.Should().Be("ams3");
        DigitalOceanRegions.SGP1.Should().Be("sgp1");
        DigitalOceanRegions.LON1.Should().Be("lon1");
        DigitalOceanRegions.FRA1.Should().Be("fra1");
        DigitalOceanRegions.TOR1.Should().Be("tor1");
        DigitalOceanRegions.BLR1.Should().Be("blr1");
        DigitalOceanRegions.SYD1.Should().Be("syd1");
    }

    [Theory]
    [InlineData(DigitalOceanRegions.NYC1)]
    [InlineData(DigitalOceanRegions.SFO1)]
    [InlineData(DigitalOceanRegions.AMS2)]
    [InlineData(DigitalOceanRegions.SGP1)]
    [InlineData(DigitalOceanRegions.LON1)]
    [InlineData(DigitalOceanRegions.FRA1)]
    [InlineData(DigitalOceanRegions.TOR1)]
    [InlineData(DigitalOceanRegions.BLR1)]
    [InlineData(DigitalOceanRegions.SYD1)]
    public void RegionConstants_AreValidRegions(string region)
    {
        // Act
        var isValid = DigitalOceanRegionValidator.ValidateRegion(region);

        // Assert
        isValid.Should().BeTrue();
    }
}

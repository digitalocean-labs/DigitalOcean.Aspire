// Licensed under the MIT License.

namespace Aspire.Hosting.DigitalOcean.ContainerRegistry;

/// <summary>
/// Validates DigitalOcean regions.
/// </summary>
public static class DigitalOceanRegionValidator
{
    // Known valid regions as of 2025
    private static readonly HashSet<string> KnownRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        "nyc1", "nyc2", "nyc3",
        "sfo1", "sfo2", "sfo3",
        "ams2", "ams3",
        "sgp1",
        "lon1",
        "fra1",
        "tor1",
        "blr1",
        "syd1"
    };

    /// <summary>
    /// Validates that a region slug is a known valid region.
    /// </summary>
    /// <param name="region">The region slug to validate.</param>
    /// <returns>True if the region is known to be valid.</returns>
    public static bool ValidateRegion(string region)
    {
        return KnownRegions.Contains(region);
    }

    /// <summary>
    /// Gets the list of known valid regions.
    /// </summary>
    /// <returns>A list of known region slugs.</returns>
    public static IReadOnlyList<string> GetKnownRegions()
    {
        return KnownRegions.ToList();
    }
}

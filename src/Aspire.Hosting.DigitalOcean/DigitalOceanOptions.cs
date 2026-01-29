// Licensed under the MIT License.

namespace Aspire.Hosting.DigitalOcean;

/// <summary>
/// Configuration options for DigitalOcean integration.
/// </summary>
public class DigitalOceanOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "DigitalOcean";

    /// <summary>
    /// Gets or sets the DigitalOcean API token.
    /// Can also be set via the <c>DigitalOcean__ApiToken</c> environment variable.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Gets or sets the default region for resources.
    /// Use <see cref="DigitalOceanRegions"/> constants or any valid region slug.
    /// </summary>
    public string? Region { get; set; }
}

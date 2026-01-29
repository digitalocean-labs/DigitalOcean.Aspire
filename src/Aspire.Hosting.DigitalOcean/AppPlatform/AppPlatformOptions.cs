// Licensed under the MIT License.

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Configuration options for DigitalOcean App Platform deployment.
/// </summary>
public class AppPlatformOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "DigitalOcean:AppPlatform";

    /// <summary>
    /// Gets or sets the App Platform app name.
    /// If not specified, the app name will be derived from the Aspire app model.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the region where the app will be deployed.
    /// Use <see cref="DigitalOceanRegions"/> constants or any valid region slug.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the default instance size slug for services.
    /// Examples: "apps-s-1vcpu-0.5gb", "apps-s-1vcpu-1gb", "apps-s-2vcpu-4gb".
    /// </summary>
    public string? InstanceSizeSlug { get; set; }

    /// <summary>
    /// Gets or sets the default instance count for services.
    /// </summary>
    public int? InstanceCount { get; set; }
}

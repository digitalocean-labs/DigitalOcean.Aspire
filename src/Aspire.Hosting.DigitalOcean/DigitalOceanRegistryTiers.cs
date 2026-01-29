// Licensed under the MIT License.

namespace Aspire.Hosting.DigitalOcean;

/// <summary>
/// Well-known DigitalOcean Container Registry subscription tier slugs.
/// </summary>
public static class DigitalOceanRegistryTiers
{
    /// <summary>
    /// Starter tier - 500 MB storage, free.
    /// </summary>
    public const string Starter = "starter";

    /// <summary>
    /// Basic tier - 5 GB storage.
    /// </summary>
    public const string Basic = "basic";

    /// <summary>
    /// Professional tier - 50 GB storage.
    /// </summary>
    public const string Professional = "professional";
}

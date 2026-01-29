// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DigitalOcean.ContainerRegistry;

/// <summary>
/// Represents a DigitalOcean Container Registry (DOCR) resource.
/// Extends <see cref="ContainerRegistryResource"/> with DOCR-specific configuration.
/// </summary>
public class DigitalOceanContainerRegistryResource : ContainerRegistryResource
{
    /// <summary>
    /// Creates a new DOCR resource with the specified name and registry name.
    /// </summary>
    /// <param name="name">The resource name in the Aspire app model.</param>
    /// <param name="registryName">The DOCR registry name (used as the repository path).</param>
    public DigitalOceanContainerRegistryResource(string name, string registryName)
        : base(
            name,
            endpoint: ReferenceExpression.Create($"registry.digitalocean.com"),
            repository: ReferenceExpression.Create($"{registryName}"))
    {
        RegistryName = registryName;
    }

    /// <summary>
    /// Creates a new DOCR resource with a parameterized registry name.
    /// </summary>
    /// <param name="name">The resource name in the Aspire app model.</param>
    /// <param name="registryNameExpression">A reference expression for the registry name.</param>
    public DigitalOceanContainerRegistryResource(string name, ReferenceExpression registryNameExpression)
        : base(
            name,
            endpoint: ReferenceExpression.Create($"registry.digitalocean.com"),
            repository: registryNameExpression)
    {
    }

    /// <summary>
    /// Gets the DOCR registry name.
    /// </summary>
    public string? RegistryName { get; }

    /// <summary>
    /// Gets or sets the subscription tier (e.g., "starter", "basic", "professional").
    /// Use <see cref="DigitalOceanRegistryTiers"/> constants or any valid tier slug.
    /// </summary>
    public string Tier { get; set; } = DigitalOceanRegistryTiers.Starter;

    /// <summary>
    /// Gets or sets whether this references an existing registry (won't be provisioned).
    /// </summary>
    public bool IsExisting { get; internal set; }
}

/// <summary>
/// Annotation that attaches a DigitalOcean API token to a resource.
/// </summary>
/// <param name="tokenParameter">The parameter resource containing the API token.</param>
public sealed class DigitalOceanApiTokenAnnotation(ParameterResource tokenParameter) : IResourceAnnotation
{
    /// <summary>
    /// Gets the parameter resource containing the DigitalOcean API token.
    /// </summary>
    public ParameterResource TokenParameter { get; } = tokenParameter;
}

/// <summary>
/// Annotation that specifies the DigitalOcean region for a resource.
/// </summary>
/// <param name="region">The region slug (e.g., "nyc3"). Use <see cref="DigitalOceanRegions"/> constants.</param>
public sealed class DigitalOceanRegionAnnotation(string region) : IResourceAnnotation
{
    /// <summary>
    /// Gets the DigitalOcean region slug.
    /// </summary>
    public string Region { get; } = region;
}

/// <summary>
/// Extension methods for querying DigitalOcean annotations from resources.
/// </summary>
public static class DigitalOceanAnnotationExtensions
{
    /// <summary>
    /// Gets the DigitalOcean API token annotation from a resource, if present.
    /// </summary>
    /// <param name="resource">The resource to query.</param>
    /// <returns>The token annotation, or null if not present.</returns>
    public static DigitalOceanApiTokenAnnotation? GetDigitalOceanTokenAnnotation(this IResource resource)
    {
        return resource.Annotations.OfType<DigitalOceanApiTokenAnnotation>().FirstOrDefault();
    }

    /// <summary>
    /// Gets the DigitalOcean API token parameter from a resource, if present.
    /// </summary>
    /// <param name="resource">The resource to query.</param>
    /// <returns>The token parameter, or null if not present.</returns>
    public static ParameterResource? GetDigitalOceanTokenParameter(this IResource resource)
    {
        return resource.GetDigitalOceanTokenAnnotation()?.TokenParameter;
    }

    /// <summary>
    /// Gets the DigitalOcean region annotation from a resource, if present.
    /// </summary>
    /// <param name="resource">The resource to query.</param>
    /// <returns>The region annotation, or null if not present.</returns>
    public static DigitalOceanRegionAnnotation? GetDigitalOceanRegionAnnotation(this IResource resource)
    {
        return resource.Annotations.OfType<DigitalOceanRegionAnnotation>().FirstOrDefault();
    }

    /// <summary>
    /// Gets the DigitalOcean region slug from a resource, if present.
    /// </summary>
    /// <param name="resource">The resource to query.</param>
    /// <returns>The region slug, or null if not present.</returns>
    public static string? GetDigitalOceanRegion(this IResource resource)
    {
        return resource.GetDigitalOceanRegionAnnotation()?.Region;
    }
}

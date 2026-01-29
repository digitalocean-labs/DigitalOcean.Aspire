// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DigitalOcean.ContainerRegistry;

/// <summary>
/// Extension methods for adding DigitalOcean Container Registry resources.
/// </summary>
public static class DigitalOceanContainerRegistryExtensions
{
    /// <summary>
    /// The default parameter name for the DigitalOcean API token.
    /// </summary>
    public const string DefaultTokenParameterName = "digitalOceanToken";

    /// <summary>
    /// Adds a DigitalOcean Container Registry resource with a literal registry name.
    /// Automatically creates a secret parameter for the API token (from "Parameters:digitalOceanToken").
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name in the Aspire app model.</param>
    /// <param name="registryName">The DOCR registry name.</param>
    /// <returns>A resource builder for the DOCR resource.</returns>
    public static IResourceBuilder<DigitalOceanContainerRegistryResource> AddDigitalOceanContainerRegistry(
        this IDistributedApplicationBuilder builder,
        string name,
        string registryName)
    {
        var resource = new DigitalOceanContainerRegistryResource(name, registryName);
        
        // Add the resource first to get the builder
        var resourceBuilder = builder.AddResource(resource);
        
        // Automatically add default token parameter and attach it
        var tokenParameter = builder.AddParameter(DefaultTokenParameterName, secret: true);
        resourceBuilder.WithToken(tokenParameter);
        
        return resourceBuilder;
    }

    /// <summary>
    /// Adds a DigitalOcean Container Registry resource with a parameterized registry name.
    /// Automatically creates a secret parameter for the API token (from "Parameters:digitalOceanToken").
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name in the Aspire app model.</param>
    /// <param name="registryNameParameter">A parameter resource containing the registry name.</param>
    /// <returns>A resource builder for the DOCR resource.</returns>
    public static IResourceBuilder<DigitalOceanContainerRegistryResource> AddDigitalOceanContainerRegistry(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> registryNameParameter)
    {
        var resource = new DigitalOceanContainerRegistryResource(
            name,
            ReferenceExpression.Create($"{registryNameParameter.Resource}"));
        
        // Add the resource first to get the builder
        var resourceBuilder = builder.AddResource(resource);
        
        // Automatically add default token parameter and attach it
        var tokenParameter = builder.AddParameter(DefaultTokenParameterName, secret: true);
        resourceBuilder.WithToken(tokenParameter);
        
        return resourceBuilder;
    }

    /// <summary>
    /// Marks this registry as referencing an existing DOCR (won't be provisioned).
    /// </summary>
    /// <param name="builder">The DOCR resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<DigitalOceanContainerRegistryResource> RunAsExisting(
        this IResourceBuilder<DigitalOceanContainerRegistryResource> builder)
    {
        builder.Resource.IsExisting = true;
        return builder;
    }

    /// <summary>
    /// Configures the DigitalOcean region for this resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="region">The region slug (e.g., "nyc3"). Use <see cref="DigitalOceanRegions"/> constants.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var docr = builder.AddDigitalOceanContainerRegistry("docr", "my-registry")
    ///     .WithRegion(DigitalOceanRegions.NYC3);
    /// </code>
    /// </example>
    public static IResourceBuilder<DigitalOceanContainerRegistryResource> WithRegion(
        this IResourceBuilder<DigitalOceanContainerRegistryResource> builder,
        string region)
    {
        var annotation = new DigitalOceanRegionAnnotation(region);
        return builder.WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Sets the subscription tier for the DOCR.
    /// </summary>
    /// <param name="builder">The DOCR resource builder.</param>
    /// <param name="tier">The tier slug (e.g., "starter"). Use <see cref="DigitalOceanRegistryTiers"/> constants.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<DigitalOceanContainerRegistryResource> WithTier(
        this IResourceBuilder<DigitalOceanContainerRegistryResource> builder,
        string tier)
    {
        builder.Resource.Tier = tier;
        return builder;
    }

    /// <summary>
    /// Configures the DigitalOcean API token for authenticating with DigitalOcean services.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="tokenParameter">A secret parameter containing the DigitalOcean API token.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var customToken = builder.AddParameter("myDoToken", secret: true);
    /// var docr = builder.AddDigitalOceanContainerRegistry("docr", "my-registry")
    ///     .WithToken(customToken);
    /// </code>
    /// </example>
    public static IResourceBuilder<DigitalOceanContainerRegistryResource> WithToken(
        this IResourceBuilder<DigitalOceanContainerRegistryResource> builder,
        IResourceBuilder<ParameterResource> tokenParameter)
    {
        var annotation = new DigitalOceanApiTokenAnnotation(tokenParameter.Resource);
        
        return builder
            .WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Replace)
            .WithReferenceRelationship(tokenParameter);
    }
}

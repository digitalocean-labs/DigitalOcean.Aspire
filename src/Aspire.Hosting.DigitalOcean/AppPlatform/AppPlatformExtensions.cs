// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.ContainerRegistry;
using Aspire.Hosting.Docker;

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Extension methods for DigitalOcean App Platform deployment.
/// </summary>
public static class AppPlatformExtensions
{
    /// <summary>
    /// Adds App Platform deployment support to a Docker Compose environment.
    /// </summary>
    /// <param name="builder">The Docker Compose environment resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddDockerComposeEnvironment("do-app")
    ///     .WithAppPlatformDeploySupport();
    /// </code>
    /// </example>
    public static IResourceBuilder<DockerComposeEnvironmentResource> WithAppPlatformDeploySupport(
        this IResourceBuilder<DockerComposeEnvironmentResource> builder)
    {
        // Add annotation to indicate this environment supports App Platform deployment
        builder.WithAnnotation(new AppPlatformDeploymentAnnotation());
        return builder;
    }

    /// <summary>
    /// Attaches a DigitalOcean Container Registry to the environment for image storage.
    /// </summary>
    /// <param name="builder">The Docker Compose environment resource builder.</param>
    /// <param name="registry">The DOCR resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var docr = builder.AddDigitalOceanContainerRegistry("docr", "my-registry");
    /// builder.AddDockerComposeEnvironment("do-app")
    ///     .WithDigitalOceanContainerRegistry(docr)
    ///     .WithAppPlatformDeploySupport();
    /// </code>
    /// </example>
    public static IResourceBuilder<DockerComposeEnvironmentResource> WithDigitalOceanContainerRegistry(
        this IResourceBuilder<DockerComposeEnvironmentResource> builder,
        IResourceBuilder<DigitalOceanContainerRegistryResource> registry)
    {
        builder.WithAnnotation(new DigitalOceanContainerRegistryAnnotation(registry.Resource));
        return builder;
    }

    /// <summary>
    /// Configures a project resource for source-based deployment from GitHub.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="repository">The GitHub repository in "owner/repo" format.</param>
    /// <param name="branch">The branch to deploy from (default: "main").</param>
    /// <param name="deployOnPush">Whether to auto-deploy on push (default: true).</param>
    /// <param name="sourceDir">Optional source directory within the repository.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///     .WithGitHubSource("myorg/myrepo", branch: "main");
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithGitHubSource<T>(
        this IResourceBuilder<T> builder,
        string repository,
        string branch = "main",
        bool deployOnPush = true,
        string? sourceDir = null) where T : IResource
    {
        var config = new GitHubSourceConfig
        {
            Repository = repository,
            Branch = branch,
            DeployOnPush = deployOnPush,
            SourceDir = sourceDir
        };

        builder.WithAnnotation(new GitHubSourceAnnotation(config));
        return builder;
    }

    /// <summary>
    /// Sets the App Platform region for deployment.
    /// </summary>
    /// <param name="builder">The Docker Compose environment resource builder.</param>
    /// <param name="region">The region slug (e.g., "nyc"). Use <see cref="DigitalOceanRegions"/> constants.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> WithRegion(
        this IResourceBuilder<DockerComposeEnvironmentResource> builder,
        string region)
    {
        builder.WithAnnotation(new AppPlatformRegionAnnotation(region));
        return builder;
    }

    /// <summary>
    /// Sets the App Platform app name.
    /// </summary>
    /// <param name="builder">The Docker Compose environment resource builder.</param>
    /// <param name="appName">The app name for App Platform.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> WithAppName(
        this IResourceBuilder<DockerComposeEnvironmentResource> builder,
        string appName)
    {
        builder.WithAnnotation(new AppPlatformAppNameAnnotation(appName));
        return builder;
    }
}

/// <summary>
/// Annotation indicating App Platform deployment support is enabled.
/// </summary>
internal sealed class AppPlatformDeploymentAnnotation : IResourceAnnotation
{
}

/// <summary>
/// Annotation containing the DOCR resource for the environment.
/// </summary>
internal sealed class DigitalOceanContainerRegistryAnnotation(DigitalOceanContainerRegistryResource registry) 
    : IResourceAnnotation
{
    public DigitalOceanContainerRegistryResource Registry { get; } = registry;
}

/// <summary>
/// Annotation containing GitHub source configuration for a resource.
/// </summary>
internal sealed class GitHubSourceAnnotation(GitHubSourceConfig config) : IResourceAnnotation
{
    public GitHubSourceConfig Config { get; } = config;
}

/// <summary>
/// Annotation containing the App Platform region.
/// </summary>
internal sealed class AppPlatformRegionAnnotation(string region) : IResourceAnnotation
{
    public string Region { get; } = region;
}

/// <summary>
/// Annotation containing the App Platform app name.
/// </summary>
internal sealed class AppPlatformAppNameAnnotation(string appName) : IResourceAnnotation
{
    public string AppName { get; } = appName;
}

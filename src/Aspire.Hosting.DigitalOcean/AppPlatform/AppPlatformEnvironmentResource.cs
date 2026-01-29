// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.ContainerRegistry;

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Represents a DigitalOcean App Platform deployment environment.
/// </summary>
public class AppPlatformEnvironmentResource : Resource
{
    /// <summary>
    /// Creates a new App Platform environment resource.
    /// </summary>
    /// <param name="name">The resource name.</param>
    public AppPlatformEnvironmentResource(string name) : base(name)
    {
    }

    /// <summary>
    /// Gets or sets the App Platform app name.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the region where the app will be deployed.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the container registry to use for images.
    /// </summary>
    public DigitalOceanContainerRegistryResource? ContainerRegistry { get; set; }

    /// <summary>
    /// Gets the resources that will be deployed to App Platform.
    /// </summary>
    public List<IResource> DeployedResources { get; } = [];

    /// <summary>
    /// Gets the GitHub source mappings for source-based deployments.
    /// </summary>
    public Dictionary<IResource, GitHubSourceConfig> GitHubSources { get; } = [];
}

/// <summary>
/// Configuration for GitHub source-based deployment.
/// </summary>
public class GitHubSourceConfig
{
    /// <summary>
    /// Gets or sets the GitHub repository in "owner/repo" format.
    /// </summary>
    public required string Repository { get; set; }

    /// <summary>
    /// Gets or sets the branch to deploy from.
    /// </summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// Gets or sets whether to auto-deploy on push.
    /// </summary>
    public bool DeployOnPush { get; set; } = true;

    /// <summary>
    /// Gets or sets the source directory within the repository.
    /// </summary>
    public string? SourceDir { get; set; }
}

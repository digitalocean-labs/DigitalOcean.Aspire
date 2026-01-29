// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.ContainerRegistry;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using DOModels = InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Extension methods for DigitalOcean App Platform deployment.
/// </summary>
public static class AppPlatformExtensions
{
    private static bool _appSpecPublisherResourceAdded;

    /// <summary>
    /// Adds the DigitalOcean App Platform publisher to the distributed application.
    /// This enables publishing resources as an App Platform app spec when running <c>aspire publish</c>.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Optional name for the App Platform publisher resource.</param>
    /// <returns>The resource builder for the App Platform publisher.</returns>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// builder.AddAppPlatformPublisher();
    /// 
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///     .PublishAsAppSpec();
    /// </code>
    /// </example>
    public static IResourceBuilder<AppPlatformPublisherResource> AddAppPlatformPublisher(
        this IDistributedApplicationBuilder builder,
        string name = "appplatform-publisher")
    {
        // Prevent duplicate registration
        if (_appSpecPublisherResourceAdded)
        {
            // Return existing resource builder
            var existingResource = builder.Resources.OfType<AppPlatformPublisherResource>().FirstOrDefault();
            if (existingResource is not null)
            {
                return builder.CreateResourceBuilder(existingResource);
            }
        }
        _appSpecPublisherResourceAdded = true;

        // Configure options
        builder.Services.Configure<AppPlatformOptions>(
            builder.Configuration.GetSection(AppPlatformOptions.SectionName));

        // Subscribe to AfterPublishEvent to generate the App Spec
        builder.Eventing.Subscribe<AfterPublishEvent>(
            async (@event, cancellationToken) =>
            {
                await AppPlatformPublisherResource.GenerateAppSpecAsync(@event, cancellationToken);
            });

        var resource = new AppPlatformPublisherResource(name);
        return builder.AddResource(resource)
            .ExcludeFromManifest();
    }

    /// <summary>
    /// Marks a resource for publishing as part of the DigitalOcean App Platform app spec.
    /// When <c>aspire publish</c> is run, this resource will be included in the generated app-spec.yaml.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">Optional callback to configure the app spec entry for this resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///     .WithExternalHttpEndpoints()
    ///     .PublishAsAppSpec();
    /// </code>
    /// </example>
    public static IResourceBuilder<T> PublishAsAppSpec<T>(
        this IResourceBuilder<T> builder,
        Action<T, DOModels.App_service_spec>? configure = null) where T : IResource
    {
        // Ensure the publisher resource is added
        builder.ApplicationBuilder.AddAppPlatformPublisher();
        
        var annotation = new AppSpecPublishAnnotation();
        if (configure is not null)
        {
            annotation.ConfigureServiceCallback = (resource, spec) => configure((T)resource, spec);
        }
        
        builder.WithAnnotation(annotation);
        return builder;
    }

    /// <summary>
    /// Marks a resource for deployment as a DigitalOcean App Platform service.
    /// Services are HTTP-based components that handle web traffic.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">Optional callback to configure the App Platform service spec using the InfinityFlow model.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///     .WithExternalHttpEndpoints()
    ///     .PublishAsAppService(service =>
    ///     {
    ///         service.Instance_count = 2;
    ///         service.Instance_size_slug = "apps-s-1vcpu-1gb";
    ///         service.Http_port = 8080;
    ///     });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> PublishAsAppService<T>(
        this IResourceBuilder<T> builder,
        Action<DOModels.App_service_spec>? configure = null) where T : IResource
    {
        // Ensure the publisher resource is added
        builder.ApplicationBuilder.AddAppPlatformPublisher();
        
        var spec = new DOModels.App_service_spec();
        configure?.Invoke(spec);
        
        var annotation = new AppServiceAnnotation(spec);
        builder.WithAnnotation(annotation);
        
        // Also add the AppSpecPublishAnnotation for backward compatibility
        builder.WithAnnotation(new AppSpecPublishAnnotation());
        
        return builder;
    }

    /// <summary>
    /// Marks a resource for deployment as a DigitalOcean App Platform worker.
    /// Workers are background processes that don't handle HTTP traffic.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">Optional callback to configure the App Platform worker spec using the InfinityFlow model.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.MyWorker&gt;("worker")
    ///     .PublishAsAppWorker(worker =>
    ///     {
    ///         worker.Instance_count = 1;
    ///         worker.Instance_size_slug = "apps-s-1vcpu-0.5gb";
    ///     });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> PublishAsAppWorker<T>(
        this IResourceBuilder<T> builder,
        Action<DOModels.App_worker_spec>? configure = null) where T : IResource
    {
        // Ensure the publisher resource is added
        builder.ApplicationBuilder.AddAppPlatformPublisher();
        
        var spec = new DOModels.App_worker_spec();
        configure?.Invoke(spec);
        
        var annotation = new AppWorkerAnnotation(spec);
        builder.WithAnnotation(annotation);
        
        // Also add the AppSpecPublishAnnotation for backward compatibility
        builder.WithAnnotation(new AppSpecPublishAnnotation());
        
        return builder;
    }

    /// <summary>
    /// Configures the resource to be published as a container image to DOCR (DigitalOcean Container Registry).
    /// When this is not called, the resource will use source-based deployment from the current git repository.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">Optional callback to configure the container image settings.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///     .PublishAsAppService()
    ///     .WithContainerImage(image =>
    ///     {
    ///         image.Tag = "v1.0.0";
    ///     });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithContainerImage<T>(
        this IResourceBuilder<T> builder,
        Action<PublishAsContainerImageAnnotation>? configure = null) where T : IResource
    {
        var annotation = new PublishAsContainerImageAnnotation();
        configure?.Invoke(annotation);
        
        builder.WithAnnotation(annotation);
        return builder;
    }

    /// <summary>
    /// Adds App Platform deployment support to the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="appName">The name of the App Platform application.</param>
    /// <param name="region">Optional region for deployment (e.g., "nyc", "sfo", "ams"). Defaults to "nyc".</param>
    /// <param name="configureAppSpec">Optional callback to configure the entire App Platform app spec using the InfinityFlow model.</param>
    /// <returns>The distributed application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// builder.WithAppPlatformDeploySupport("my-awesome-app", region: "sfo", configureAppSpec: spec =>
    /// {
    ///     spec.Features = [new() { Name = "buildpack-stack", Value = "ubuntu-22" }];
    /// });
    /// 
    /// builder.AddProject&lt;Projects.MyApi&gt;("api")
    ///     .PublishAsAppService();
    /// </code>
    /// </example>
    public static IDistributedApplicationBuilder WithAppPlatformDeploySupport(
        this IDistributedApplicationBuilder builder,
        string? appName = null,
        string? region = null,
        Action<DOModels.App_spec>? configureAppSpec = null)
    {
        var publisherBuilder = builder.AddAppPlatformPublisher();
        
        // Add app name and region annotations to the publisher resource
        if (appName is not null)
        {
            publisherBuilder.WithAnnotation(new AppPlatformAppNameAnnotation(appName));
        }
        if (region is not null)
        {
            publisherBuilder.WithAnnotation(new AppPlatformRegionAnnotation(region));
        }
        
        // Add app spec configuration annotation if provided
        if (configureAppSpec is not null)
        {
            publisherBuilder.WithAnnotation(new AppSpecConfigurationAnnotation(configureAppSpec));
        }
        
        return builder;
    }

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

/// <summary>
/// Annotation marking a resource as an App Platform service.
/// </summary>
public sealed class AppServiceAnnotation(DOModels.App_service_spec serviceSpec) : IResourceAnnotation
{
    /// <summary>
    /// Gets the service spec from InfinityFlow.DigitalOcean.Client.
    /// </summary>
    public DOModels.App_service_spec ServiceSpec { get; } = serviceSpec;
}

/// <summary>
/// Annotation marking a resource as an App Platform worker.
/// </summary>
public sealed class AppWorkerAnnotation(DOModels.App_worker_spec workerSpec) : IResourceAnnotation
{
    /// <summary>
    /// Gets the worker spec from InfinityFlow.DigitalOcean.Client.
    /// </summary>
    public DOModels.App_worker_spec WorkerSpec { get; } = workerSpec;
}

/// <summary>
/// Annotation containing the App Spec configuration callback.
/// </summary>
internal sealed class AppSpecConfigurationAnnotation(Action<DOModels.App_spec> configure) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback to configure the App Spec.
    /// </summary>
    public Action<DOModels.App_spec> Configure { get; } = configure;
}

/// <summary>
/// Annotation indicating that a resource should be published as a container image to DOCR.
/// When this annotation is present, the resource will use DOCR for deployment.
/// When absent, the resource will use source-based deployment from the current git repository.
/// </summary>
public sealed class PublishAsContainerImageAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the container registry name (for DOCR).
    /// If not set, uses the registry from the AppPlatform configuration.
    /// </summary>
    public string? RegistryName { get; set; }

    /// <summary>
    /// Gets or sets the image name. If not set, uses the resource name.
    /// </summary>
    public string? ImageName { get; set; }

    /// <summary>
    /// Gets or sets the image tag. Defaults to "latest".
    /// </summary>
    public string Tag { get; set; } = "latest";
}

/// <summary>
/// Annotation containing detected git repository information for source-based deployment.
/// </summary>
internal sealed class GitRepoInfoAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the GitHub repository in "owner/repo" format.
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// Gets or sets the source directory relative to the repository root.
    /// </summary>
    public string? SourceDir { get; set; }
}

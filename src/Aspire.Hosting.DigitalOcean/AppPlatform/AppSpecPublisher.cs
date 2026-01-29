// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using DOModels = InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Resource that generates DigitalOcean App Platform app specs during publishing.
/// </summary>
public class AppPlatformPublisherResource : Aspire.Hosting.ApplicationModel.Resource
{
    /// <summary>
    /// Creates a new App Platform publisher resource.
    /// </summary>
    /// <param name="name">The resource name.</param>
    public AppPlatformPublisherResource(string name) : base(name)
    {
        // Don't add ManifestPublishingCallbackAnnotation here as it conflicts with ExcludeFromManifest
    }

    /// <summary>
    /// Generates the App Spec after publishing completes.
    /// This is called via the AfterPublishEvent subscription.
    /// </summary>
    internal static async Task GenerateAppSpecAsync(
        AfterPublishEvent publishEvent,
        CancellationToken cancellationToken)
    {
        var model = publishEvent.Model;
        
        // Find resources marked for App Spec publishing
        var resourcesToPublish = model.Resources
            .Where(r => r.TryGetAnnotationsOfType<AppSpecPublishAnnotation>(out _))
            .ToList();

        if (resourcesToPublish.Count == 0)
        {
            return;
        }

        // Get the publisher resource for configuration
        var publisherResource = model.Resources.OfType<AppPlatformPublisherResource>().FirstOrDefault();

        // Get app name and region
        var appName = GetAppName(model, publisherResource);
        var region = GetRegion(model, publisherResource);
        var registryName = GetRegistryName(model);

        // Detect git repository info for source-based deployment
        var gitInfo = AppSpecGenerator.DetectGitInfo(Directory.GetCurrentDirectory());

        // Generate the app spec using InfinityFlow.DigitalOcean.Client models
        var spec = AppSpecGenerator.Generate(appName, region, resourcesToPublish, registryName, gitInfo);

        // Apply any app-level configuration callback
        if (publisherResource?.TryGetAnnotationsOfType<AppSpecConfigurationAnnotation>(out var configAnnotations) == true)
        {
            configAnnotations.First().Configure(spec);
        }

        var yaml = AppSpecGenerator.ToYaml(spec);

        // Get the output path from pipeline options
#pragma warning disable ASPIREPIPELINES001 // PipelineOptions is experimental
        var pipelineOptions = publishEvent.Services.GetService<Microsoft.Extensions.Options.IOptions<Aspire.Hosting.Pipelines.PipelineOptions>>();
#pragma warning restore ASPIREPIPELINES001
        var outputDir = pipelineOptions?.Value?.OutputPath ?? Path.Combine(Directory.GetCurrentDirectory(), "aspire-output");
        Directory.CreateDirectory(outputDir);
        
        var appSpecPath = Path.Combine(outputDir, "app-spec.yaml");
        await File.WriteAllTextAsync(appSpecPath, yaml, cancellationToken);

        // Write the deploy script
        var doctlPath = Path.Combine(outputDir, "deploy-appplatform.sh");
        var sanitizedAppName = AppSpecGenerator.SanitizeName(appName);
        var doctlScript = $$"""
            #!/bin/bash
            # Deploy to DigitalOcean App Platform
            # Requires: doctl CLI (https://docs.digitalocean.com/reference/doctl/how-to/install/)
            
            set -e
            
            SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
            cd "$SCRIPT_DIR"
            
            APP_NAME="{{sanitizedAppName}}"
            
            echo "Deploying '$APP_NAME' to DigitalOcean App Platform..."
            
            # Check if app exists
            if doctl apps list --format Name --no-header 2>/dev/null | grep -q "^$APP_NAME$"; then
                echo "Updating existing app '$APP_NAME'..."
                APP_ID=$(doctl apps list --format ID,Name --no-header | grep "$APP_NAME" | awk '{print $1}' | head -1)
                doctl apps update "$APP_ID" --spec app-spec.yaml
            else
                echo "Creating new app '$APP_NAME'..."
                doctl apps create --spec app-spec.yaml
            fi
            
            echo ""
            echo "Deployment initiated. Check status at https://cloud.digitalocean.com/apps"
            """;
        
        await File.WriteAllTextAsync(doctlPath, doctlScript, cancellationToken);

        // Make script executable on Unix
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(doctlPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                           UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                           UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    private static string GetAppName(DistributedApplicationModel model, AppPlatformPublisherResource? publisherResource)
    {
        // Check for AppPlatformAppNameAnnotation on the publisher resource first
        if (publisherResource?.TryGetAnnotationsOfType<AppPlatformAppNameAnnotation>(out var publisherAnnotations) == true)
        {
            return publisherAnnotations.First().AppName;
        }

        // Check for AppPlatformAppNameAnnotation on any other resource
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<AppPlatformAppNameAnnotation>(out var annotations))
            {
                return annotations.First().AppName;
            }
        }

        // Default to a sanitized app name
        return "aspire-app";
    }

    private static string GetRegion(DistributedApplicationModel model, AppPlatformPublisherResource? publisherResource)
    {
        // Check for AppPlatformRegionAnnotation on the publisher resource first
        if (publisherResource?.TryGetAnnotationsOfType<AppPlatformRegionAnnotation>(out var publisherAnnotations) == true)
        {
            return publisherAnnotations.First().Region;
        }

        // Check for AppPlatformRegionAnnotation on any other resource
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<AppPlatformRegionAnnotation>(out var annotations))
            {
                return annotations.First().Region;
            }
        }

        // Default to NYC
        return "nyc";
    }

    private static string? GetRegistryName(DistributedApplicationModel model)
    {
        // Check for DigitalOceanContainerRegistryAnnotation
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<DigitalOceanContainerRegistryAnnotation>(out var annotations))
            {
                return annotations.First().Registry.RegistryName;
            }
        }

        return null;
    }
}

/// <summary>
/// Annotation indicating a resource should be included in App Spec publishing.
/// </summary>
public sealed class AppSpecPublishAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the optional configuration callback for service specs.
    /// </summary>
    public Action<IResource, DOModels.App_service_spec>? ConfigureServiceCallback { get; set; }
}

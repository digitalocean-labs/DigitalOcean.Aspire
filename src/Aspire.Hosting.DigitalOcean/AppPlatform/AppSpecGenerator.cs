// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Generates DigitalOcean App Platform app specs from Aspire resources.
/// </summary>
public static class AppSpecGenerator
{
    /// <summary>
    /// Generates an App Spec from the given Aspire resources.
    /// </summary>
    /// <param name="appName">The app name.</param>
    /// <param name="region">The deployment region.</param>
    /// <param name="resources">The Aspire resources to include.</param>
    /// <param name="registryName">Optional DOCR registry name for container images.</param>
    /// <returns>The generated app spec as a dictionary.</returns>
    public static Dictionary<string, object> Generate(
        string appName,
        string region,
        IEnumerable<IResource> resources,
        string? registryName = null)
    {
        var spec = new Dictionary<string, object>
        {
            ["name"] = appName,
            ["region"] = region
        };

        var services = new List<Dictionary<string, object>>();
        var workers = new List<Dictionary<string, object>>();
        var databases = new List<Dictionary<string, object>>();

        foreach (var resource in resources)
        {
            switch (resource)
            {
                case ProjectResource project:
                    var projectSpec = GenerateProjectSpec(project, registryName);
                    if (IsHttpService(project))
                    {
                        services.Add(projectSpec);
                    }
                    else
                    {
                        workers.Add(projectSpec);
                    }
                    break;

                case ContainerResource container:
                    var containerSpec = GenerateContainerSpec(container);
                    if (IsHttpService(container))
                    {
                        services.Add(containerSpec);
                    }
                    else
                    {
                        workers.Add(containerSpec);
                    }
                    break;

                // Database resources
                case IResource r when IsPostgresResource(r):
                    databases.Add(GenerateDatabaseSpec(r, "PG"));
                    break;

                case IResource r when IsRedisResource(r):
                    databases.Add(GenerateDatabaseSpec(r, "REDIS"));
                    break;
            }
        }

        if (services.Count > 0)
        {
            spec["services"] = services;
        }

        if (workers.Count > 0)
        {
            spec["workers"] = workers;
        }

        if (databases.Count > 0)
        {
            spec["databases"] = databases;
        }

        return spec;
    }

    /// <summary>
    /// Serializes an app spec to YAML.
    /// </summary>
    public static string ToYaml(Dictionary<string, object> spec)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return serializer.Serialize(spec);
    }

    private static Dictionary<string, object> GenerateProjectSpec(
        ProjectResource project,
        string? registryName)
    {
        var spec = new Dictionary<string, object>
        {
            ["name"] = SanitizeName(project.Name)
        };

        // Check for GitHub source annotation
        if (project.TryGetAnnotationsOfType<GitHubSourceAnnotation>(out var annotations))
        {
            var gitHubAnnotation = annotations.FirstOrDefault();
            if (gitHubAnnotation is not null)
            {
                spec["github"] = new Dictionary<string, object>
                {
                    ["repo"] = gitHubAnnotation.Config.Repository,
                    ["branch"] = gitHubAnnotation.Config.Branch,
                    ["deploy_on_push"] = gitHubAnnotation.Config.DeployOnPush
                };

                if (gitHubAnnotation.Config.SourceDir is not null)
                {
                    spec["source_dir"] = gitHubAnnotation.Config.SourceDir;
                }

                // .NET projects use the dotnet buildpack
                spec["environment_slug"] = "dotnet";
            }
        }
        else if (registryName is not null)
        {
            // Container-based deployment
            spec["image"] = new Dictionary<string, object>
            {
                ["registry_type"] = "DOCR",
                ["repository"] = $"{registryName}/{SanitizeName(project.Name)}",
                ["tag"] = "latest"
            };
        }

        // Default HTTP port for ASP.NET apps
        spec["http_port"] = 8080;
        spec["instance_count"] = 1;
        spec["instance_size_slug"] = "apps-s-1vcpu-0.5gb";

        return spec;
    }

    private static Dictionary<string, object> GenerateContainerSpec(ContainerResource container)
    {
        var spec = new Dictionary<string, object>
        {
            ["name"] = SanitizeName(container.Name)
        };

        // Get the image from the container resource
        if (container.TryGetContainerImageName(out var imageName))
        {
            var parts = imageName.Split(':');
            var repository = parts[0];
            var tag = parts.Length > 1 ? parts[1] : "latest";

            // Determine registry type from image name
            var registryType = "DOCKER_HUB";
            if (repository.StartsWith("registry.digitalocean.com"))
            {
                registryType = "DOCR";
                repository = repository.Replace("registry.digitalocean.com/", "");
            }
            else if (repository.StartsWith("ghcr.io"))
            {
                registryType = "GHCR";
            }

            spec["image"] = new Dictionary<string, object>
            {
                ["registry_type"] = registryType,
                ["repository"] = repository,
                ["tag"] = tag
            };
        }

        spec["instance_count"] = 1;
        spec["instance_size_slug"] = "apps-s-1vcpu-0.5gb";

        return spec;
    }

    private static Dictionary<string, object> GenerateDatabaseSpec(IResource resource, string engine)
    {
        return new Dictionary<string, object>
        {
            ["name"] = SanitizeName(resource.Name),
            ["engine"] = engine,
            ["production"] = false
        };
    }

    private static bool IsHttpService(IResource resource)
    {
        // Check if the resource has HTTP endpoints
        if (resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints))
        {
            return endpoints.Any(e => 
                e.UriScheme == "http" || 
                e.UriScheme == "https");
        }

        // Default to true for project resources
        return resource is ProjectResource;
    }

    private static bool IsPostgresResource(IResource resource)
    {
        var typeName = resource.GetType().Name;
        return typeName.Contains("Postgres", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRedisResource(IResource resource)
    {
        var typeName = resource.GetType().Name;
        return typeName.Contains("Redis", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Valkey", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Garnet", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sanitizes a resource name to comply with App Platform naming requirements.
    /// </summary>
    private static string SanitizeName(string name)
    {
        // App Platform names must be lowercase, alphanumeric with hyphens
        // Min 2, max 32 characters
        var sanitized = name.ToLowerInvariant()
            .Replace("_", "-")
            .Replace(".", "-");

        // Remove any characters that aren't alphanumeric or hyphens
        sanitized = new string(sanitized
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());

        // Ensure it starts and ends with alphanumeric
        sanitized = sanitized.Trim('-');

        // Truncate to 32 characters
        if (sanitized.Length > 32)
        {
            sanitized = sanitized[..32].TrimEnd('-');
        }

        // Ensure minimum length
        if (sanitized.Length < 2)
        {
            sanitized = $"app-{sanitized}";
        }

        return sanitized;
    }
}

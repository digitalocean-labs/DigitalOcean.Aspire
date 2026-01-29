// Licensed under the MIT License.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using InfinityFlow.DigitalOcean.Client.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.DigitalOcean.AppPlatform;

/// <summary>
/// Information about the git repository for source-based deployment.
/// </summary>
public record GitRepoInfo(string? Repository, string Branch, string RepoRootPath);

/// <summary>
/// Generates DigitalOcean App Platform app specs from Aspire resources.
/// Uses models from InfinityFlow.DigitalOcean.Client for type-safe App Spec generation.
/// </summary>
public static class AppSpecGenerator
{
    /// <summary>
    /// Generates an App Spec from the given Aspire resources using InfinityFlow.DigitalOcean models.
    /// </summary>
    /// <param name="appName">The app name.</param>
    /// <param name="region">The deployment region.</param>
    /// <param name="resources">The Aspire resources to include.</param>
    /// <param name="registryName">Optional DOCR registry name for container images.</param>
    /// <param name="gitInfo">Optional git repository info for source-based deployment.</param>
    /// <returns>The generated app spec.</returns>
    public static App_spec Generate(
        string appName,
        string region,
        IEnumerable<IResource> resources,
        string? registryName = null,
        GitRepoInfo? gitInfo = null)
    {
        var spec = new App_spec
        {
            Name = SanitizeName(appName),
            Region = ParseRegion(region),
            Services = [],
            Workers = [],
            Databases = []
        };

        foreach (var resource in resources)
        {
            switch (resource)
            {
                case ProjectResource project:
                    if (IsHttpService(project))
                    {
                        spec.Services.Add(GenerateServiceSpec(project, registryName, gitInfo));
                    }
                    else
                    {
                        spec.Workers.Add(GenerateWorkerSpec(project, registryName, gitInfo));
                    }
                    break;

                case ContainerResource container:
                    if (IsHttpService(container))
                    {
                        spec.Services.Add(GenerateContainerServiceSpec(container, registryName, gitInfo));
                    }
                    else
                    {
                        spec.Workers.Add(GenerateContainerWorkerSpec(container, registryName, gitInfo));
                    }
                    break;

                // Database resources
                case IResource r when IsPostgresResource(r):
                    spec.Databases.Add(GenerateDatabaseSpec(r, App_database_spec_engine.PG));
                    break;

                case IResource r when IsRedisResource(r):
                    spec.Databases.Add(GenerateDatabaseSpec(r, App_database_spec_engine.REDIS));
                    break;

                // Fallback for any resource with AppServiceAnnotation or AppWorkerAnnotation
                default:
                    if (resource.TryGetAnnotationsOfType<AppServiceAnnotation>(out _))
                    {
                        spec.Services.Add(GenerateGenericServiceSpec(resource, registryName, gitInfo));
                    }
                    else if (resource.TryGetAnnotationsOfType<AppWorkerAnnotation>(out _))
                    {
                        spec.Workers.Add(GenerateGenericWorkerSpec(resource, registryName, gitInfo));
                    }
                    break;
            }
        }

        // Remove empty collections for cleaner YAML output
        if (spec.Services.Count == 0) spec.Services = null;
        if (spec.Workers.Count == 0) spec.Workers = null;
        if (spec.Databases.Count == 0) spec.Databases = null;

        return spec;
    }

    /// <summary>
    /// Serializes an App Spec to YAML.
    /// </summary>
    /// <param name="spec">The app spec to serialize.</param>
    /// <returns>The YAML representation of the app spec.</returns>
    public static string ToYaml(App_spec spec)
    {
        // Convert to a clean dictionary structure for proper YAML serialization
        var cleanSpec = ConvertToCleanSpec(spec);
        
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        return serializer.Serialize(cleanSpec);
    }

    /// <summary>
    /// Converts the App_spec to a clean dictionary structure for YAML serialization.
    /// This handles the composed type wrappers from the InfinityFlow models.
    /// </summary>
    private static Dictionary<string, object?> ConvertToCleanSpec(App_spec spec)
    {
        var result = new Dictionary<string, object?>
        {
            ["name"] = spec.Name,
            ["region"] = spec.Region?.ToString()?.ToLowerInvariant()
        };

        if (spec.Services is { Count: > 0 })
        {
            result["services"] = spec.Services.Select(s => new Dictionary<string, object?>
            {
                ["name"] = s.Name,
                ["http_port"] = s.HttpPort,
                ["instance_count"] = s.InstanceCount,
                ["instance_size_slug"] = s.InstanceSizeSlug?.String,
                ["environment_slug"] = s.EnvironmentSlug,
                ["source_dir"] = s.SourceDir,
                ["build_command"] = s.BuildCommand,
                ["run_command"] = s.RunCommand,
                ["health_check"] = s.HealthCheck is not null ? new Dictionary<string, object?>
                {
                    ["http_path"] = s.HealthCheck.HttpPath,
                    ["initial_delay_seconds"] = s.HealthCheck.InitialDelaySeconds,
                    ["period_seconds"] = s.HealthCheck.PeriodSeconds,
                    ["timeout_seconds"] = s.HealthCheck.TimeoutSeconds,
                    ["success_threshold"] = s.HealthCheck.SuccessThreshold,
                    ["failure_threshold"] = s.HealthCheck.FailureThreshold
                }.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value) : null,
                ["github"] = s.Github is not null ? new Dictionary<string, object?>
                {
                    ["repo"] = s.Github.Repo,
                    ["branch"] = s.Github.Branch,
                    ["deploy_on_push"] = s.Github.DeployOnPush
                } : null,
                ["image"] = s.Image is not null ? new Dictionary<string, object?>
                {
                    ["registry_type"] = s.Image.RegistryType?.ToString(),
                    ["repository"] = s.Image.Repository,
                    ["tag"] = s.Image.Tag
                } : null
            }.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();
        }

        if (spec.Workers is { Count: > 0 })
        {
            result["workers"] = spec.Workers.Select(w => new Dictionary<string, object?>
            {
                ["name"] = w.Name,
                ["instance_count"] = w.InstanceCount,
                ["instance_size_slug"] = w.InstanceSizeSlug?.String,
                ["environment_slug"] = w.EnvironmentSlug,
                ["source_dir"] = w.SourceDir,
                ["build_command"] = w.BuildCommand,
                ["run_command"] = w.RunCommand,
                ["github"] = w.Github is not null ? new Dictionary<string, object?>
                {
                    ["repo"] = w.Github.Repo,
                    ["branch"] = w.Github.Branch,
                    ["deploy_on_push"] = w.Github.DeployOnPush
                } : null,
                ["image"] = w.Image is not null ? new Dictionary<string, object?>
                {
                    ["registry_type"] = w.Image.RegistryType?.ToString(),
                    ["repository"] = w.Image.Repository,
                    ["tag"] = w.Image.Tag
                } : null
            }.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();
        }

        if (spec.Databases is { Count: > 0 })
        {
            result["databases"] = spec.Databases.Select(d => new Dictionary<string, object?>
            {
                ["name"] = d.Name,
                ["engine"] = d.Engine?.ToString(),
                ["production"] = d.Production
            }.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();
        }

        // Remove null values from the root
        return result.Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Generates an App Spec dictionary for backward compatibility.
    /// </summary>
    [Obsolete("Use the overload that returns App_spec for type-safe access.")]
    public static Dictionary<string, object> Generate(
        string appName,
        string region,
        IEnumerable<IResource> resources,
        string? registryName,
        bool asDictionary)
    {
        var spec = Generate(appName, region, resources, registryName);
        return ConvertToDict(spec);
    }

    /// <summary>
    /// Serializes a dictionary-based app spec to YAML.
    /// </summary>
    [Obsolete("Use the overload that takes App_spec for type-safe serialization.")]
    public static string ToYaml(Dictionary<string, object> spec)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return serializer.Serialize(spec);
    }

    private static App_service_spec GenerateServiceSpec(ProjectResource project, string? registryName, GitRepoInfo? gitInfo)
    {
        // Create the spec with Aspire-inferred defaults
        var serviceSpec = new App_service_spec
        {
            Name = SanitizeName(project.Name),
            HttpPort = GetHttpPort(project),
            InstanceCount = 1,
            InstanceSizeSlug = new App_service_spec.App_service_spec_instance_size_slug 
            { 
                String = "apps-s-1vcpu-0.5gb" 
            }
        };

        // Determine deployment source: container image or source-based
        if (project.TryGetAnnotationsOfType<PublishAsContainerImageAnnotation>(out var containerAnnotations))
        {
            // Container-based deployment using DOCR
            var containerConfig = containerAnnotations.First();
            var registry = containerConfig.RegistryName ?? registryName;
            var imageName = containerConfig.ImageName ?? SanitizeName(project.Name);
            
            serviceSpec.Image = new Apps_image_source_spec
            {
                RegistryType = Apps_image_source_spec_registry_type.DOCR,
                Repository = registry is not null ? $"{registry}/{imageName}" : imageName,
                Tag = containerConfig.Tag
            };
        }
        else if (project.TryGetAnnotationsOfType<GitHubSourceAnnotation>(out var gitHubAnnotations))
        {
            // Explicit GitHub source annotation
            var gitHubAnnotation = gitHubAnnotations.First();
            serviceSpec.Github = new Apps_github_source_spec
            {
                Repo = gitHubAnnotation.Config.Repository,
                Branch = gitHubAnnotation.Config.Branch,
                DeployOnPush = gitHubAnnotation.Config.DeployOnPush
            };

            if (gitHubAnnotation.Config.SourceDir is not null)
            {
                serviceSpec.SourceDir = gitHubAnnotation.Config.SourceDir;
            }

            // .NET projects use the dotnet buildpack
            serviceSpec.EnvironmentSlug = "dotnet";
        }
        else if (gitInfo?.Repository is not null)
        {
            // Source-based deployment from current git repository
            serviceSpec.Github = new Apps_github_source_spec
            {
                Repo = gitInfo.Repository,
                Branch = gitInfo.Branch,
                DeployOnPush = true
            };

            // Calculate source directory from project path relative to repo root
            serviceSpec.SourceDir = GetProjectSourceDir(project, gitInfo.RepoRootPath);

            // .NET projects use the dotnet buildpack
            serviceSpec.EnvironmentSlug = "dotnet";
        }

        // Apply user configuration callback last to allow full override
        if (project.TryGetAnnotationsOfType<AppServiceAnnotation>(out var serviceAnnotations))
        {
            serviceAnnotations.First().Configure?.Invoke(serviceSpec);
        }

        return serviceSpec;
    }

    /// <summary>
    /// Gets the source directory for a project relative to the repo root.
    /// </summary>
    private static string? GetProjectSourceDir(ProjectResource project, string repoRootPath)
    {
        // Try to get the project path from annotations
        if (project.TryGetAnnotationsOfType<IProjectMetadata>(out var metadata))
        {
            var projectPath = metadata.First().ProjectPath;
            if (!string.IsNullOrEmpty(projectPath))
            {
                var projectDir = Path.GetDirectoryName(projectPath);
                if (projectDir is not null)
                {
                    // Make relative to repo root
                    var relativePath = Path.GetRelativePath(repoRootPath, projectDir);
                    // Normalize to forward slashes for YAML/Linux
                    return relativePath.Replace('\\', '/');
                }
            }
        }
        return null;
    }

    private static App_worker_spec GenerateWorkerSpec(ProjectResource project, string? registryName, GitRepoInfo? gitInfo)
    {
        // Create the spec with Aspire-inferred defaults
        var workerSpec = new App_worker_spec
        {
            Name = SanitizeName(project.Name),
            InstanceCount = 1,
            InstanceSizeSlug = new App_worker_spec.App_worker_spec_instance_size_slug 
            { 
                String = "apps-s-1vcpu-0.5gb" 
            }
        };

        // Determine deployment source: container image or source-based
        if (project.TryGetAnnotationsOfType<PublishAsContainerImageAnnotation>(out var containerAnnotations))
        {
            // Container-based deployment using DOCR
            var containerConfig = containerAnnotations.First();
            var registry = containerConfig.RegistryName ?? registryName;
            var imageName = containerConfig.ImageName ?? SanitizeName(project.Name);
            
            workerSpec.Image = new Apps_image_source_spec
            {
                RegistryType = Apps_image_source_spec_registry_type.DOCR,
                Repository = registry is not null ? $"{registry}/{imageName}" : imageName,
                Tag = containerConfig.Tag
            };
        }
        else if (project.TryGetAnnotationsOfType<GitHubSourceAnnotation>(out var gitHubAnnotations))
        {
            // Explicit GitHub source annotation
            var gitHubAnnotation = gitHubAnnotations.First();
            workerSpec.Github = new Apps_github_source_spec
            {
                Repo = gitHubAnnotation.Config.Repository,
                Branch = gitHubAnnotation.Config.Branch,
                DeployOnPush = gitHubAnnotation.Config.DeployOnPush
            };

            if (gitHubAnnotation.Config.SourceDir is not null)
            {
                workerSpec.SourceDir = gitHubAnnotation.Config.SourceDir;
            }

            workerSpec.EnvironmentSlug = "dotnet";
        }
        else if (gitInfo?.Repository is not null)
        {
            // Source-based deployment from current git repository
            workerSpec.Github = new Apps_github_source_spec
            {
                Repo = gitInfo.Repository,
                Branch = gitInfo.Branch,
                DeployOnPush = true
            };

            // Calculate source directory from project path relative to repo root
            workerSpec.SourceDir = GetProjectSourceDir(project, gitInfo.RepoRootPath);
            workerSpec.EnvironmentSlug = "dotnet";
        }

        // Apply user configuration callback last to allow full override
        if (project.TryGetAnnotationsOfType<AppWorkerAnnotation>(out var workerAnnotations))
        {
            workerAnnotations.First().Configure?.Invoke(workerSpec);
        }

        return workerSpec;
    }

    private static App_service_spec GenerateContainerServiceSpec(ContainerResource container, string? registryName, GitRepoInfo? gitInfo)
    {
        // Create the spec with Aspire-inferred defaults
        var serviceSpec = new App_service_spec
        {
            Name = SanitizeName(container.Name),
            HttpPort = GetHttpPort(container),
            InstanceCount = 1,
            InstanceSizeSlug = new App_service_spec.App_service_spec_instance_size_slug 
            { 
                String = "apps-s-1vcpu-0.5gb" 
            }
        };

        // Determine deployment source
        if (container.TryGetAnnotationsOfType<PublishAsContainerImageAnnotation>(out var containerAnnotations))
        {
            // Explicit container image to DOCR
            var containerConfig = containerAnnotations.First();
            var registry = containerConfig.RegistryName ?? registryName;
            var imageName = containerConfig.ImageName ?? SanitizeName(container.Name);
            
            serviceSpec.Image = new Apps_image_source_spec
            {
                RegistryType = Apps_image_source_spec_registry_type.DOCR,
                Repository = registry is not null ? $"{registry}/{imageName}" : imageName,
                Tag = containerConfig.Tag
            };
        }
        else if (gitInfo?.Repository is not null && container.TryGetAnnotationsOfType<AppServiceAnnotation>(out _))
        {
            // Source-based deployment when marked with PublishAsAppService and git info is available
            serviceSpec.Github = new Apps_github_source_spec
            {
                Repo = gitInfo.Repository,
                Branch = gitInfo.Branch,
                DeployOnPush = true
            };

            // Calculate source directory from container's working directory
            serviceSpec.SourceDir = GetContainerSourceDir(container, gitInfo.RepoRootPath);
        }
        else if (container.TryGetContainerImageName(out var imageName))
        {
            // Fallback to container image
            var (repository, tag, registryType) = ParseImageName(imageName);
            serviceSpec.Image = new Apps_image_source_spec
            {
                RegistryType = registryType,
                Repository = repository,
                Tag = tag
            };
        }

        // Apply user configuration callback last to allow full override
        if (container.TryGetAnnotationsOfType<AppServiceAnnotation>(out var serviceAnnotations))
        {
            serviceAnnotations.First().Configure?.Invoke(serviceSpec);
        }

        return serviceSpec;
    }

    /// <summary>
    /// Gets the source directory for a container relative to the repo root.
    /// </summary>
    private static string? GetContainerSourceDir(IResource resource, string repoRootPath)
    {
        // Try to get the working directory from ExecutableAnnotation
        if (resource.TryGetAnnotationsOfType<ExecutableAnnotation>(out var executableAnnotations))
        {
            var workingDir = executableAnnotations.First().WorkingDirectory;
            if (!string.IsNullOrEmpty(workingDir))
            {
                // Make relative to repo root
                var relativePath = Path.GetRelativePath(repoRootPath, workingDir);
                // Normalize to forward slashes for YAML/Linux
                return relativePath.Replace('\\', '/');
            }
        }

        return null;
    }

    private static App_worker_spec GenerateContainerWorkerSpec(ContainerResource container, string? registryName, GitRepoInfo? gitInfo)
    {
        // Create the spec with Aspire-inferred defaults
        var workerSpec = new App_worker_spec
        {
            Name = SanitizeName(container.Name),
            InstanceCount = 1,
            InstanceSizeSlug = new App_worker_spec.App_worker_spec_instance_size_slug 
            { 
                String = "apps-s-1vcpu-0.5gb" 
            }
        };

        // Determine deployment source
        if (container.TryGetAnnotationsOfType<PublishAsContainerImageAnnotation>(out var containerAnnotations))
        {
            // Explicit container image to DOCR
            var containerConfig = containerAnnotations.First();
            var registry = containerConfig.RegistryName ?? registryName;
            var imageName = containerConfig.ImageName ?? SanitizeName(container.Name);
            
            workerSpec.Image = new Apps_image_source_spec
            {
                RegistryType = Apps_image_source_spec_registry_type.DOCR,
                Repository = registry is not null ? $"{registry}/{imageName}" : imageName,
                Tag = containerConfig.Tag
            };
        }
        else if (gitInfo?.Repository is not null && container.TryGetAnnotationsOfType<AppWorkerAnnotation>(out _))
        {
            // Source-based deployment when marked with PublishAsAppWorker and git info is available
            workerSpec.Github = new Apps_github_source_spec
            {
                Repo = gitInfo.Repository,
                Branch = gitInfo.Branch,
                DeployOnPush = true
            };

            workerSpec.SourceDir = GetContainerSourceDir(container, gitInfo.RepoRootPath);
        }
        else if (container.TryGetContainerImageName(out var imageName))
        {
            // Fallback to container image
            var (repository, tag, registryType) = ParseImageName(imageName);
            workerSpec.Image = new Apps_image_source_spec
            {
                RegistryType = registryType,
                Repository = repository,
                Tag = tag
            };
        }

        // Apply user configuration callback last to allow full override
        if (container.TryGetAnnotationsOfType<AppWorkerAnnotation>(out var workerAnnotations))
        {
            workerAnnotations.First().Configure?.Invoke(workerSpec);
        }

        return workerSpec;
    }

    /// <summary>
    /// Generates a service spec for any resource type that has the AppServiceAnnotation.
    /// </summary>
    private static App_service_spec GenerateGenericServiceSpec(IResource resource, string? registryName, GitRepoInfo? gitInfo)
    {
        // Create the spec with Aspire-inferred defaults
        var serviceSpec = new App_service_spec
        {
            Name = SanitizeName(resource.Name),
            HttpPort = GetHttpPort(resource),
            InstanceCount = 1,
            InstanceSizeSlug = new App_service_spec.App_service_spec_instance_size_slug 
            { 
                String = "apps-s-1vcpu-0.5gb" 
            }
        };

        // Determine deployment source
        if (resource.TryGetAnnotationsOfType<PublishAsContainerImageAnnotation>(out var containerAnnotations))
        {
            var containerConfig = containerAnnotations.First();
            var registry = containerConfig.RegistryName ?? registryName;
            var imageName = containerConfig.ImageName ?? SanitizeName(resource.Name);
            
            serviceSpec.Image = new Apps_image_source_spec
            {
                RegistryType = Apps_image_source_spec_registry_type.DOCR,
                Repository = registry is not null ? $"{registry}/{imageName}" : imageName,
                Tag = containerConfig.Tag
            };
        }
        else if (resource.TryGetAnnotationsOfType<GitHubSourceAnnotation>(out var gitHubAnnotations))
        {
            var gitHubAnnotation = gitHubAnnotations.First();
            serviceSpec.Github = new Apps_github_source_spec
            {
                Repo = gitHubAnnotation.Config.Repository,
                Branch = gitHubAnnotation.Config.Branch,
                DeployOnPush = gitHubAnnotation.Config.DeployOnPush
            };

            if (gitHubAnnotation.Config.SourceDir is not null)
            {
                serviceSpec.SourceDir = gitHubAnnotation.Config.SourceDir;
            }
        }
        else if (gitInfo?.Repository is not null)
        {
            serviceSpec.Github = new Apps_github_source_spec
            {
                Repo = gitInfo.Repository,
                Branch = gitInfo.Branch,
                DeployOnPush = true
            };
            serviceSpec.SourceDir = GetContainerSourceDir(resource, gitInfo.RepoRootPath);
        }

        // Apply user configuration callback last to allow full override
        if (resource.TryGetAnnotationsOfType<AppServiceAnnotation>(out var serviceAnnotations))
        {
            serviceAnnotations.First().Configure?.Invoke(serviceSpec);
        }

        return serviceSpec;
    }

    /// <summary>
    /// Generates a worker spec for any resource type that has the AppWorkerAnnotation.
    /// </summary>
    private static App_worker_spec GenerateGenericWorkerSpec(IResource resource, string? registryName, GitRepoInfo? gitInfo)
    {
        // Create the spec with Aspire-inferred defaults
        var workerSpec = new App_worker_spec
        {
            Name = SanitizeName(resource.Name),
            InstanceCount = 1,
            InstanceSizeSlug = new App_worker_spec.App_worker_spec_instance_size_slug 
            { 
                String = "apps-s-1vcpu-0.5gb" 
            }
        };

        // Determine deployment source
        if (resource.TryGetAnnotationsOfType<PublishAsContainerImageAnnotation>(out var containerAnnotations))
        {
            var containerConfig = containerAnnotations.First();
            var registry = containerConfig.RegistryName ?? registryName;
            var imageName = containerConfig.ImageName ?? SanitizeName(resource.Name);
            
            workerSpec.Image = new Apps_image_source_spec
            {
                RegistryType = Apps_image_source_spec_registry_type.DOCR,
                Repository = registry is not null ? $"{registry}/{imageName}" : imageName,
                Tag = containerConfig.Tag
            };
        }
        else if (resource.TryGetAnnotationsOfType<GitHubSourceAnnotation>(out var gitHubAnnotations))
        {
            var gitHubAnnotation = gitHubAnnotations.First();
            workerSpec.Github = new Apps_github_source_spec
            {
                Repo = gitHubAnnotation.Config.Repository,
                Branch = gitHubAnnotation.Config.Branch,
                DeployOnPush = gitHubAnnotation.Config.DeployOnPush
            };

            if (gitHubAnnotation.Config.SourceDir is not null)
            {
                workerSpec.SourceDir = gitHubAnnotation.Config.SourceDir;
            }
        }
        else if (gitInfo?.Repository is not null)
        {
            workerSpec.Github = new Apps_github_source_spec
            {
                Repo = gitInfo.Repository,
                Branch = gitInfo.Branch,
                DeployOnPush = true
            };
            workerSpec.SourceDir = GetContainerSourceDir(resource, gitInfo.RepoRootPath);
        }

        // Apply user configuration callback last to allow full override
        if (resource.TryGetAnnotationsOfType<AppWorkerAnnotation>(out var workerAnnotations))
        {
            workerAnnotations.First().Configure?.Invoke(workerSpec);
        }

        return workerSpec;
    }

    private static App_database_spec GenerateDatabaseSpec(IResource resource, App_database_spec_engine engine)
    {
        return new App_database_spec
        {
            Name = SanitizeName(resource.Name),
            Engine = engine,
            Production = false
        };
    }

    private static (string repository, string tag, Apps_image_source_spec_registry_type registryType) ParseImageName(string imageName)
    {
        var parts = imageName.Split(':');
        var repository = parts[0];
        var tag = parts.Length > 1 ? parts[1] : "latest";

        var registryType = Apps_image_source_spec_registry_type.DOCKER_HUB;
        if (repository.StartsWith("registry.digitalocean.com"))
        {
            registryType = Apps_image_source_spec_registry_type.DOCR;
            repository = repository.Replace("registry.digitalocean.com/", "");
        }
        else if (repository.StartsWith("ghcr.io"))
        {
            registryType = Apps_image_source_spec_registry_type.GHCR;
        }

        return (repository, tag, registryType);
    }

    private static App_spec_region? ParseRegion(string region)
    {
        // Map common region codes to App Platform region enum
        return region.ToLowerInvariant() switch
        {
            "nyc" or "nyc1" or "nyc2" or "nyc3" => App_spec_region.Nyc,
            "ams" or "ams2" or "ams3" => App_spec_region.Ams,
            "sfo" or "sfo1" or "sfo2" or "sfo3" => App_spec_region.Sfo,
            "sgp" or "sgp1" => App_spec_region.Sgp,
            "lon" or "lon1" => App_spec_region.Lon,
            "fra" or "fra1" => App_spec_region.Fra,
            "tor" or "tor1" => App_spec_region.Tor,
            "blr" or "blr1" => App_spec_region.Blr,
            "syd" or "syd1" => App_spec_region.Syd,
            _ => App_spec_region.Nyc // Default to NYC
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

    /// <summary>
    /// Gets the HTTP port from endpoint annotations, or returns the default port.
    /// </summary>
    private static int GetHttpPort(IResource resource, int defaultPort = 8080)
    {
        if (resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints))
        {
            // Look for HTTP/HTTPS endpoints and get the target port
            var httpEndpoint = endpoints.FirstOrDefault(e => 
                e.UriScheme == "http" || e.UriScheme == "https");
            
            if (httpEndpoint?.TargetPort is not null)
            {
                return httpEndpoint.TargetPort.Value;
            }
        }

        return defaultPort;
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
    public static string SanitizeName(string name)
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

    private static Dictionary<string, object> ConvertToDict(App_spec spec)
    {
        var dict = new Dictionary<string, object>();
        
        if (spec.Name is not null) dict["name"] = spec.Name;
        if (spec.Region is not null) dict["region"] = spec.Region.ToString()!.ToLowerInvariant();
        
        if (spec.Services is not null && spec.Services.Count > 0)
        {
            dict["services"] = spec.Services.Select(s => new Dictionary<string, object>
            {
                ["name"] = s.Name ?? "",
                ["http_port"] = s.HttpPort ?? 8080,
                ["instance_count"] = s.InstanceCount ?? 1,
                ["instance_size_slug"] = s.InstanceSizeSlug?.String ?? "apps-s-1vcpu-0.5gb"
            }).ToList();
        }
        
        if (spec.Workers is not null && spec.Workers.Count > 0)
        {
            dict["workers"] = spec.Workers.Select(w => new Dictionary<string, object>
            {
                ["name"] = w.Name ?? "",
                ["instance_count"] = w.InstanceCount ?? 1,
                ["instance_size_slug"] = w.InstanceSizeSlug?.String ?? "apps-s-1vcpu-0.5gb"
            }).ToList();
        }
        
        if (spec.Databases is not null && spec.Databases.Count > 0)
        {
            dict["databases"] = spec.Databases.Select(d => new Dictionary<string, object>
            {
                ["name"] = d.Name ?? "",
                ["engine"] = d.Engine?.ToString() ?? "PG",
                ["production"] = d.Production ?? false
            }).ToList();
        }
        
        return dict;
    }

    /// <summary>
    /// Detects git repository information from the current working directory.
    /// </summary>
    /// <param name="startPath">The starting path to search for git repository.</param>
    /// <returns>Git repository info, or null if not in a git repository.</returns>
    public static GitRepoInfo? DetectGitInfo(string startPath)
    {
        try
        {
            // Find the git repository root
            var repoRoot = FindGitRepoRoot(startPath);
            if (repoRoot is null)
            {
                return null;
            }

            // Get the remote URL
            var remoteUrl = RunGitCommand(repoRoot, "remote get-url origin");
            if (string.IsNullOrEmpty(remoteUrl))
            {
                return null;
            }

            // Parse the remote URL to get owner/repo format
            var repository = ParseGitRemoteUrl(remoteUrl);
            if (repository is null)
            {
                return null;
            }

            // Get the current branch
            var branch = RunGitCommand(repoRoot, "rev-parse --abbrev-ref HEAD") ?? "main";

            return new GitRepoInfo(repository, branch, repoRoot);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindGitRepoRoot(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);
        while (currentDir is not null)
        {
            var gitPath = Path.Combine(currentDir.FullName, ".git");
            // Check for .git directory (normal repo) or .git file (worktree)
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        return null;
    }

    private static string? RunGitCommand(string workingDirectory, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ParseGitRemoteUrl(string remoteUrl)
    {
        // Parse SSH format: git@github.com:owner/repo.git
        if (remoteUrl.StartsWith("git@"))
        {
            var parts = remoteUrl.Replace("git@", "").Replace(":", "/").Replace(".git", "").Split('/');
            if (parts.Length >= 3)
            {
                // Skip the host part (e.g., "github.com")
                return $"{parts[1]}/{parts[2]}";
            }
        }
        // Parse HTTPS format: https://github.com/owner/repo.git
        else if (remoteUrl.StartsWith("https://"))
        {
            var uri = new Uri(remoteUrl.Replace(".git", ""));
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            if (pathParts.Length >= 2)
            {
                return $"{pathParts[0]}/{pathParts[1]}";
            }
        }

        return null;
    }
}

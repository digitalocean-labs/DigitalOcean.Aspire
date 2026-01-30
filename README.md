# DigitalOcean.Aspire

DigitalOcean hosting integration for .NET Aspire. Deploy applications to DigitalOcean App Platform with container registry support.

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| `Aspire.Hosting.DigitalOcean` | Coming soon | DigitalOcean hosting integration |

## Features

- **DigitalOcean Container Registry (DOCR)** - Provision or reference existing container registries
- **App Platform Deployment** - Deploy Aspire applications to DigitalOcean App Platform
  - Services (`PublishAsAppService`) - HTTP services with automatic port inference
  - Workers (`PublishAsAppWorker`) - Background workers
  - Static Sites (`PublishAsStaticSite`) - Static web applications
  - Functions (`PublishAsFunctions`) - Serverless functions
- **Source-based Deployment** - Deploy directly from GitHub repositories
- **Container-based Deployment** - Build and push images to DOCR, then deploy
- **Automatic Configuration** - Health check paths, HTTP ports, and internal ports inferred from Aspire annotations
- **Type-safe App Spec** - Uses strongly-typed models from `InfinityFlow.DigitalOcean.Client`

## Quick Start

### Installation

```bash
dotnet add package Aspire.Hosting.DigitalOcean
```

### App Platform Deployment

Enable App Platform deployment support and configure resources:

```csharp
using Aspire.Hosting.DigitalOcean.AppPlatform;

var builder = DistributedApplication.CreateBuilder(args);

// Enable App Platform deployment support with app name and region
builder.WithAppPlatformDeploySupport("my-aspire-app", region: "nyc");

// Add a service with App Platform configuration
var api = builder.AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsAppService("apps-s-1vcpu-1gb");  // Specify instance size

// Add a worker
var worker = builder.AddProject<Projects.MyWorker>("worker")
    .PublishAsAppWorker("apps-d-1vcpu-2gb");

// Add a static site
var frontend = builder.AddViteApp("frontend", "../frontend")
    .PublishAsStaticSite();

builder.Build().Run();
```

Generate the App Platform spec with:
```bash
aspire publish
```

This creates an `app-spec.yaml` file that can be used to deploy to DigitalOcean App Platform.

### Source-Based Deployment

Deploy directly from GitHub without building container images:

```csharp
var api = builder.AddProject<Projects.MyApi>("api")
    .WithGitHubSource("myorg/myrepo", branch: "main", sourceDir: "src/api")
    .PublishAsAppService();
```

### Container-Based Deployment

Build and push container images to DOCR:

```csharp
// Add a DOCR registry
var docr = builder.AddDigitalOceanContainerRegistry("docr", "my-registry")
    .WithRegion(DigitalOceanRegions.NYC3)
    .WithTier(DigitalOceanRegistryTiers.Basic);

// Configure container image publishing
var api = builder.AddProject<Projects.MyApi>("api")
    .PublishAsContainerImage(registryName: "my-registry", imageName: "my-api")
    .PublishAsAppService();
```

### Reference Existing Registry

Reference an existing DOCR registry:

```csharp
var registryName = builder.AddParameter("registryName");

var docr = builder.AddDigitalOceanContainerRegistry("docr", registryName)
    .RunAsExisting();
```

## API Reference

### App Platform Extensions

#### `WithAppPlatformDeploySupport`
Enables App Platform deployment and generates `app-spec.yaml` when running `aspire publish`.

```csharp
builder.WithAppPlatformDeploySupport("my-app", region: "nyc");
builder.WithAppPlatformDeploySupport("my-app", region: "nyc", configureAppSpec: spec => {
    // Configure the entire app spec
});
```

#### `PublishAsAppService`
Marks a resource as an App Platform service (HTTP endpoint required).

```csharp
// Simple usage with instance size
.PublishAsAppService("apps-s-1vcpu-1gb");

// With configuration callback
.PublishAsAppService(service => {
    service.InstanceCount = 3;
    service.HttpPort = 8080;
    service.HealthCheck = new App_service_spec_health_check {
        HttpPath = "/health"
    };
});
```

#### `PublishAsAppWorker`
Marks a resource as an App Platform worker.

```csharp
.PublishAsAppWorker("apps-d-1vcpu-2gb");
```

#### `PublishAsStaticSite`
Marks a resource as a static site.

```csharp
.PublishAsStaticSite();
.PublishAsStaticSite(site => {
    site.EnvironmentSlug = "node-js";
    site.OutputDir = "dist";
});
```

#### `PublishAsFunctions`
Marks a resource as serverless functions (requires GitHub source).

```csharp
.PublishAsFunctions();
```

#### `WithGitHubSource`
Configures GitHub source for source-based deployment.

```csharp
.WithGitHubSource("myorg/myrepo", branch: "main", sourceDir: "src/app");
```

### Automatic Inference

The library automatically infers:
- **HTTP Port** - From `WithHttpEndpoint` or `WithExternalHttpEndpoints` annotations (prefers external HTTPS, then external HTTP, then any HTTP)
- **Internal Ports** - Additional ports from endpoint annotations
- **Health Check Path** - From `WithHttpHealthCheck` annotation
- **Source Directory** - From resource annotations relative to repository root

## Configuration

### Environment Variables

| Variable | Description |
|----------|-------------|
| `DigitalOcean__ApiToken` | DigitalOcean API token |
| `DigitalOcean__Region` | Default region (e.g., "nyc3") |
| `DigitalOcean__AppPlatform__AppName` | App Platform app name |

### appsettings.json

```json
{
  "DigitalOcean": {
    "ApiToken": "dop_v1_...",
    "Region": "nyc3",
    "AppPlatform": {
      "AppName": "my-aspire-app",
      "InstanceSizeSlug": "apps-s-1vcpu-1gb",
      "InstanceCount": 1
    }
  }
}
```

## Available Regions

Use `DigitalOceanRegions` constants or any valid region slug:

```csharp
.WithRegion(DigitalOceanRegions.NYC3)
.WithRegion("nyc3")  // equivalent
```

Available regions: `NYC1`, `NYC2`, `NYC3`, `SFO1`, `SFO2`, `SFO3`, `AMS2`, `AMS3`, `SGP1`, `LON1`, `FRA1`, `TOR1`, `BLR1`, `SYD1`

## Registry Tiers

Use `DigitalOceanRegistryTiers` constants:

| Tier | Storage | Cost |
|------|---------|------|
| `Starter` | 500 MB | Free |
| `Basic` | 5 GB | Paid |
| `Professional` | 50 GB | Paid |

## Project Structure

```
src/
  Aspire.Hosting.DigitalOcean/
    ContainerRegistry/     # DOCR resource and extensions
    AppPlatform/           # App Platform deployment
tests/
  Aspire.Hosting.DigitalOcean.Tests/
    AppPlatform/          # App Platform tests
    ContainerRegistry/     # Container registry tests
```

## Testing

The project includes comprehensive unit tests using xUnit and FluentAssertions. Run tests with:

```bash
dotnet test
```

Test coverage includes:
- **App Platform Extensions** - Extension method behavior and annotation handling
- **App Spec Generation** - YAML generation, region parsing, name sanitization
- **Endpoint Inference** - HTTP port detection, internal ports, health check paths
- **Region Validation** - Region validation and constants
- **Annotations** - All annotation types and their configuration callbacks

## CI/CD with GitHub Actions

```yaml
name: Deploy to DigitalOcean App Platform

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Install Aspire CLI
        run: curl -sSL https://aspire.dev/install.sh | bash
      - name: Deploy
        env:
          DigitalOcean__ApiToken: ${{ secrets.DO_API_TOKEN }}
        run: aspire deploy
```

## Links

- [DigitalOcean App Platform](https://docs.digitalocean.com/products/app-platform/)
- [App Spec Reference](https://docs.digitalocean.com/products/app-platform/reference/app-spec/)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)

## License

MIT

# Aspire.Hosting.DigitalOcean

DigitalOcean hosting integration for .NET Aspire. Deploy applications to DigitalOcean App Platform with container registry support.

## Installation

```bash
dotnet add package Aspire.Hosting.DigitalOcean
```

## Quick Start

### Container-Based Deployment

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a DOCR registry
var docr = builder.AddDigitalOceanContainerRegistry("docr", "my-registry")
    .WithRegion(DigitalOceanRegions.NYC3)
    .WithTier(DigitalOceanRegistryTiers.Basic);

// Add your services
var api = builder.AddProject<Projects.MyApi>("api");
var web = builder.AddProject<Projects.MyWeb>("web")
    .WithReference(api);

// Configure App Platform deployment
builder.AddDockerComposeEnvironment("do-app")
    .WithDigitalOceanContainerRegistry(docr)
    .WithAppPlatformDeploySupport();

builder.Build().Run();
```

Deploy with:
```bash
aspire deploy
```

### Source-Based Deployment

Deploy directly from GitHub without building container images:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MyApi>("api")
    .WithGitHubSource("myorg/myrepo", branch: "main");

builder.AddDockerComposeEnvironment("do-app")
    .WithAppPlatformDeploySupport();

builder.Build().Run();
```

### Reference Existing Registry

```csharp
var registryName = builder.AddParameter("registryName");

var docr = builder.AddDigitalOceanContainerRegistry("docr", registryName)
    .RunAsExisting();
```

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

## Regions

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

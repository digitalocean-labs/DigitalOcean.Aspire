using Aspire.Hosting.DigitalOcean.AppPlatform;

var builder = DistributedApplication.CreateBuilder(args);

// Enable App Platform deployment support with app name and region
builder.WithAppPlatformDeploySupport("docr-sample-app", region: "nyc");

// Add Redis cache - will be published as App Platform database
var cache = builder.AddRedis("cache");

// Add the .NET server project - will use source-based deployment from GitHub
// Health check path is automatically inferred from WithHttpHealthCheck()
var server = builder.AddProject<Projects.docr_Web_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsAppService("apps-s-1vcpu-1gb");

// Add the Vite frontend - uses source-based deployment with node-js buildpack
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server)
    .PublishAsStaticSite();

builder.Build().Run();

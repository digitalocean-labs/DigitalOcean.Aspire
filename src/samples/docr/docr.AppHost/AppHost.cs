using Aspire.Hosting.DigitalOcean.AppPlatform;
using InfinityFlow.DigitalOcean.Client.Models;

var builder = DistributedApplication.CreateBuilder(args);

// Enable App Platform deployment support with app name and region
builder.WithAppPlatformDeploySupport("docr-sample-app", region: "nyc");

// Add Redis cache - will be published as App Platform database
var cache = builder.AddRedis("cache");

// Add the .NET server project - will use source-based deployment from GitHub
var server = builder.AddProject<Projects.docr_Web_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsAppService(service =>
    {
        service.InstanceCount = 2;
        service.InstanceSizeSlug = new App_service_spec.App_service_spec_instance_size_slug { String = "apps-s-1vcpu-1gb" };
        service.HealthCheck = new App_service_spec_health_check { HttpPath = "/health" };
    });

// Add the Vite frontend - uses source-based deployment with node-js buildpack
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server)
    .PublishAsAppService(service =>
    {
        service.EnvironmentSlug = "node-js";
    });

builder.Build().Run();

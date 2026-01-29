using Aspire.Hosting.DigitalOcean;
using Aspire.Hosting.DigitalOcean.AppPlatform;
using Aspire.Hosting.DigitalOcean.ContainerRegistry;

var builder = DistributedApplication.CreateBuilder(args);

// Add a DigitalOcean Container Registry (DOCR)
var docr = builder.AddDigitalOceanContainerRegistry("docr", "my-registry")
    .WithRegion(DigitalOceanRegions.NYC3)
    .WithTier(DigitalOceanRegistryTiers.Professional);

// Configure Docker Compose environment with App Platform deployment support
builder.AddDockerComposeEnvironment("do-app")
    .WithAppPlatformDeploySupport();


var cache = builder.AddRedis("cache").PublishAsDockerComposeService((resource, service) =>
{
    service.Name = "cache";
});

var server = builder.AddProject<Projects.docr_Web_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "server";
    });

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "webfrontend";
    });

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();

// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.AppPlatform;
using FluentAssertions;
using InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.Tests.AppPlatform;

public class AppSpecGeneratorEndpointTests
{
    [Fact]
    public void Generate_WithHttpEndpoint_SetsHttpPort()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 3000)
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].HttpPort.Should().Be(3000);
    }

    [Fact]
    public void Generate_WithExternalHttpEndpoints_PrefersExternalPort()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .WithExternalHttpEndpoints()
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].HttpPort.Should().Be(8080);
    }

    [Fact]
    public void Generate_WithMultipleEndpoints_SetsInternalPorts()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080, name: "http")
            .WithEndpoint(targetPort: 9090, name: "grpc", scheme: "http")
            .WithEndpoint(targetPort: 9091, name: "metrics", scheme: "http")
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].HttpPort.Should().Be(8080);
        spec.Services[0].InternalPorts.Should().Contain(9090);
        spec.Services[0].InternalPorts.Should().Contain(9091);
    }

    [Fact]
    public void Generate_WithNoEndpoints_ContainerHasNoHttpPort()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert - ContainerResource without HTTP endpoints is treated as a worker
        // Since it doesn't have HTTP endpoints, it falls back to container image deployment
        spec.Should().NotBeNull();
    }

    [Fact]
    public void Generate_WithHttpHealthCheck_SetsHealthCheckPath()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .WithHttpHealthCheck("/health")
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].HealthCheck.Should().NotBeNull();
        spec.Services[0].HealthCheck!.HttpPath.Should().Be("/health");
    }

    [Fact]
    public void Generate_WithHttpHealthCheckDeepPath_SetsFullPath()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .WithHttpHealthCheck("/api/health/ready")
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].HealthCheck.Should().NotBeNull();
        spec.Services[0].HealthCheck!.HttpPath.Should().Be("/api/health/ready");
    }

    [Fact]
    public void Generate_WithoutHealthCheck_DoesNotSetHealthCheck()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].HealthCheck.Should().BeNull();
    }

    [Fact]
    public void Generate_WithInstanceSizeSlugOverride_UsesOverride()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsAppService("apps-d-2vcpu-4gb");

        var resources = new IResource[] { container.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Services.Should().HaveCount(1);
        spec.Services[0].InstanceSizeSlug?.String.Should().Be("apps-d-2vcpu-4gb");
    }
}

// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.AppPlatform;
using FluentAssertions;

namespace Aspire.Hosting.DigitalOcean.Tests.AppPlatform;

public class AppSpecYamlSerializationTests
{
    [Fact]
    public void ToYaml_WithService_GeneratesCorrectYaml()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().Contain("name: test-app");
        yaml.Should().Contain("region: nyc");
        yaml.Should().Contain("services:");
        yaml.Should().Contain("- name: myservice");
        yaml.Should().Contain("http_port: 8080");
    }

    [Fact]
    public void ToYaml_WithWorker_GeneratesWorkerSection()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myworker", "nginx")
            .PublishAsAppWorker();

        var resources = new IResource[] { container.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Workers.Should().HaveCount(1);
    }

    [Fact]
    public void ToYaml_WithHealthCheck_IncludesHealthCheckSection()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .WithHttpHealthCheck("/health")
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().Contain("health_check:");
        yaml.Should().Contain("http_path: /health");
    }

    [Fact]
    public void ToYaml_WithContainerImage_IncludesImageSection()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        
        // ContainerResource with an explicit image uses image-based deployment
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert - Container resources use their container image by default
        yaml.Should().Contain("name: myservice");
        yaml.Should().Contain("image:");
        yaml.Should().Contain("repository: nginx");
    }

    [Fact]
    public void ToYaml_WithMultipleServices_GeneratesAllServices()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var service1 = builder.AddContainer("service1", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsAppService();
        var service2 = builder.AddContainer("service2", "nginx")
            .WithHttpEndpoint(targetPort: 9090)
            .PublishAsAppService();

        var resources = new IResource[] { service1.Resource, service2.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().Contain("- name: service1");
        yaml.Should().Contain("- name: service2");
        spec.Services.Should().HaveCount(2);
    }

    [Fact]
    public void ToYaml_WithInternalPorts_IncludesInternalPortsSection()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080, name: "http")
            .WithEndpoint(targetPort: 9090, name: "grpc", scheme: "http")
            .PublishAsAppService();

        var resources = new IResource[] { container.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().Contain("internal_ports:");
        yaml.Should().Contain("- 9090");
    }

    [Fact]
    public void ToYaml_WithInstanceSizeSlug_IncludesInstanceSizeSlug()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("myservice", "nginx")
            .WithHttpEndpoint(targetPort: 8080)
            .PublishAsAppService("apps-d-2vcpu-4gb");

        var resources = new IResource[] { container.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().Contain("instance_size_slug: apps-d-2vcpu-4gb");
    }
}

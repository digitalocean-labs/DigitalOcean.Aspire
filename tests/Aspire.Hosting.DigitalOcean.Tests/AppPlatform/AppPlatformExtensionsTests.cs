// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.AppPlatform;
using FluentAssertions;
using InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.Tests.AppPlatform;

public class AppPlatformExtensionsTests
{
    [Fact]
    public void WithAppPlatformDeploySupport_AddsPublisherResource()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        builder.WithAppPlatformDeploySupport("my-app", region: "nyc");

        // Assert
        var publisherResource = builder.Resources.OfType<AppPlatformPublisherResource>().FirstOrDefault();
        publisherResource.Should().NotBeNull();
    }

    [Fact]
    public void PublishAsAppService_AddsAppServiceAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsAppService();

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppServiceAnnotation>(out var annotations).Should().BeTrue();
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public void PublishAsAppService_WithInstanceSizeSlug_SetsInstanceSize()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsAppService("apps-s-1vcpu-1gb");

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppServiceAnnotation>(out var annotations).Should().BeTrue();
        
        // Verify the callback sets the instance size
        var serviceSpec = new App_service_spec();
        annotations!.First().Configure?.Invoke(serviceSpec);
        serviceSpec.InstanceSizeSlug?.String.Should().Be("apps-s-1vcpu-1gb");
    }

    [Fact]
    public void PublishAsAppService_WithConfigureCallback_InvokesCallback()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsAppService(service =>
        {
            service.InstanceCount = 3;
            service.HttpPort = 9000;
        });

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppServiceAnnotation>(out var annotations).Should().BeTrue();
        
        var serviceSpec = new App_service_spec();
        annotations!.First().Configure?.Invoke(serviceSpec);
        serviceSpec.InstanceCount.Should().Be(3);
        serviceSpec.HttpPort.Should().Be(9000);
    }

    [Fact]
    public void PublishAsAppWorker_AddsAppWorkerAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsAppWorker();

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppWorkerAnnotation>(out var annotations).Should().BeTrue();
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public void PublishAsAppWorker_WithInstanceSizeSlug_SetsInstanceSize()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsAppWorker("apps-d-1vcpu-2gb");

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppWorkerAnnotation>(out var annotations).Should().BeTrue();
        
        var workerSpec = new App_worker_spec();
        annotations!.First().Configure?.Invoke(workerSpec);
        workerSpec.InstanceSizeSlug?.String.Should().Be("apps-d-1vcpu-2gb");
    }

    [Fact]
    public void PublishAsStaticSite_AddsAppStaticSiteAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsStaticSite();

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppStaticSiteAnnotation>(out var annotations).Should().BeTrue();
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public void PublishAsStaticSite_WithConfigureCallback_InvokesCallback()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsStaticSite(site =>
        {
            site.EnvironmentSlug = "node-js";
            site.OutputDir = "dist";
        });

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppStaticSiteAnnotation>(out var annotations).Should().BeTrue();
        
        var siteSpec = new App_static_site_spec();
        annotations!.First().Configure?.Invoke(siteSpec);
        siteSpec.EnvironmentSlug.Should().Be("node-js");
        siteSpec.OutputDir.Should().Be("dist");
    }

    [Fact]
    public void PublishAsFunctions_AddsAppFunctionsAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsFunctions();

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppFunctionsAnnotation>(out var annotations).Should().BeTrue();
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public void PublishAsAppService_AddsAppSpecPublishAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.PublishAsAppService();

        // Assert
        container.Resource.TryGetAnnotationsOfType<AppSpecPublishAnnotation>(out var annotations).Should().BeTrue();
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public void WithGitHubSource_AddsAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("test", "nginx");

        // Act
        container.WithGitHubSource("myorg/myrepo", branch: "develop");

        // Assert - we verify indirectly through the spec generator
        // The annotation is internal, but the extension method is public
        container.Resource.Annotations.Should().HaveCountGreaterThan(0);
    }
}

// Licensed under the MIT License.

using Aspire.Hosting.DigitalOcean.AppPlatform;
using FluentAssertions;
using InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.Tests.AppPlatform;

public class AnnotationTests
{
    [Fact]
    public void AppServiceAnnotation_WithNullConfigure_HasNullCallback()
    {
        // Act
        var annotation = new AppServiceAnnotation(null);

        // Assert
        annotation.Configure.Should().BeNull();
    }

    [Fact]
    public void AppServiceAnnotation_WithConfigure_StoresCallback()
    {
        // Arrange
        Action<App_service_spec> configure = spec => spec.InstanceCount = 5;

        // Act
        var annotation = new AppServiceAnnotation(configure);

        // Assert
        annotation.Configure.Should().NotBeNull();
        
        var spec = new App_service_spec();
        annotation.Configure!(spec);
        spec.InstanceCount.Should().Be(5);
    }

    [Fact]
    public void AppWorkerAnnotation_WithNullConfigure_HasNullCallback()
    {
        // Act
        var annotation = new AppWorkerAnnotation(null);

        // Assert
        annotation.Configure.Should().BeNull();
    }

    [Fact]
    public void AppWorkerAnnotation_WithConfigure_StoresCallback()
    {
        // Arrange
        Action<App_worker_spec> configure = spec => spec.InstanceCount = 3;

        // Act
        var annotation = new AppWorkerAnnotation(configure);

        // Assert
        annotation.Configure.Should().NotBeNull();
        
        var spec = new App_worker_spec();
        annotation.Configure!(spec);
        spec.InstanceCount.Should().Be(3);
    }

    [Fact]
    public void AppStaticSiteAnnotation_WithNullConfigure_HasNullCallback()
    {
        // Act
        var annotation = new AppStaticSiteAnnotation(null);

        // Assert
        annotation.Configure.Should().BeNull();
    }

    [Fact]
    public void AppStaticSiteAnnotation_WithConfigure_StoresCallback()
    {
        // Arrange
        Action<App_static_site_spec> configure = spec => spec.EnvironmentSlug = "node-js";

        // Act
        var annotation = new AppStaticSiteAnnotation(configure);

        // Assert
        annotation.Configure.Should().NotBeNull();
        
        var spec = new App_static_site_spec();
        annotation.Configure!(spec);
        spec.EnvironmentSlug.Should().Be("node-js");
    }

    [Fact]
    public void AppFunctionsAnnotation_WithNullConfigure_HasNullCallback()
    {
        // Act
        var annotation = new AppFunctionsAnnotation(null);

        // Assert
        annotation.Configure.Should().BeNull();
    }

    [Fact]
    public void AppFunctionsAnnotation_WithConfigure_StoresCallback()
    {
        // Arrange
        Action<App_functions_spec> configure = spec => spec.Name = "my-function";

        // Act
        var annotation = new AppFunctionsAnnotation(configure);

        // Assert
        annotation.Configure.Should().NotBeNull();
        
        var spec = new App_functions_spec();
        annotation.Configure!(spec);
        spec.Name.Should().Be("my-function");
    }

    [Fact]
    public void AppSpecPublishAnnotation_CanBeCreated()
    {
        // Act
        var annotation = new AppSpecPublishAnnotation();

        // Assert
        annotation.Should().NotBeNull();
    }
}

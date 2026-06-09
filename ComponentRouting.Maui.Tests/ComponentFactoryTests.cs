using ComponentRouting.Maui.Ioc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class ComponentFactoryTests
{
    [Fact]
    public void CreateComponent_generic_delegates_to_GetRequiredService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestComponent>();
        using var provider = services.BuildServiceProvider();
        var factory = new ServiceProviderComponentFactory(provider);

        Assert.Same(
            provider.GetRequiredService<TestComponent>(),
            factory.CreateComponent<TestComponent>());
    }

    [Fact]
    public void CreateComponent_type_delegates_to_GetRequiredService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestComponent>();
        using var provider = services.BuildServiceProvider();
        var factory = new ServiceProviderComponentFactory(provider);

        Assert.Same(
            provider.GetRequiredService<TestComponent>(),
            factory.CreateComponent(typeof(TestComponent)));
    }

    [Fact]
    public void CreateComponent_type_throws_when_service_is_not_registered()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var factory = new ServiceProviderComponentFactory(provider);

        Assert.Throws<InvalidOperationException>(() => factory.CreateComponent(typeof(TestComponent)));
    }
}

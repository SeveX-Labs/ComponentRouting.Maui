using Microsoft.Extensions.DependencyInjection;
using ComponentRouting.Maui;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Ioc;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddComponentRoutingMaui_registers_normal_component_as_self_singleton()
    {
        var services = new ServiceCollection();

        services.AddComponentRoutingMaui(typeof(DependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<TestPageComponent>();
        var second = provider.GetRequiredService<TestPageComponent>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddComponentRoutingMaui_registers_direct_and_indirect_overlay_components_as_self_transient()
    {
        var services = new ServiceCollection();

        services.AddComponentRoutingMaui(typeof(DependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.NotSame(
            provider.GetRequiredService<TestOverlayComponent>(),
            provider.GetRequiredService<TestOverlayComponent>());
        Assert.NotSame(
            provider.GetRequiredService<TestIndirectOverlayComponent>(),
            provider.GetRequiredService<TestIndirectOverlayComponent>());
    }

    [Fact]
    public void AddComponentRoutingMaui_registers_direct_and_indirect_snackbar_components_as_self_transient()
    {
        var services = new ServiceCollection();

        services.AddComponentRoutingMaui(typeof(DependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.NotSame(
            provider.GetRequiredService<TestSnackbarComponent>(),
            provider.GetRequiredService<TestSnackbarComponent>());
        Assert.NotSame(
            provider.GetRequiredService<TestIndirectSnackbarComponent>(),
            provider.GetRequiredService<TestIndirectSnackbarComponent>());
    }
}

public class ComponentFactoryTests
{
    [Fact]
    public void CreateComponent_generic_delegates_to_GetRequiredService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestPageComponent>();
        using var provider = services.BuildServiceProvider();
        var factory = new ServiceProviderComponentFactory(provider);

        Assert.Same(
            provider.GetRequiredService<TestPageComponent>(),
            factory.CreateComponent<TestPageComponent>());
    }

    [Fact]
    public void CreateComponent_type_delegates_to_GetRequiredService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestPageComponent>();
        using var provider = services.BuildServiceProvider();
        var factory = new ServiceProviderComponentFactory(provider);

        Assert.Same(
            provider.GetRequiredService<TestPageComponent>(),
            factory.CreateComponent(typeof(TestPageComponent)));
    }

    [Fact]
    public void CreateComponent_type_throws_when_service_is_not_registered()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var factory = new ServiceProviderComponentFactory(provider);

        Assert.Throws<InvalidOperationException>(() => factory.CreateComponent(typeof(TestPageComponent)));
    }
}


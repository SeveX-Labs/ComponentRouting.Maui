using ComponentRouting.Maui.Ioc;
using ComponentRouting.Maui.Provider.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddComponentRoutingCore_registers_component_factory()
    {
        var services = new ServiceCollection();

        services.AddComponentRoutingCore(typeof(DependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<ServiceProviderComponentFactory>(provider.GetRequiredService<ComponentFactory>());
        Assert.IsType<RouterRuntimeLifecycle>(provider.GetRequiredService<RouterRuntimeLifecycle>());
    }

    [Fact]
    public void AddComponentRoutingCore_registers_concrete_components_as_self_singleton()
    {
        var services = new ServiceCollection();

        services.AddComponentRoutingCore(typeof(DependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<TestComponent>(),
            provider.GetRequiredService<TestComponent>());
    }

    [Fact]
    public void AddComponentRoutingCore_registers_first_concrete_provider_and_router_services()
    {
        var services = new ServiceCollection();

        services.AddComponentRoutingCore(typeof(DependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<TestCatalogProvider>(provider.GetRequiredService<CatalogProvider>());
        Assert.IsType<TestLocaleProvider>(provider.GetRequiredService<LocaleProvider>());
        Assert.IsType<TestRouter>(provider.GetRequiredService<Router>());
    }
}

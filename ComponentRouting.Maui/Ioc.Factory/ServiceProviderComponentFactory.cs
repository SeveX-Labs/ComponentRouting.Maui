using Microsoft.Extensions.DependencyInjection;
using System;

namespace ComponentRouting.Maui.Ioc;

public class ServiceProviderComponentFactory : ComponentFactory
{
    private IServiceProvider ServiceProvider { get; }

    public ServiceProviderComponentFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public C CreateComponent<C>() where C : Component
    {
        return ServiceProvider.GetRequiredService<C>();
    }

    public Component CreateComponent(Type componentType)
    {
        return (Component)ServiceProvider.GetRequiredService(componentType);
    }

    public Component CreateComponent(string componentTypeName)
    {
        var componentType = Type.GetType(componentTypeName, throwOnError: true)!;
        return CreateComponent(componentType);
    }

    public void DisposeComponent(Component component)
    {
        component.Dispose();
    }
}

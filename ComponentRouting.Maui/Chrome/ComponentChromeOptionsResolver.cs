using ComponentRouting.Maui.Abstraction.Core;

namespace ComponentRouting.Maui.Chrome;

public sealed class ComponentChromeOptionsResolver
{
    private readonly ComponentChromeConfiguration configuration;

    public ComponentChromeOptionsResolver(ComponentChromeConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public ComponentChromeOptions Resolve(Component component, ComponentPresentationKind presentationKind)
    {
        var options = configuration.LibraryDefaults
            .Merge(configuration.GlobalDefaults)
            .Merge(GetPresentationDefaults(presentationKind));

        return configuration.ComponentOverrides.TryGetValue(component.GetType(), out var componentOptions)
            ? options.Merge(componentOptions)
            : options;
    }

    private ComponentChromeOptions GetPresentationDefaults(ComponentPresentationKind presentationKind)
    {
        return presentationKind switch
        {
            ComponentPresentationKind.Pushable => configuration.PushableDefaults,
            ComponentPresentationKind.Modal => configuration.ModalDefaults,
            ComponentPresentationKind.FullscreenModal => configuration.FullscreenModalDefaults,
            _ => configuration.PageDefaults
        };
    }
}

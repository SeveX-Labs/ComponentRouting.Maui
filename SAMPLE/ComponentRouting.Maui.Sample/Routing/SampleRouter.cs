using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Routing;
using ComponentRouting.Maui.Sample.Components.Root;
using ComponentRouting.Maui.Service.Core;

namespace ComponentRouting.Maui.Sample.Routing;

public sealed class SampleRouter : AbstractRouter
{
    public SampleRouter(
        ComponentFactory componentFactory,
        CatalogProvider catalogProvider,
        SafeAreaInsetsService safeAreaInsetsService)
        : base(componentFactory, catalogProvider, safeAreaInsetsService)
    {
    }

    public override bool IsSafeAreaInsetsApplyiable => false;

    public override RootComponent RootComponent =>
        ComponentFactory.CreateComponent<SampleTabbedRootComponent>();

    protected override bool CanNavigateBack(Component component)
    {
        return true;
    }

    protected override Task<bool> PresentComponent<TState>(Component component, TState input)
    {
        // Custom sample routing rules could be added here before deferring to library behavior.
        return base.PresentComponent(component, input);
    }
}

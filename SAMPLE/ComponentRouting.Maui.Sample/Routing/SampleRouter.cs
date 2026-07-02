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

    protected override RootComponent RootComponent =>
        ComponentFactory.CreateComponent<SampleModeRootComponent>();

    protected override bool CanNavigateBack(Component component)
    {
        return true;
    }

    protected override Task<bool> PresentComponent<TState>(Component component, TState input)
    {
        // Custom sample routing rules could be added here before deferring to library behavior.
        return base.PresentComponent(component, input);
    }

    protected override Task OnRuntimeResetAsync(RouterRuntimeResetOptions options)
    {
        // Invoked by ResetRuntimeAsync(...) after the runtime state has been cleared (live reset).
        // App-specific reset behavior (e.g. restore orientation, reset transient UI state) goes here.
        System.Diagnostics.Debug.WriteLine($"Runtime reset completed: {options.Reason}");
        return base.OnRuntimeResetAsync(options);
    }
}

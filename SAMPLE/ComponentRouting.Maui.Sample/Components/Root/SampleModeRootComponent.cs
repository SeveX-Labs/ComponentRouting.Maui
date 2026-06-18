using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components.Modals;
using ComponentRouting.Maui.Sample.Presenters.Root;

namespace ComponentRouting.Maui.Sample.Components.Root;

public sealed class SampleModeRootComponent : RootComponent
{
    private readonly Router router;

    public SampleModeRootComponent(Router router)
    {
        this.router = router;
    }

    protected override Presenter CreatePresenter()
    {
        return new SampleModePage();
    }

    protected override Task Configure(bool state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(bool state)
    {
        ((SampleModePage)Presenter!).Configure(
            OpenTabbedRoot,
            OpenFlyoutRoot,
            OpenNormalModal,
            OpenFullscreenModal);
        return Task.CompletedTask;
    }

    private Task OpenTabbedRoot()
    {
        _ = router.PresentComponent<SampleTabbedRootComponent, bool, bool>(true);
        return Task.CompletedTask;
    }

    private Task OpenFlyoutRoot()
    {
        _ = router.PresentComponent<SampleFlyoutRootComponent, bool, bool>(true);
        return Task.CompletedTask;
    }

    private async Task OpenNormalModal()
    {
        var result = await router.PresentComponent<DetailsComponent, DetailsComponent.ComponentState, bool>(
            new DetailsComponent.ComponentState(
                "Normal modal",
                "This modal uses ModalPageComponent and normal modal chrome."));
        ((SampleModePage)Presenter!).SetLastResult($"Normal modal result: {result}");
    }

    private async Task OpenFullscreenModal()
    {
        var result = await router.PresentComponent<FullscreenChromeDemoComponent, FullscreenChromeDemoComponent.ComponentState, bool>(
            new FullscreenChromeDemoComponent.ComponentState(
                "Fullscreen modal",
                "This modal uses FullscreenModalPageComponent and platform chrome defaults."));
        ((SampleModePage)Presenter!).SetLastResult($"Fullscreen modal result: {result}");
    }
}

using ComponentRouting.Maui.Abstraction;
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
        ((SampleModePage)Presenter!).Configure(OpenTabbedRoot, OpenFlyoutRoot);
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
}

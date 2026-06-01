using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Presenters.Flyout;

namespace ComponentRouting.Maui.Sample.Components.Flyout;

public sealed class SampleFlyoutSettingsComponent : SampleFlyoutComponent<bool>
{
    protected override Task Configure(bool state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(bool state)
    {
        ((SampleFlyoutSettingsPage)Presenter!).SetDescription(
            "Settings is another flyout destination backed by a routed FlyoutComponent.");
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}

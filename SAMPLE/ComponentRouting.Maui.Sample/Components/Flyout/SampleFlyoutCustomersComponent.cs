using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Presenters.Flyout;

namespace ComponentRouting.Maui.Sample.Components.Flyout;

public sealed class SampleFlyoutCustomersComponent : SampleFlyoutComponent<bool>
{
    protected override Task Configure(bool state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(bool state)
    {
        ((SampleFlyoutCustomersPage)Presenter!).SetDescription(
            "Customers lives inside the flyout root and is selected through a FlyoutComponent.");
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}

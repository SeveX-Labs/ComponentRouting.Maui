using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Presenters.Tabs;

namespace ComponentRouting.Maui.Sample.Components.Tabs;

public sealed class AboutComponent : SampleTabComponent<bool>
{
    protected override Task Configure(bool state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(bool state)
    {
        ((AboutPage)Presenter!).SetDescription(
            "This tab uses an existing presenter inserted into a TabComponent. It keeps the sample focused on routing primitives.");
        return Task.CompletedTask;
    }
    
    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}

using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Presenters.Modals;

namespace ComponentRouting.Maui.Sample.Components.Modals;

public sealed class FullscreenChromeDemoComponent
    : FullscreenModalPageComponent<FullscreenChromeDemoComponent.ComponentState, bool>
{
    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new FullscreenChromeDemoPage();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((FullscreenChromeDemoPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            () => CompletionSource?.TrySetResult(true));
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}

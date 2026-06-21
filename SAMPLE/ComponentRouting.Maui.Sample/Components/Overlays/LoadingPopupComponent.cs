using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Presenters.Overlays;

namespace ComponentRouting.Maui.Sample.Components.Overlays;

public sealed class LoadingPopupComponent : OverlayComponent<LoadingPopupComponent.ComponentState, bool>
{
    public readonly record struct ComponentState(string Title, string Message);

    public override View? Backdrop => Presenter as View;

    protected override Presenter CreatePresenter()
    {
        return new LoadingPopupPresenter();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((LoadingPopupPresenter)Presenter!).Initialize(
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

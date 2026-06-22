using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components;
using ComponentRouting.Maui.Sample.Components.Overlays;
using ComponentRouting.Maui.Sample.Components.Snackbars;
using ComponentRouting.Maui.Sample.Presenters.Pushables;

namespace ComponentRouting.Maui.Sample.Components.Pushables;

public sealed class OverlayMatrixPushComponent : PushableComponent<OverlayMatrixPushComponent.ComponentState, bool>
{
    private readonly Router router;
    private ComponentState state;

    public OverlayMatrixPushComponent(Router router)
    {
        this.router = router;
    }

    public readonly record struct ComponentState(string Title, string Message, string SurfaceName);

    protected override Presenter CreatePresenter()
    {
        return new OverlayMatrixPushPage();
    }

    protected override Task Initialize(ComponentState state)
    {
        this.state = state;
        ((OverlayMatrixPushPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            state.SurfaceName,
            ShowOverlay,
            ShowSnackbar,
            ShowMutablePopup,
            CloseOverlay,
            Close);
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    public override void HandleBackTapped()
    {
        _ = Close();
    }

    private Task ShowOverlay()
    {
        OverlayMatrixTraceLog.Click(state.SurfaceName == "push inside modal" ? "PushInsideModal" : "Push", $"Show overlay from {state.SurfaceName}", this);
        if (router.GetMountedOverlayComponents<LoadingPopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<LoadingPopupComponent, LoadingPopupComponent.ComponentState, bool>(
                new LoadingPopupComponent.ComponentState(
                    $"Overlay from {state.SurfaceName}",
                    "This intentionally uses the current SampleApp overlay host."));
        }

        return Task.CompletedTask;
    }

    private Task ShowSnackbar()
    {
        OverlayMatrixTraceLog.Click(state.SurfaceName == "push inside modal" ? "PushInsideModal" : "Push", $"Show snackbar from {state.SurfaceName}", this);
        _ = router.PresentComponent<InfoSnackbarComponent, SnackbarConfiguration, bool>(
            new SnackbarConfiguration($"Snackbar from {state.SurfaceName}", false, 0));
        return Task.CompletedTask;
    }

    private Task ShowMutablePopup()
    {
        var context = state.SurfaceName == "push inside modal" ? "PushInsideModal" : "Push";
        OverlayMatrixTraceLog.Click(context, $"Show mutable popup from {state.SurfaceName}", this);
        if (router.GetMountedOverlayComponents<MutablePopupComponent>().Count == 0)
        {
            _ = router.PresentComponent<MutablePopupComponent, MutablePopupComponent.ComponentState, bool>(
                new MutablePopupComponent.ComponentState(
                    $"Mutable popup from {state.SurfaceName}",
                    "Initial text. Use the buttons inside this popup to update or unpresent it through mounted overlay lookup."));
        }

        return Task.CompletedTask;
    }

    private Task CloseOverlay()
    {
        OverlayMatrixTraceLog.Click(state.SurfaceName == "push inside modal" ? "PushInsideModal" : "Push", $"Close overlay from {state.SurfaceName}", this);
        router.GetMountedOverlayComponent<LoadingPopupComponent>()?.Unpresent();
        return Task.CompletedTask;
    }

    private async Task Close()
    {
        OverlayMatrixTraceLog.Click(state.SurfaceName == "push inside modal" ? "PushInsideModal" : "Push", $"Close {state.SurfaceName}", this);
        await router.DismissComponent<OverlayMatrixPushComponent, ComponentState, bool>(true);
        CompletionSource?.TrySetResult(true);
    }
}

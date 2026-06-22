using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Presenters.Overlays;

namespace ComponentRouting.Maui.Sample.Components.Overlays;

public sealed class MutablePopupComponent : OverlayComponent<MutablePopupComponent.ComponentState, bool>
{
    private readonly Router router;
    private int updateCount;

    public readonly record struct ComponentState(string Title, string Message);

    public MutablePopupComponent(Router router)
    {
        this.router = router;
    }

    public override View? Backdrop => Presenter as View;

    public void UpdateMessage(string message)
    {
        ((MutablePopupPresenter?)Presenter)?.UpdateMessage(message);
    }

    protected override Presenter CreatePresenter()
    {
        return new MutablePopupPresenter();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        updateCount = 0;
        ((MutablePopupPresenter)Presenter!).Initialize(
            state.Title,
            state.Message,
            UpdateMountedPopup,
            UnpresentMountedPopup);
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    private void UpdateMountedPopup()
    {
        OverlayMatrixTraceLog.Click("MutablePopup", "Update via GetMountedOverlayComponent", this);
        var popup = router.GetMountedOverlayComponent<MutablePopupComponent>();
        popup?.UpdateMessage($"Updated via mounted overlay lookup #{++updateCount}.");
    }

    private void UnpresentMountedPopup()
    {
        OverlayMatrixTraceLog.Click("MutablePopup", "Unpresent via GetMountedOverlayComponent", this);
        router.GetMountedOverlayComponent<MutablePopupComponent>()?.Unpresent();
    }
}

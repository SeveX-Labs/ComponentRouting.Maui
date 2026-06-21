using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Pushables;

public partial class OverlayMatrixPushPage : PushablePresenter, OverlayHost
{
    private Func<Task>? showOverlay;
    private Func<Task>? showSnackbar;
    private Func<Task>? closeOverlay;
    private Func<Task>? close;

    public OverlayMatrixPushPage()
    {
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void Initialize(
        string title,
        string message,
        string surfaceName,
        Func<Task> showOverlay,
        Func<Task> showSnackbar,
        Func<Task> closeOverlay,
        Func<Task> close)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        SurfaceLabel.Text = $"Surface: {surfaceName}";
        this.showOverlay = showOverlay;
        this.showSnackbar = showSnackbar;
        this.closeOverlay = closeOverlay;
        this.close = close;
    }

    private async void HandleShowOverlayClicked(object? sender, EventArgs e) => await Invoke(showOverlay);
    private async void HandleShowSnackbarClicked(object? sender, EventArgs e) => await Invoke(showSnackbar);
    private async void HandleCloseOverlayClicked(object? sender, EventArgs e) => await Invoke(closeOverlay);
    private async void HandleCloseClicked(object? sender, EventArgs e) => await Invoke(close);

    private static async Task Invoke(Func<Task>? action)
    {
        if (action is not null)
            await action();
    }
}

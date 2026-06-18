using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Root;

public partial class SampleModePage : ContentPage, Presenter
{
    private Func<Task>? openTabbedRoot;
    private Func<Task>? openFlyoutRoot;
    private Func<Task>? openNormalModal;
    private Func<Task>? openFullscreenModal;

    public SampleModePage()
    {
        InitializeComponent();
    }

    public void Configure(
        Func<Task> openTabbedRoot,
        Func<Task> openFlyoutRoot,
        Func<Task> openNormalModal,
        Func<Task> openFullscreenModal)
    {
        this.openTabbedRoot = openTabbedRoot;
        this.openFlyoutRoot = openFlyoutRoot;
        this.openNormalModal = openNormalModal;
        this.openFullscreenModal = openFullscreenModal;
    }

    public void SetLastResult(string text)
    {
        LastResultLabel.Text = text;
    }

    private async void HandleTabbedRootClicked(object? sender, EventArgs e) => await Invoke(openTabbedRoot);
    private async void HandleFlyoutRootClicked(object? sender, EventArgs e) => await Invoke(openFlyoutRoot);
    private async void HandleOpenNormalModalClicked(object? sender, EventArgs e) => await Invoke(openNormalModal);
    private async void HandleOpenFullscreenModalClicked(object? sender, EventArgs e) => await Invoke(openFullscreenModal);

    private static async Task Invoke(Func<Task>? action)
    {
        if (action is not null)
            await action();
    }
}

using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Root;

public partial class SampleModePage : ContentPage, Presenter
{
    private Func<Task>? openTabbedRoot;
    private Func<Task>? openFlyoutRoot;

    public SampleModePage()
    {
        InitializeComponent();
    }

    public void Configure(Func<Task> openTabbedRoot, Func<Task> openFlyoutRoot)
    {
        this.openTabbedRoot = openTabbedRoot;
        this.openFlyoutRoot = openFlyoutRoot;
    }

    private async void HandleTabbedRootClicked(object? sender, EventArgs e) => await Invoke(openTabbedRoot);
    private async void HandleFlyoutRootClicked(object? sender, EventArgs e) => await Invoke(openFlyoutRoot);

    private static async Task Invoke(Func<Task>? action)
    {
        if (action is not null)
            await action();
    }
}

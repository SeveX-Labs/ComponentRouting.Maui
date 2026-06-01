using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Flyout;

public partial class SampleFlyoutSettingsPage : ContentPage, Presenter, OverlayHost
{
    private readonly Action openFlyout;

    public SampleFlyoutSettingsPage(Action openFlyout)
    {
        this.openFlyout = openFlyout;
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void SetDescription(string description)
    {
        DescriptionLabel.Text = description;
    }

    private void HandleMenuClicked(object? sender, EventArgs e) => openFlyout();
}

using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Sample.Presenters.Tabs;

public partial class AboutPage : ContentPage, Presenter, OverlayHost
{
    public AboutPage()
    {
        InitializeComponent();
    }

    public AbsoluteLayout? OverlayContainer => OverlayLayer;

    public void SetDescription(string description)
    {
        DescriptionLabel.Text = description;
    }
}

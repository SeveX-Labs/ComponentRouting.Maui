using ComponentRouting.Maui.Sample.Models;

namespace ComponentRouting.Maui.Sample.Presenters.Root;

public sealed class SampleFlyoutMenuPage : ContentPage
{
    private readonly Dictionary<SampleFlyoutPageKey, Button> buttons = new();

    public SampleFlyoutMenuPage()
    {
        Title = "Menu";

        var layout = new VerticalStackLayout
        {
            Padding = new Thickness(20, 48, 20, 20),
            Spacing = 10
        };

        layout.Children.Add(new Label
        {
            Text = "Flyout root",
            FontAttributes = FontAttributes.Bold,
            FontSize = 22
        });

        AddMenuButton(layout, SampleFlyoutPageKey.Home, "Home");
        AddMenuButton(layout, SampleFlyoutPageKey.Customers, "Customers");
        AddMenuButton(layout, SampleFlyoutPageKey.Settings, "Settings");

        Content = new ScrollView { Content = layout };
        SetSelectedPage(SampleFlyoutPageKey.Home);
    }

    public event EventHandler<SampleFlyoutPageKey>? SelectionRequested;

    public void SetSelectedPage(SampleFlyoutPageKey key)
    {
        foreach (var pair in buttons)
        {
            var isSelected = pair.Key == key;
            pair.Value.BackgroundColor = isSelected ? Color.FromArgb("#512BD4") : Colors.Transparent;
            pair.Value.TextColor = isSelected ? Colors.White : Color.FromArgb("#242424");
            pair.Value.BorderColor = isSelected ? Color.FromArgb("#512BD4") : Color.FromArgb("#C8C8C8");
            pair.Value.BorderWidth = 1;
        }
    }

    private void AddMenuButton(Layout layout, SampleFlyoutPageKey key, string text)
    {
        var button = new Button
        {
            Text = text,
            HorizontalOptions = LayoutOptions.Fill,
            CornerRadius = 6
        };
        button.Clicked += (_, _) => SelectionRequested?.Invoke(this, key);
        buttons[key] = button;
        layout.Children.Add(button);
    }
}

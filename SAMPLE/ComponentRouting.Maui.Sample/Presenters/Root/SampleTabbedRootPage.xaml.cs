using ComponentRouting.Maui.Model.Core;
using ComponentRouting.Maui.Sample.Components.Tabs;
using ComponentRouting.Maui.Sample.Presenters.Tabs;

namespace ComponentRouting.Maui.Sample.Presenters.Root;

public partial class SampleTabbedRootPage : TabbedPage, Presenter, OverlayHost
{
    private readonly Router router;
    private bool isReady;

    public SampleTabbedRootPage(Router router)
    {
        this.router = router;
        InitializeComponent();

        HomePage = new HomePage { Title = "Home" };
        AboutPage = new AboutPage { Title = "About" };
        Children.Add(new NavigationPage(HomePage) { Title = "Home" });
        Children.Add(new NavigationPage(AboutPage) { Title = "About" });
        CurrentPageChanged += HandleCurrentPageChanged;
    }

    public HomePage HomePage { get; }
    public AboutPage AboutPage { get; }
    public HomeComponent? HomeComponent { get; set; }
    public AboutComponent? AboutComponent { get; set; }

    public AbsoluteLayout? OverlayContainer
    {
        get
        {
            if (CurrentPage is NavigationPage { RootPage: OverlayHost host })
                return host.OverlayContainer;

            return null;
        }
    }

    public void SelectInitialTab()
    {
        isReady = true;
        CurrentPage = Children[0];
        _ = router.PresentComponent<HomeComponent, bool, bool>(true);
    }

    private void HandleCurrentPageChanged(object? sender, EventArgs e)
    {
        if (!isReady) return;

        if (CurrentPage is NavigationPage homeNavigationPage && ReferenceEquals(homeNavigationPage.RootPage, HomePage))
            _ = router.PresentComponent<HomeComponent, bool, bool>(true);
        else if (CurrentPage is NavigationPage aboutNavigationPage && ReferenceEquals(aboutNavigationPage.RootPage, AboutPage))
            _ = router.PresentComponent<AboutComponent, bool, bool>(true);
    }
}

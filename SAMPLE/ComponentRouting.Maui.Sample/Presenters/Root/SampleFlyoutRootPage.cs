using ComponentRouting.Maui.Model.Core;
using ComponentRouting.Maui.Sample.Components.Flyout;
using ComponentRouting.Maui.Sample.Models;
using ComponentRouting.Maui.Sample.Presenters.Flyout;

namespace ComponentRouting.Maui.Sample.Presenters.Root;

public sealed class SampleFlyoutRootPage : FlyoutPage, Presenter, OverlayHost
{
    private readonly Dictionary<SampleFlyoutPageKey, NavigationPage> navigationPages;
    private readonly SampleFlyoutMenuPage menuPage;
    private readonly Router router;
    private bool isReady;

    public SampleFlyoutRootPage(Router router)
    {
        this.router = router;

        HomePage = new SampleFlyoutHomePage(OpenFlyout) { Title = "Home" };
        CustomersPage = new SampleFlyoutCustomersPage(OpenFlyout) { Title = "Customers" };
        SettingsPage = new SampleFlyoutSettingsPage(OpenFlyout) { Title = "Settings" };

        navigationPages = new Dictionary<SampleFlyoutPageKey, NavigationPage>
        {
            [SampleFlyoutPageKey.Home] = CreateNavigationPage(HomePage),
            [SampleFlyoutPageKey.Customers] = CreateNavigationPage(CustomersPage),
            [SampleFlyoutPageKey.Settings] = CreateNavigationPage(SettingsPage)
        };

        menuPage = new SampleFlyoutMenuPage();
        menuPage.SelectionRequested += HandleSelectionRequested;

        Flyout = menuPage;
        Detail = navigationPages[SampleFlyoutPageKey.Home];
    }

    public SampleFlyoutHomePage HomePage { get; }
    public SampleFlyoutCustomersPage CustomersPage { get; }
    public SampleFlyoutSettingsPage SettingsPage { get; }
    public SampleFlyoutHomeComponent? HomeComponent { get; set; }
    public SampleFlyoutCustomersComponent? CustomersComponent { get; set; }
    public SampleFlyoutSettingsComponent? SettingsComponent { get; set; }

    public AbsoluteLayout? OverlayContainer
    {
        get
        {
            if (Detail is NavigationPage { RootPage: OverlayHost host })
                return host.OverlayContainer;

            return null;
        }
    }

    public void SelectInitialPage()
    {
        isReady = true;
        SelectPage(SampleFlyoutPageKey.Home);
    }

    private void HandleSelectionRequested(object? sender, SampleFlyoutPageKey key)
    {
        if (!isReady) return;

        SelectPage(key);
    }

    private void SelectPage(SampleFlyoutPageKey key)
    {
        Detail = navigationPages[key];
        menuPage.SetSelectedPage(key);
        IsPresented = false;

        switch (key)
        {
            case SampleFlyoutPageKey.Home:
                _ = router.PresentComponent<SampleFlyoutHomeComponent, bool, bool>(true);
                break;
            case SampleFlyoutPageKey.Customers:
                _ = router.PresentComponent<SampleFlyoutCustomersComponent, bool, bool>(true);
                break;
            case SampleFlyoutPageKey.Settings:
                _ = router.PresentComponent<SampleFlyoutSettingsComponent, bool, bool>(true);
                break;
        }
    }

    private void OpenFlyout()
    {
        IsPresented = true;
    }

    private static NavigationPage CreateNavigationPage(Page page)
    {
        return new NavigationPage(page)
        {
            Title = page.Title,
            BarTextColor = Colors.White
        };
    }
}

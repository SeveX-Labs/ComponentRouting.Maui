using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ComponentRouting.Maui.Abstraction.Core;
using ComponentRouting.Maui.Chrome;
using Microsoft.Maui.Controls;

#if IOS
using IosPage = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page;
using IosPlatform = Microsoft.Maui.Controls.PlatformConfiguration.iOS;
using IosModalPresentationStyle = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle;
#endif

namespace ComponentRouting.Maui.Routing;

internal static class ComponentModalPresentationPolicyApplier
{
    public static void Apply(
        Component component,
        ComponentPresentationKind presentationKind,
        Page? mountablePage = null)
    {
        if (presentationKind is not ComponentPresentationKind.FullscreenModal)
            return;

#if IOS
        var appliedPages = new HashSet<int>();

        ApplyToPage(mountablePage, appliedPages);

        if (component.Presenter is Page presenterPage)
            ApplyToPage(presenterPage, appliedPages);

        if (component is NavigationComponent { Navigation: not null } navigationComponent)
            ApplyToPage(navigationComponent.Navigation, appliedPages);
#endif
    }

#if IOS
    private static void ApplyToPage(Page? page, HashSet<int> appliedPages)
    {
        if (page is null || !appliedPages.Add(RuntimeHelpers.GetHashCode(page)))
            return;

        IosPage.SetModalPresentationStyle(page.On<IosPlatform>(), IosModalPresentationStyle.FullScreen);
    }
#endif
}

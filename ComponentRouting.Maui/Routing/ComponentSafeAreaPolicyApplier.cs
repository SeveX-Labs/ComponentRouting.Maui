using System.Collections.Generic;
using ComponentRouting.Maui.Abstraction.Core;
using ComponentRouting.Maui.Chrome;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace ComponentRouting.Maui.Routing;

internal static class ComponentSafeAreaPolicyApplier
{
    private static readonly string SafeAreaEdgesPropertyName = nameof(ContentPage.SafeAreaEdges);

    public static void Apply(Component component, ComponentPresentationKind presentationKind, Page? mountablePage = null)
    {
        var safeAreaEdges = GetSafeAreaEdges(presentationKind);
        var appliedPages = new HashSet<int>();

        ApplyToPage(mountablePage, safeAreaEdges, appliedPages);

        if (component.Presenter is Page presenterPage)
            ApplyToPage(presenterPage, safeAreaEdges, appliedPages);

        if (component is NavigationComponent { Navigation: not null } navigationComponent)
            ApplyToPage(navigationComponent.Navigation, safeAreaEdges, appliedPages);
    }

    private static SafeAreaEdges GetSafeAreaEdges(ComponentPresentationKind presentationKind)
    {
        return presentationKind is ComponentPresentationKind.FullscreenModal
            ? new SafeAreaEdges(SafeAreaRegions.None)
            : new SafeAreaEdges(SafeAreaRegions.Container);
    }

    private static void ApplyToPage(Page? page, SafeAreaEdges safeAreaEdges, HashSet<int> appliedPages)
    {
        if (page is null || !appliedPages.Add(page.GetHashCode()))
            return;

        var property = page.GetType().GetProperty(SafeAreaEdgesPropertyName);
        if (property is null || !property.CanWrite || !property.PropertyType.IsInstanceOfType(safeAreaEdges))
            return;

        property.SetValue(page, safeAreaEdges);
    }
}

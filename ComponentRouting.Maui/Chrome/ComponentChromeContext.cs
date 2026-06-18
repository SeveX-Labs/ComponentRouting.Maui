using ComponentRouting.Maui.Abstraction.Core;
using Microsoft.Maui.Controls;

namespace ComponentRouting.Maui.Chrome;

public sealed class ComponentChromeContext
{
    public ComponentChromeContext(
        string source,
        Component component,
        ComponentPresentationKind presentationKind,
        ComponentChromeOptions options,
        Page? mountablePage = null,
        INavigation? navigation = null)
    {
        Source = source;
        Component = component;
        PresentationKind = presentationKind;
        Options = options;
        MountablePage = mountablePage;
        Navigation = navigation;
    }

    public string Source { get; }
    public Component Component { get; }
    public ComponentPresentationKind PresentationKind { get; }
    public ComponentChromeOptions Options { get; }
    public Page? MountablePage { get; }
    public INavigation? Navigation { get; }
}

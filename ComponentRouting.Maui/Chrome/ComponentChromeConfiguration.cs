using System;
using System.Collections.Generic;

namespace ComponentRouting.Maui.Chrome;

public sealed class ComponentChromeConfiguration
{
    public ComponentChromeOptions LibraryDefaults { get; set; } = new();
    public ComponentChromeOptions GlobalDefaults { get; set; } = new();
    public ComponentChromeOptions PageDefaults { get; set; } = new();
    public ComponentChromeOptions PushableDefaults { get; set; } = new();
    public ComponentChromeOptions ModalDefaults { get; set; } = new();
    public ComponentChromeOptions FullscreenModalDefaults { get; set; } = new();
    public IDictionary<Type, ComponentChromeOptions> ComponentOverrides { get; } = new Dictionary<Type, ComponentChromeOptions>();
}

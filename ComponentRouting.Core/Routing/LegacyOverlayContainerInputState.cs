using System;

namespace ComponentRouting.Maui.Routing;

internal readonly record struct LegacyOverlayContainerInputState(
    bool IsVisible,
    bool InputTransparent)
{
    public static LegacyOverlayContainerInputState FromChildCount(int childCount)
    {
        if (childCount < 0)
            throw new ArgumentOutOfRangeException(nameof(childCount));

        var hasChildren = childCount > 0;
        return new LegacyOverlayContainerInputState(
            IsVisible: hasChildren,
            InputTransparent: !hasChildren);
    }
}

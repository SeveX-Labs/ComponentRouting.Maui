using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComponentRouting.Maui.Routing;

internal sealed class ReferenceEqualityComparer<TComponent> : IEqualityComparer<TComponent>
    where TComponent: Component
{
    public static ReferenceEqualityComparer<TComponent> Instance { get; } = new();

    private ReferenceEqualityComparer()
    {
    }

    public bool Equals(TComponent? x, TComponent? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(TComponent obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}

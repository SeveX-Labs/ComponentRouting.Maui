using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComponentRouting.Maui.Routing;

internal sealed class ReferenceEqualityComparer : IEqualityComparer<Component>
{
    public static ReferenceEqualityComparer Instance { get; } = new();

    private ReferenceEqualityComparer()
    {
    }

    public bool Equals(Component? x, Component? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(Component obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}

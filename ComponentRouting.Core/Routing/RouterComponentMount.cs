using System;

namespace ComponentRouting.Maui.Routing;

internal sealed class RouterComponentMount<TMount, TOwner>
    where TMount : class
    where TOwner : class
{
    public RouterComponentMount(TMount mount, TOwner owner)
    {
        Mount = mount ?? throw new ArgumentNullException(nameof(mount));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public TMount Mount { get; }

    public TOwner Owner { get; }
}

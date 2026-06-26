using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface IRouterShutdownAwareComponent
{
    ValueTask OnRouterShutdownAsync(RouterShutdownContext context);
}

using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface IRouterShutdownAwarePresenter
{
    ValueTask OnRouterShutdownAsync(RouterShutdownContext context);
}

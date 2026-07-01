using System;

namespace ComponentRouting.Maui.Exceptions
{
    public enum RouterError
    {
        ApplicationCurrentIsNull,
        NoApplicationWindows,
        WindowPageIsNull,

        MissingPresenter,
        PresenterIsNotPage,

        NavigationNotAvailable,

        EmptyNavigationStack,
        PresenterIsNotOnTopNavigationStack,
        PresenterIsNotOnTopModalStack,

        PresentMethodNotFound,
        PresentMethodInvokeFailed,

        ComponentPresentationFailed,
        ComponentDismissalNotSupported,

        RouterIsShuttingDown,

        ComponentAlreadyPresented
    }

    public class RouterException : Exception
    {
        public RouterError Error { get; }
        public Component? Component { get; }

        public RouterException(RouterError error, Component? component = null, Exception? innerException = null)
            : base(BuildMessage(error, component), innerException)
        {
            Error = error;
            Component = component;
        }

        private static string BuildMessage(RouterError error, Component? component)
        {
            var comp = component is null ? "" : $" | Component={component.GetType().Name}";
            var detail = error == RouterError.ComponentAlreadyPresented
                ? ". The component already has an active or pending presentation. Complete its result before presenting it again, or register it with a transient lifetime if concurrent instances are required."
                : "";
            return $"[Router] {error}{detail}{comp}";
        }
    }
}

using System;

namespace ComponentRouting.Maui.Exceptions
{
    public enum ComponentError
    {
        MissingState,
        GenericError
    }

    public class ComponentException : Exception
    {
        public ComponentError Error { get; }

        public ComponentException(ComponentError error)
        {
            Error = error;
        }
    }
}

using System;

namespace SailwindFastForward
{
    internal static class SaveErrorBoundary
    {
        internal static Exception Handle(Exception error, Action cancel)
        {
            if (error == null) return null;
            // Cleanup must never replace or suppress the original save failure.
            try { cancel?.Invoke(); }
            catch { }
            return error;
        }
    }
}

namespace SailwindFastForward
{
    // Permit only the busy period belonging to a timer-triggered autosave.
    internal sealed class AutosaveState
    {
        private bool inAutosave;
        private bool continueWhileBusy;

        internal bool ShouldCancelSave => !inAutosave || !continueWhileBusy;

        internal void Begin(bool keepFastForward)
        {
            inAutosave = true;
            continueWhileBusy = keepFastForward;
        }

        internal void End(bool busy)
        {
            inAutosave = false;
            continueWhileBusy &= busy;
        }

        internal bool BlocksBusySave(bool busy)
        {
            if (!busy) continueWhileBusy = false;
            return busy && !continueWhileBusy;
        }

        internal void Reset()
        {
            inAutosave = false;
            continueWhileBusy = false;
        }
    }
}

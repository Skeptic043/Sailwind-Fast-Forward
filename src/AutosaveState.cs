namespace SailwindFastForward
{
    // Permit only the busy period belonging to a timer-triggered autosave.
    internal sealed class AutosaveState
    {
        private enum Phase { Idle, AwaitingPrefix, Saving, WaitingForCompletion }
        private Phase phase;

        // The wrapper grants exactly one synchronous prefix exemption.
        // A manual/nested save consumes no reusable permission and fails closed.
        internal bool TryEnterSave()
        {
            if (phase == Phase.AwaitingPrefix)
            {
                phase = Phase.Saving;
                return true;
            }
            Reset();
            return false;
        }

        internal void Begin(bool keepFastForward)
        {
            phase = keepFastForward && phase == Phase.Idle ? Phase.AwaitingPrefix : Phase.Idle;
        }

        internal void End(bool busy)
        {
            phase = phase == Phase.Saving && busy ? Phase.WaitingForCompletion : Phase.Idle;
        }

        internal bool BlocksBusySave(bool busy, bool cancelOnAutosave = false)
        {
            if (!busy || cancelOnAutosave) Reset();
            return busy && phase != Phase.WaitingForCompletion;
        }

        internal void Reset()
        {
            phase = Phase.Idle;
        }
    }
}

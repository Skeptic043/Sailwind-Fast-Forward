namespace SailwindFastForward
{
    // Distinguish a timer autosave from a manual save covered by an owned hold.
    internal sealed class AutosaveState
    {
        private enum Phase { Idle, AwaitingPrefix, AwaitingCancelledPrefix, Saving, WaitingForCompletion, ManualHoldSave }
        private Phase phase;

        // A timer wrapper grants exactly one synchronous prefix exemption.
        // Manual continuation requires a fresh owned hold and an idle save system.
        internal bool TryEnterSave(bool allowManualHold = false, bool busy = false)
        {
            if (phase == Phase.AwaitingPrefix)
            {
                phase = Phase.Saving;
                return true;
            }
            if (phase == Phase.Idle && allowManualHold && !busy)
            {
                phase = Phase.ManualHoldSave;
                return true;
            }
            Reset();
            return false;
        }

        internal void Begin(bool keepFastForward)
        {
            phase = keepFastForward && phase == Phase.Idle ? Phase.AwaitingPrefix : Phase.AwaitingCancelledPrefix;
        }

        internal void End(bool busy)
        {
            phase = phase == Phase.Saving && busy ? Phase.WaitingForCompletion : Phase.Idle;
        }

        internal bool BlocksBusySave(bool busy, bool cancelOnAutosave = false, bool eligibleManualHold = false)
        {
            if (!busy || (phase == Phase.ManualHoldSave ? !eligibleManualHold : cancelOnAutosave)) Reset();
            return busy && phase != Phase.WaitingForCompletion && phase != Phase.ManualHoldSave;
        }

        internal void Reset()
        {
            phase = Phase.Idle;
        }
    }
}

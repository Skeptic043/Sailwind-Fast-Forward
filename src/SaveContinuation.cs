using System;

namespace SailwindFastForward
{
    // Continuation is optional. Every save cancels until both hooks are available.
    internal sealed class SaveContinuation
    {
        private readonly AutosaveState state = new AutosaveState();
        private readonly Action cancel;
        private bool timerInstalled, timerSupported, coroutineSupported;
        internal bool Supported => timerInstalled && timerSupported && coroutineSupported;
        internal SaveContinuation(Action cancel) { this.cancel = cancel; }
        internal void SetTimerInstalled(bool installed)
        {
            bool previouslySupported = Supported;
            timerInstalled = installed;
            if (!Supported) state.Reset();
            if (previouslySupported && !Supported) cancel?.Invoke();
        }
        internal void SetTimerSupport(bool supported) => SetSupport(supported, coroutineSupported);
        internal void SetCoroutineSupport(bool supported) => SetSupport(timerSupported, supported);

        private void SetSupport(bool timer, bool coroutine)
        {
            bool previouslySupported = Supported;
            timerSupported = timer;
            coroutineSupported = coroutine;
            if (!Supported) state.Reset();
            // Harmony reruns transpilers when another mod patches the same method.
            // Revoke active continuation immediately if it no longer matches.
            if (previouslySupported && !Supported) cancel?.Invoke();
        }

        internal bool TryEnterSave(bool allowManualHold = false, bool busy = false) =>
            Supported && state.TryEnterSave(allowManualHold, busy);
        internal void Begin(bool keepFastForward) => state.Begin(Supported && keepFastForward);
        internal void End(bool busy) => state.End(Supported && busy);
        internal bool BlocksBusySave(bool busy, bool cancelOnAutosave = false, bool eligibleManualHold = false) =>
            Supported ? state.BlocksBusySave(busy, cancelOnAutosave, eligibleManualHold) : busy;
        internal void Reset() => state.Reset();

        internal static bool TryInstall(string name, Action install, Action<string> warn)
        {
            try { install(); return true; }
            catch (Exception error)
            {
                warn($"{name} unavailable ({error.GetType().Name}: {error.Message}). Fast-forward remains available. All saves will cancel fast-forward, including held fast-forward. Report this warning if save continuation is needed.");
                return false;
            }
        }
    }
}

using System;
using BepInEx.Configuration;
using UnityEngine;

namespace SailwindFastForward
{
    // One physical main-key session can acquire hold speed at most once.
    internal sealed class HoldInput
    {
        private enum Phase { Idle, Active, Suppressed }
        private Phase phase;
        private bool configured;
        private KeyboardShortcut previousHold;
        private int previousSpeed;
        private bool sampledMainHeld;
        internal bool Active => phase == Phase.Active;

        internal void Invalidate() => phase = Phase.Suppressed;

        internal bool HasIntent(float current, float selected, bool focused, KeyboardShortcut hold,
            Func<KeyCode, bool> getKeyDown, Func<KeyCode, bool> getKey) =>
            current == selected && HotkeyInput.IsHeld(hold.MainKey, hold.Modifiers, getKey) &&
            (Active || (phase == Phase.Idle && focused && getKeyDown(hold.MainKey)));

        internal SpeedInputAction Dispatch(SpeedOwnership speed, float current, int maximum, int activityMaximum,
            int effectiveMaximum, bool focused, string blocked, KeyboardShortcut cycle, KeyboardShortcut reset,
            KeyboardShortcut hold, int holdSpeed, Func<KeyCode, bool> getKeyDown, Func<KeyCode, bool> getKey,
            Action<string> cancel, Action resetAction)
        {
            bool mainHeld = hold.MainKey != KeyCode.None && getKey(hold.MainKey);
            sampledMainHeld = mainHeld;
            bool chordHeld = HotkeyInput.IsHeld(hold.MainKey, hold.Modifiers, getKey);
            bool changed = configured && (!previousHold.Equals(hold) || previousSpeed != holdSpeed);
            configured = true;
            previousHold = hold;
            previousSpeed = holdSpeed;

            if (phase == Phase.Suppressed && !mainHeld) phase = Phase.Idle;
            if (focused && HotkeyInput.IsDown(reset.MainKey, reset.Modifiers, getKeyDown, getKey))
            {
                Invalidate();
                resetAction();
                RearmAfterObservedRelease();
                return SpeedInputAction.Reset;
            }
            if (blocked != null) return Cancel(cancel, blocked);
            if (speed.Active && current != speed.SelectedSpeed) return Cancel(cancel, "timescale changed externally");
            if (!Active && speed.SelectedSpeed > maximum) return Cancel(cancel, "configured maximum lowered");
            if (changed && (Active || mainHeld)) return Cancel(cancel, "hold settings changed");
            if (Active)
            {
                if (!chordHeld) return Cancel(cancel, "hold released");
                // An intentional hold overrides ordinary movement/inventory limits.
                return speed.TryLimit(current, holdSpeed)
                    ? SpeedInputAction.Limit : SpeedInputAction.None;
            }
            if (!focused)
            {
                if (mainHeld) Invalidate();
                return speed.TryLimit(current, effectiveMaximum) ? SpeedInputAction.Limit : SpeedInputAction.None;
            }
            if (mainHeld)
            {
                if (phase == Phase.Idle && chordHeld && getKeyDown(hold.MainKey))
                {
                    int requested = holdSpeed;
                    if (speed.TrySelect(current, requested))
                    {
                        phase = Phase.Active;
                        return SpeedInputAction.Hold;
                    }
                    Invalidate();
                    return speed.TryLimit(current, effectiveMaximum) ? SpeedInputAction.Limit : SpeedInputAction.Unavailable;
                }
                Invalidate();
                if (chordHeld) return speed.TryLimit(current, effectiveMaximum) ? SpeedInputAction.Limit : SpeedInputAction.None;
            }
            return HotkeyInput.Dispatch(speed, current, effectiveMaximum, focused, cycle, reset,
                getKeyDown, getKey, resetAction);
        }

        private SpeedInputAction Cancel(Action<string> cancel, string reason)
        {
            Invalidate();
            cancel(reason);
            RearmAfterObservedRelease();
            return SpeedInputAction.Cancel;
        }

        private void RearmAfterObservedRelease()
        {
            if (!sampledMainHeld) phase = Phase.Idle;
        }
    }
}

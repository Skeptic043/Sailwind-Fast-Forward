using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SailwindFastForward
{
    internal enum SpeedInputAction { None, Reset, Limit, Cycle, Hold, Cancel, Unavailable }

    internal static class HotkeyInput
    {
        internal static bool IsHeld(KeyCode mainKey, IEnumerable<KeyCode> modifiers, Func<KeyCode, bool> getKey)
        {
            if (mainKey == KeyCode.None || !getKey(mainKey)) return false;
            foreach (KeyCode modifier in modifiers)
                if (!getKey(modifier)) return false;
            return true;
        }

        internal static SpeedInputAction Dispatch(SpeedOwnership speed, float current, int effectiveMaximum,
            bool focused, KeyboardShortcut cycle, KeyboardShortcut reset,
            Func<KeyCode, bool> getKeyDown, Func<KeyCode, bool> getKey, Action cancel)
        {
            // Reset wins overlapping shortcuts and activity changes in this frame.
            // Route through the same cancellation path as gameplay boundaries.
            if (focused && IsDown(reset.MainKey, reset.Modifiers, getKeyDown, getKey))
            {
                cancel();
                return SpeedInputAction.Reset;
            }
            if (speed.TryLimit(current, effectiveMaximum)) return SpeedInputAction.Limit;
            // Background simulation continues; another app's keys cannot change speed.
            if (!focused || !IsDown(cycle.MainKey, cycle.Modifiers, getKeyDown, getKey) || effectiveMaximum == 1)
                return SpeedInputAction.None;
            return speed.TryCycle(current, effectiveMaximum) ? SpeedInputAction.Cycle : SpeedInputAction.Unavailable;
        }

        // BepInEx KeyboardShortcut.IsDown rejects unrelated held keyboard keys.
        // Sailing controls must remain usable while toggling either direction.
        internal static bool IsDown(KeyCode mainKey, IEnumerable<KeyCode> modifiers,
            Func<KeyCode, bool> getKeyDown, Func<KeyCode, bool> getKey)
        {
            if (mainKey == KeyCode.None || !getKeyDown(mainKey)) return false;
            foreach (KeyCode modifier in modifiers)
                if (!getKey(modifier)) return false;
            return true;
        }
    }
}

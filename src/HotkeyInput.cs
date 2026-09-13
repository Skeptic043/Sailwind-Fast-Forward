using System;
using System.Collections.Generic;
using UnityEngine;

namespace SailwindFastForward
{
    internal static class HotkeyInput
    {
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

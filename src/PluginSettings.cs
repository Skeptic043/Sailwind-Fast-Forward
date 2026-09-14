using System;
using BepInEx.Configuration;
using UnityEngine;

namespace SailwindFastForward
{
    internal sealed class PluginSettings
    {
        private const string ShortcutHelp = " Add modifiers with +, for example LeftShift + Mouse4. Names ignore capitalization and accept spaces, such as left shift + mouse 4. Leave the value blank to disable this shortcut.";
        internal ShortcutSetting Hotkey { get; }
        internal ShortcutSetting ResetHotkey { get; }
        internal ShortcutSetting HoldHotkey { get; }
        internal ConfigEntry<bool> ShowIndicator { get; }
        internal ConfigEntry<int> MaxSpeed { get; }
        internal ConfigEntry<int> HoldSpeed { get; }
        internal ConfigEntry<int> MovementInventoryMaxSpeed { get; }
        internal ConfigEntry<bool> CancelOnAutosave { get; }

        internal PluginSettings(ConfigFile config, Action<string> warning = null)
        {
            // Keep the existing section/key identities so custom settings still bind.
            // Bind related settings together; BepInEx sorts section names.
            Hotkey = new ShortcutSetting(config, "Controls", "Hotkey", new KeyboardShortcut(KeyCode.F7),
                "Cycle through the allowed fast-forward speeds, then return to normal speed." + ShortcutHelp, warning);
            // A previous custom F8 cycle binding must remain usable on upgrade.
            // Bind still preserves an explicitly configured reset shortcut.
            var resetDefault = new KeyboardShortcut(Hotkey.Value.MainKey == KeyCode.F8 ? KeyCode.None : KeyCode.F8);
            ResetHotkey = new ShortcutSetting(config, "Controls", "ResetHotkey", resetDefault,
                "Return immediately to normal speed. This takes priority over the cycle and hold shortcuts." + ShortcutHelp, warning);
            var holdDefault = new KeyboardShortcut(Hotkey.Value.MainKey == KeyCode.F9 || ResetHotkey.Value.MainKey == KeyCode.F9
                ? KeyCode.None : KeyCode.F9);
            HoldHotkey = new ShortcutSetting(config, "Controls", "HoldHotkey", holdDefault,
                "Fast-forward at HoldSpeed while this key is held. Release it to return to normal speed." + ShortcutHelp, warning);

            ShowIndicator = config.Bind("Display", "ShowIndicator", true,
                "Show the active fast-forward speed.");

            MaxSpeed = config.Bind("Simulation", "MaxSpeed", 4,
                new ConfigDescription("Maximum fast-forward speed for cycling. HoldSpeed is independent of this limit.",
                    new AcceptableValueList<int>(2, 4, 8)));
            HoldSpeed = config.Bind("Simulation", "HoldSpeed", 4,
                new ConfigDescription("Fast-forward speed while holding HoldHotkey, independent of MaxSpeed. Holding ignores movement and inventory limits.",
                    new AcceptableValueList<int>(2, 4, 8)));
            MovementInventoryMaxSpeed = config.Bind("Simulation", "MovementInventoryMaxSpeed", 2,
                new ConfigDescription("Cycle-mode speed limit while moving or viewing inventory, also bounded by MaxSpeed. Holding ignores this limit. 1 turns cycle-mode fast-forward off. 8 adds no lower limit. Ending the activity does not raise speed automatically.",
                    new AcceptableValueList<int>(1, 2, 4, 8)));
            CancelOnAutosave = config.Bind("Simulation", "CancelOnAutosave", false,
                "Turn off fast-forward when an autosave starts, including while holding. When disabled, active fast-forward continues during autosaves. Loading always cancels fast-forward. Manual saves cancel cycle mode but allow an active hold to continue.");
        }
    }
}

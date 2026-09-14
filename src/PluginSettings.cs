using BepInEx.Configuration;
using UnityEngine;

namespace SailwindFastForward
{
    internal sealed class PluginSettings
    {
        private const string ShortcutHelp = " Add modifiers with + using exact names such as LeftShift, RightShift, LeftControl or LeftAlt. Leave the value blank to disable this shortcut.";
        internal ConfigEntry<KeyboardShortcut> Hotkey { get; }
        internal ConfigEntry<KeyboardShortcut> ResetHotkey { get; }
        internal ConfigEntry<KeyboardShortcut> HoldHotkey { get; }
        internal ConfigEntry<bool> ShowIndicator { get; }
        internal ConfigEntry<int> MaxSpeed { get; }
        internal ConfigEntry<int> HoldSpeed { get; }
        internal ConfigEntry<int> MovementInventoryMaxSpeed { get; }
        internal ConfigEntry<bool> CancelOnAutosave { get; }

        internal PluginSettings(ConfigFile config)
        {
            // Keep the existing section/key identities so custom settings still bind.
            // Bind related settings together; BepInEx sorts section names.
            Hotkey = config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.F7),
                "Cycle through the allowed fast-forward speeds, then return to normal speed." + ShortcutHelp);
            // A previous custom F8 cycle binding must remain usable on upgrade.
            // Bind still preserves an explicitly configured reset shortcut.
            var resetDefault = new KeyboardShortcut(Hotkey.Value.MainKey == KeyCode.F8 ? KeyCode.None : KeyCode.F8);
            ResetHotkey = config.Bind("Controls", "ResetHotkey", resetDefault,
                "Return immediately to normal speed. This takes priority over the cycle and hold shortcuts." + ShortcutHelp);
            var holdDefault = new KeyboardShortcut(Hotkey.Value.MainKey == KeyCode.F9 || ResetHotkey.Value.MainKey == KeyCode.F9
                ? KeyCode.None : KeyCode.F9);
            HoldHotkey = config.Bind("Controls", "HoldHotkey", holdDefault,
                "Fast-forward at HoldSpeed while this key is held. Release it to return to normal speed." + ShortcutHelp);

            ShowIndicator = config.Bind("Display", "ShowIndicator", true,
                "Show the active fast-forward speed.");

            MaxSpeed = config.Bind("Simulation", "MaxSpeed", 4,
                new ConfigDescription("Maximum fast-forward speed for cycling and holding.",
                    new AcceptableValueList<int>(2, 4, 8)));
            HoldSpeed = config.Bind("Simulation", "HoldSpeed", 4,
                new ConfigDescription("Speed requested while holding HoldHotkey, bounded by MaxSpeed and the movement/inventory limit.",
                    new AcceptableValueList<int>(2, 4, 8)));
            MovementInventoryMaxSpeed = config.Bind("Simulation", "MovementInventoryMaxSpeed", 2,
                new ConfigDescription("Speed limit while moving or viewing inventory, also bounded by MaxSpeed. 1 turns fast-forward off. 8 adds no lower limit. Ending the activity does not raise speed automatically.",
                    new AcceptableValueList<int>(1, 2, 4, 8)));
            CancelOnAutosave = config.Bind("Simulation", "CancelOnAutosave", false,
                "Turn off fast-forward when an autosave starts. When disabled, active fast-forward continues during autosaves. Manual saves and loading always cancel fast-forward.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using SailwindFastForward;
using UnityEngine;

internal static class ShortcutChecks
{
    // Run only after the harness initializes BepInEx Paths to scratch storage.
    internal static void Run(string scratch, Action<bool, string> check)
    {
        foreach (string raw in new[] { "o", "left shift + mouse 4" })
        {
            string baselinePath = Path.Combine(scratch, "shortcut-old-loader-" + Guid.NewGuid().ToString("N") + ".cfg");
            File.WriteAllText(baselinePath, "[Controls]\nHotkey = " + raw + "\n");
            var baselineFile = new ConfigFile(baselinePath, false);
            var baseline = baselineFile.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.F7));
            baselineFile.Save();
            check(baseline.Value.Equals(KeyboardShortcut.Empty) &&
                File.ReadAllLines(baselinePath).Contains("Hotkey = "),
                "old behavior reproduced with actual loader: invalid typed shortcut becomes Empty and loses raw text: " + raw);
        }
        foreach (string raw in new[] { "o", "O" })
        {
            check(ShortcutParser.TryParse(raw, out var key, out _) && key.MainKey == KeyCode.O,
                "letter shortcut accepts " + raw);
            check(HotkeyInput.IsDown(key.MainKey, key.Modifiers, pressed => pressed == KeyCode.O,
                held => held == KeyCode.W), "lowercase letter routes through actual input matcher");
        }
        foreach (string raw in new[] { "left shift + mouse 4", "Mouse4 + LeftShift", "LEFTSHIFT + MOUSE4",
            "Mouse4 LeftShift", "Mouse4,LeftShift", "LeftShift;Mouse4", "Mouse4|LeftShift", "Mouse4 + LeftShift + LeftShift" })
        {
            check(ShortcutParser.TryParse(raw, out var chord, out _) &&
                chord.Equals(new KeyboardShortcut(KeyCode.Mouse4, KeyCode.LeftShift)), "modifier chord accepts " + raw);
            var held = new HashSet<KeyCode> { KeyCode.Mouse4, KeyCode.LeftShift, KeyCode.W, KeyCode.Mouse0 };
            check(HotkeyInput.IsDown(chord.MainKey, chord.Modifiers, key => key == KeyCode.Mouse4, held.Contains) &&
                HotkeyInput.IsHeld(chord.MainKey, chord.Modifiers, held.Contains), "parsed chord works beside unrelated held controls");
            held.Remove(KeyCode.LeftShift);
            check(!HotkeyInput.IsDown(chord.MainKey, chord.Modifiers, key => key == KeyCode.Mouse4, held.Contains) &&
                !HotkeyInput.IsHeld(chord.MainKey, chord.Modifiers, held.Contains), "parsed chord still requires its modifier");
        }
        foreach (string raw in new[] { "", "  ", "None", "none" })
            check(ShortcutParser.TryParse(raw, out var disabled, out _) && disabled.MainKey == KeyCode.None,
                "intentional disabled value remains supported: " + raw);
        foreach (string raw in new[] { "shift", "ctrl", "not a real key", "+ ; | ,", "None + F7", "999999" })
            check(!ShortcutParser.TryParse(raw, out var invalid, out var error) && invalid.MainKey == KeyCode.None &&
                !string.IsNullOrEmpty(error), "invalid shortcut disables with a reason: " + raw);
        check(ShortcutParser.TryParse("F7 + F8 + LeftControl", out var legacy, out _) &&
            legacy.Equals(new KeyboardShortcut(KeyCode.F7, KeyCode.F8, KeyCode.LeftControl)), "multiple nonmodifier keys keep first-key semantics");
        check(ShortcutParser.TryParse("LeftShift + RightControl", out var modifierOnly, out _) &&
            modifierOnly.MainKey == KeyCode.LeftShift, "modifier-only chords keep first-key semantics");

        var keys = new[] { "Hotkey", "ResetHotkey", "HoldHotkey" };
        foreach (string keyName in keys)
        foreach (string raw in new[] { "o", "left shift + mouse 4", "F6 + LeftControl", "bad key", "+ ; | ,", "", "None" })
        {
            string path = Path.Combine(scratch, "shortcut-" + keyName + "-" + Guid.NewGuid().ToString("N") + ".cfg");
            File.WriteAllText(path, "[Controls]\n" + keyName + " = " + raw +
                "\nLegacyControl = retained\n[Simulation]\nMaxSpeed = 8\nCancelOnAutosave = true\n[OtherOwner]\nExtra = retained\n");
            var warnings = new List<string>();
            var file = new ConfigFile(path, false);
            var settings = new PluginSettings(file, warnings.Add);
            var setting = Select(settings, keyName);
            bool valid = ShortcutParser.TryParse(raw, out var expected, out _);
            check(setting.Value.Equals(expected), "real initial bind parses " + keyName + ": " + raw);
            check(warnings.Count == (valid ? 0 : 1), "initial bind warns exactly once for invalid " + keyName);
            if (!valid) check(warnings[0].Contains(keyName) && warnings[0].Contains(raw) && warnings[0].Contains("kept unchanged"),
                "warning identifies the invalid binding and preserved text");
            for (int i = 0; i < 20; i++) { var ignored = setting.Value; }
            file.Save();
            file.Reload();
            check(warnings.Count == (valid ? 0 : 1), "same-value reads and reload do not repeat invalid warning");
            check(file.TryGetEntry<string>("Controls", keyName, out var rawEntry) && rawEntry.Value == raw,
                "real text config keeps original value for " + keyName);
            string saved = File.ReadAllText(path);
            check(saved.Contains(keyName + " = " + raw + Environment.NewLine) && saved.Contains("LegacyControl = retained") &&
                saved.Contains("Extra = retained"), "save preserves raw shortcut and orphaned settings");
            var reloaded = new PluginSettings(new ConfigFile(path, false));
            check(Select(reloaded, keyName).Value.Equals(expected) && reloaded.MaxSpeed.Value == 8 && reloaded.CancelOnAutosave.Value,
                "new config instance preserves shortcut and unrelated custom settings");

            rawEntry.Value = "different invalid value";
            check(setting.Value.MainKey == KeyCode.None && warnings.Count == (valid ? 1 : 2), "changed invalid raw text updates cached result and warns once");
            rawEntry.Value = "different invalid value";
            check(warnings.Count == (valid ? 1 : 2), "same invalid assignment does not spam warnings");
            rawEntry.Value = "left shift + mouse 4";
            check(setting.Value.Equals(new KeyboardShortcut(KeyCode.Mouse4, KeyCode.LeftShift)), "changing raw text repairs cached shortcut");
            setting.Value = new KeyboardShortcut(KeyCode.F10, KeyCode.RightControl);
            file.Reload();
            check(setting.Value.Equals(new KeyboardShortcut(KeyCode.F10, KeyCode.RightControl)), "typed API setter persists through real reload");
        }

        string activePath = Path.Combine(scratch, "shortcut-active-hold.cfg");
        File.WriteAllText(activePath, "[Controls]\nHoldHotkey = left shift + mouse 4\n");
        var activeFile = new ConfigFile(activePath, false);
        var active = new PluginSettings(activeFile);
        var speed = new SpeedOwnership();
        var holdState = new HoldInput();
        var heldKeys = new HashSet<KeyCode> { KeyCode.Mouse4, KeyCode.LeftShift, KeyCode.W };
        int cancellations = 0;
        Action<string> cancel = _ => { cancellations++; holdState.Invalidate(); speed.Release(4f); };
        var acquired = holdState.Dispatch(speed, 1f, 8, 2, 2, true, null, active.Hotkey.Value, active.ResetHotkey.Value,
            active.HoldHotkey.Value, 4, key => key == KeyCode.Mouse4, heldKeys.Contains, cancel, () => cancel("reset"));
        check(acquired == SpeedInputAction.Hold && holdState.Active, "real configured spaced chord acquires production hold state");
        activeFile.TryGetEntry<string>("Controls", "HoldHotkey", out var activeRaw);
        activeRaw.Value = "broken binding";
        var stopped = holdState.Dispatch(speed, 4f, 8, 2, 2, true, null, active.Hotkey.Value, active.ResetHotkey.Value,
            active.HoldHotkey.Value, 4, _ => false, heldKeys.Contains, cancel, () => cancel("reset"));
        check(stopped == SpeedInputAction.Cancel && cancellations == 1 && !holdState.Active && !speed.Active &&
            activeRaw.Value == "broken binding", "invalid raw edit cancels active hold without erasing the invalid text");
    }

    private static ShortcutSetting Select(PluginSettings settings, string key) =>
        key == "Hotkey" ? settings.Hotkey : key == "ResetHotkey" ? settings.ResetHotkey : settings.HoldHotkey;
}

using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using SailwindFastForward;
using UnityEngine;

internal static class ConfigurationManagerChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(ConfigurationManagerWindow.PluginId == "com.bepis.bepinex.configurationmanager",
            "optional Configuration Manager lookup retains the exact plugin identity");
        int lookups = 0, warnings = 0;
        var absent = new ConfigurationManagerWindow(() => { lookups++; return null; }, _ => warnings++);
        check(!absent.IsOpen() && !absent.IsOpen() && lookups == 1 && warnings == 0,
            "absent Configuration Manager retains cursor cancellation without repeated lookups or warnings");
        var fixture = new WindowFixture();
        var window = new ConfigurationManagerWindow(() => { lookups++; return fixture; }, _ => warnings++);
        check(!window.IsOpen(), "closed Configuration Manager is not exempt");
        fixture.DisplayingWindow = true;
        check(window.IsOpen() && lookups == 2, "cached Configuration Manager getter observes opening");
        fixture.DisplayingWindow = false;
        check(!window.IsOpen() && lookups == 2, "cached Configuration Manager getter observes closing");
        foreach (object invalid in new object[] { new object(), new WrongTypeFixture(), new StaticFixture(), new ThrowingFixture() })
        {
            int priorWarnings = warnings;
            var unavailable = new ConfigurationManagerWindow(() => invalid, _ => warnings++);
            check(!unavailable.IsOpen() && !unavailable.IsOpen() && warnings == priorWarnings + 1,
                "invalid Configuration Manager getter fails closed with one warning: " + invalid.GetType().Name);
        }
        var brokenLookup = new ConfigurationManagerWindow(() => throw new InvalidOperationException("lookup"), _ => warnings++);
        int previousWarnings = warnings;
        check(!brokenLookup.IsOpen() && !brokenLookup.IsOpen() && warnings == previousWarnings + 1,
            "failed optional plugin lookup does not disable fast-forward or retry indefinitely");

        foreach (bool cursor in new[] { false, true })
        {
            var f = new Frames { Cursor = cursor, ConfigurationOpen = true };
            f.Down.Add(KeyCode.F7);
            check(f.Step() == SpeedInputAction.Cycle && f.Scale == 2f,
                "cycle can start with Configuration Manager open and cursor=" + cursor);
            check(f.Step() == SpeedInputAction.Cycle && f.Scale == 4f,
                "cycle can advance with Configuration Manager open and cursor=" + cursor);
            f.Down.Clear();
            check(f.Step() == SpeedInputAction.None && f.Scale == 4f,
                "Configuration Manager can remain open without cancelling active cycle");
            f.Keys.Add(KeyCode.F9); f.Down.Add(KeyCode.F9);
            check(f.Step() == SpeedInputAction.Hold && f.Scale == 8f,
                "hold can override cycle while Configuration Manager is open");
            f.Down.Clear();
            check(f.Step() == SpeedInputAction.None && f.Scale == 8f && f.Hold.Active,
                "held fast-forward continues while Configuration Manager is open");
            f.Keys.Clear();
            check(f.Step() == SpeedInputAction.Cancel && f.Scale == 1f && !f.Speed.Active,
                "Configuration Manager does not prevent hold release");
        }
        var opening = new Frames();
        opening.Down.Add(KeyCode.F7); opening.Step(); opening.Down.Clear();
        opening.Cursor = true; opening.ConfigurationOpen = true;
        check(opening.Step() == SpeedInputAction.None && opening.Scale == 2f,
            "opening Configuration Manager preserves an already-active cycle");
        opening.ConfigurationOpen = false;
        check(opening.Step() == SpeedInputAction.Cancel && opening.Scale == 1f,
            "a cursor menu left after Configuration Manager closes is not exempt");
        opening.Down.Add(KeyCode.F7);
        check(opening.Step() == SpeedInputAction.Cancel && opening.Scale == 1f,
            "ordinary cursor menus still prevent cycle activation");

        var nativeMenu = new Frames { Cursor = true, ConfigurationOpen = true, NativeCursorMenu = true };
        nativeMenu.Down.Add(KeyCode.F7);
        check(nativeMenu.Step() == SpeedInputAction.Cancel && nativeMenu.Scale == 1f,
            "native cursor menu behind Configuration Manager still blocks cycle activation");
        nativeMenu.NativeCursorMenu = false; nativeMenu.Step(); nativeMenu.Down.Clear();
        nativeMenu.NativeCursorMenu = true;
        check(nativeMenu.Step() == SpeedInputAction.Cancel && !nativeMenu.Speed.Active,
            "opening a native cursor menu behind Configuration Manager cancels active cycle");
        check(!ConfigurationManagerWindow.BlocksCursorMenu(true, true, false, false),
            "existing inventory cursor exception remains unchanged");

        // Model the independent boundaries supplied by Plugin.BlockReason. The optional
        // window exception must not change dispatch or external time-scale ownership.
        foreach (string boundary in new[] { "settings menu", "save loading", "sleep/bed", "economy menu", "shipyard", "save busy" })
        {
            var f = new Frames { Cursor = true, ConfigurationOpen = true };
            f.Down.Add(KeyCode.F7); f.Step(); f.Down.Clear(); f.Blocked = boundary;
            check(f.Step() == SpeedInputAction.Cancel && f.Scale == 1f && !f.Speed.Active,
                "Configuration Manager cannot bypass independent " + boundary + " boundary");
        }
        foreach (float external in new[] { 0f, 0.5f, 16f })
        {
            var f = new Frames { Cursor = true, ConfigurationOpen = true };
            f.Down.Add(KeyCode.F7); f.Step(); f.Down.Clear(); f.Scale = external;
            check(f.Step() == SpeedInputAction.Cancel && f.Scale == external && !f.Speed.Active,
                "Configuration Manager preserves external/native scale " + external);
            f.Down.Add(KeyCode.F7);
            check(f.Step() == SpeedInputAction.Unavailable && f.Scale == external,
                "Configuration Manager cannot acquire native/external scale " + external);
        }
    }

    public sealed class WindowFixture { public bool DisplayingWindow { get; set; } }
    public sealed class WrongTypeFixture { public int DisplayingWindow => 1; }
    public sealed class StaticFixture { public static bool DisplayingWindow => true; }
    public sealed class ThrowingFixture { public bool DisplayingWindow => throw new InvalidOperationException("getter"); }

    private sealed class Frames
    {
        internal readonly SpeedOwnership Speed = new SpeedOwnership();
        internal readonly HoldInput Hold = new HoldInput();
        internal readonly HashSet<KeyCode> Keys = new HashSet<KeyCode>();
        internal readonly HashSet<KeyCode> Down = new HashSet<KeyCode>();
        internal float Scale = 1f;
        internal bool Cursor, ConfigurationOpen, NativeCursorMenu;
        internal string Blocked;
        internal SpeedInputAction Step()
        {
            var holdShortcut = new KeyboardShortcut(KeyCode.F9);
            bool intentionalHold = Hold.HasIntent(Scale, Speed.SelectedSpeed, true, holdShortcut, Down.Contains, Keys.Contains);
            string reason = Blocked ?? (ConfigurationManagerWindow.BlocksCursorMenu(Cursor, false, ConfigurationOpen, NativeCursorMenu) &&
                HoldPolicy.Blocks(HoldBoundary.CursorMenu, intentionalHold) ? "cursor menu" : null);
            var action = Hold.Dispatch(Speed, Scale, 4, 2, 4, true, reason,
                new KeyboardShortcut(KeyCode.F7), new KeyboardShortcut(KeyCode.F8), holdShortcut, 8,
                Down.Contains, Keys.Contains, Cancel, () => Cancel("reset"));
            if (action == SpeedInputAction.Cycle || action == SpeedInputAction.Hold || action == SpeedInputAction.Limit)
                Scale = Speed.SelectedSpeed;
            return action;
        }
        private void Cancel(string reason)
        {
            Hold.Invalidate();
            if (Speed.Release(Scale)) Scale = 1f;
        }
    }
}

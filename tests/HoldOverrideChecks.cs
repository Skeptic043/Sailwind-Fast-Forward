using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using SailwindFastForward;
using UnityEngine;

internal static class HoldOverrideChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (int requested in new[] { 2, 4, 8 })
        foreach (int maximum in new[] { 2, 4, 8 })
        foreach (int activity in new[] { 1, 2, 4, 8 })
        {
            var frame = new Frames { Requested = requested, Maximum = maximum, ActivityMaximum = activity };
            frame.Keys.UnionWith(new[] { KeyCode.F9, KeyCode.W, KeyCode.Mouse0 });
            frame.Down.Add(KeyCode.F9);
            check(frame.Step() == SpeedInputAction.Hold && frame.Scale == requested,
                $"hold {requested}x overrides activity {activity}x independently of cycle maximum {maximum}x");
            frame.Down.Clear();
            frame.ActivityMaximum = activity == 1 ? 8 : 1;
            check(frame.Step() == SpeedInputAction.None && frame.State.Active && frame.Scale == requested,
                "activity setting changes neither cancel nor lower active hold");
            frame.Keys.Remove(KeyCode.F9);
            check(frame.Step() == SpeedInputAction.Cancel && frame.Scale == 1f && !frame.State.Active,
                "override hold release returns directly to normal speed");
        }
        var toggled = new Frames { Requested = 8, ActivityMaximum = 1 };
        toggled.Speed.TrySelect(1f, 2); toggled.Scale = 2f;
        toggled.Keys.Add(KeyCode.F9); toggled.Down.Add(KeyCode.F9);
        check(toggled.Step() == SpeedInputAction.Hold && toggled.Scale == 8f,
            "intentional hold overrides activity cap one even from latched fast-forward");
        toggled.Keys.Clear(); toggled.Down.Clear(); toggled.Step();
        check(toggled.Scale == 1f, "override release does not restore previously latched speed");

        foreach (string boundary in new[] { "reset", "pause", "loading", "autosave", "external", "hold-speed", "binding" })
        {
            var frame = new Frames { ActivityMaximum = 1 };
            frame.Keys.Add(KeyCode.F9); frame.Down.Add(KeyCode.F9); frame.Step(); frame.Down.Clear();
            if (boundary == "reset") frame.Down.Add(KeyCode.F8);
            else if (boundary == "external") frame.Scale = 16f;
            else if (boundary == "hold-speed") frame.Requested = 8;
            else if (boundary == "binding")
            {
                frame.Binding = new KeyboardShortcut(KeyCode.F10);
                frame.Keys.Add(KeyCode.F10);
            }
            else frame.Blocked = boundary;
            frame.Step();
            check(!frame.State.Active && !frame.Speed.Active, boundary + " still cancels override hold");
            if (boundary == "external") check(frame.Scale == 16f, "override cancellation preserves externally owned time scale");
            frame.Scale = 1f; frame.Blocked = null; frame.Down.Clear();
            check(frame.Step() == SpeedInputAction.None && frame.Scale == 1f,
                boundary + " cannot reacquire override while the main key remains held");
            frame.Down.Add(frame.Binding.MainKey);
            check(frame.Step() == SpeedInputAction.None && frame.Scale == 1f,
                boundary + " rejects repeated press until an observed release");
            frame.Keys.Clear(); frame.Down.Clear(); frame.Step();
            frame.Keys.Add(frame.Binding.MainKey); frame.Down.Add(frame.Binding.MainKey);
            check(frame.Step() == SpeedInputAction.Hold,
                boundary + " permits an intentional new override after release");
        }
        var independent = new Frames { Requested = 8, Maximum = 4, ActivityMaximum = 1 };
        independent.Keys.Add(KeyCode.F9); independent.Down.Add(KeyCode.F9); independent.Step(); independent.Down.Clear();
        independent.Maximum = 2;
        check(independent.Step() == SpeedInputAction.None && independent.Scale == 8f && independent.State.Active,
            "cycle ceiling edit leaves active hold unchanged despite activity cap one");
        independent.Requested = 4;
        check(independent.Step() == SpeedInputAction.Cancel && independent.Scale == 1f,
            "HoldSpeed edit still cancels after an independent cycle ceiling edit");
        check(independent.Step() == SpeedInputAction.None && independent.Scale == 1f,
            "HoldSpeed edit cannot reacquire without release and a fresh press");

        var cycle = new Frames { ActivityMaximum = 1 };
        cycle.Down.Add(KeyCode.F7);
        check(cycle.Step() == SpeedInputAction.None && cycle.Scale == 1f, "ordinary cycling still respects activity cap one");
        var suppressed = new Frames { ActivityMaximum = 1 };
        suppressed.Speed.TrySelect(1f, 8); suppressed.Scale = 8f;
        suppressed.State.Invalidate(); suppressed.Keys.Add(KeyCode.F9);
        check(suppressed.Step() == SpeedInputAction.Limit && suppressed.Scale == 1f,
            "suppressed hold input does not turn latched speed into an override");

        var focus = new Frames { ActivityMaximum = 1, Maximum = 4, Requested = 8 };
        focus.Keys.Add(KeyCode.F9); focus.Down.Add(KeyCode.F9); focus.Step(); focus.Down.Clear();
        focus.Focused = false;
        check(focus.Step() == SpeedInputAction.None && focus.State.Active && focus.Scale == 8f,
            "active hold continues while physical chord is reported held despite focus loss");
        focus.Keys.Clear();
        check(focus.Step() == SpeedInputAction.Cancel && focus.Scale == 1f,
            "reported physical hold release cancels even while unfocused");
        focus.Keys.Add(KeyCode.F9); focus.Down.Add(KeyCode.F9);
        check(focus.Step() != SpeedInputAction.Hold && focus.Scale == 1f,
            "new hold cannot begin while unfocused");
        focus.Focused = true; focus.Down.Clear();
        check(focus.Step() == SpeedInputAction.None && focus.Scale == 1f,
            "unfocused press cannot queue hold activation on focus return");

        foreach (HoldBoundary boundary in Enum.GetValues(typeof(HoldBoundary)))
        {
            bool ordinary = boundary == HoldBoundary.SleepOrBed || boundary == HoldBoundary.Recovery ||
                boundary == HoldBoundary.Shipyard || boundary == HoldBoundary.Economy || boundary == HoldBoundary.CursorMenu;
            var frame = new Frames { Boundary = boundary, ActivityMaximum = 1 };
            frame.Keys.Add(KeyCode.F9); frame.Down.Add(KeyCode.F9);
            check((frame.Step() == SpeedInputAction.Hold) == ordinary,
                "production hold boundary classification for " + boundary);
            check(HoldPolicy.Blocks(boundary, false), "ordinary input remains blocked by " + boundary);
        }
        foreach (int requested in new[] { 2, 4, 8 })
        foreach (string ending in new[] { "release", "reset", "pause", "loading", "external", "early-error", "late-error" })
        {
            var manual = new Frames { Requested = requested, ActivityMaximum = 1, Maximum = 4 };
            manual.Keys.Add(KeyCode.F9); manual.Down.Add(KeyCode.F9); manual.Step(); manual.Down.Clear();
            check(manual.BeginManualSave(), "active owned hold permits a manual save at " + requested);
            if (ending == "early-error")
            {
                var error = new Exception("notification or coroutine-start failure");
                check(ReferenceEquals(error, manual.Error(error)) && !manual.Busy && !manual.State.Active,
                    "early manual SaveGame exception preserves error and cancels hold before busy");
            }
            else
            {
                manual.Busy = true;
                for (int frame = 0; frame < 2; frame++)
                    check(manual.Step() == SpeedInputAction.None && manual.Scale == requested,
                        "held manual save continues only its existing owned speed");
            }
            if (ending == "release") manual.Keys.Clear();
            if (ending == "reset") manual.Down.Add(KeyCode.F8);
            if (ending == "pause") manual.Boundary = HoldBoundary.Pause;
            if (ending == "loading") manual.Boundary = HoldBoundary.Loading;
            if (ending == "external") manual.Scale = 16f;
            if (ending == "late-error")
            {
                var error = new Exception("manual coroutine failure");
                check(ReferenceEquals(error, manual.Error(error)) && manual.Busy,
                    "late manual coroutine exception preserves error and vanilla busy state");
            }
            manual.Step();
            check(!manual.State.Active && !manual.Speed.Active,
                ending + " cancels hold and manual-save ownership");
            if (ending == "external") check(manual.Scale == 16f, "manual-save hold never restores native/external scale");
            manual.Busy = false; manual.Boundary = null; manual.Down.Clear(); manual.Scale = 1f;
            check(manual.Step() == SpeedInputAction.None && manual.Scale == 1f,
                "manual save completion cannot reacquire a cancelled hold");
        }
    }

    // Only the Unity scale read/write adapter is represented here. Routing and
    // ownership decisions run the same production helpers as the plugin.
    private sealed class Frames
    {
        internal readonly SpeedOwnership Speed = new SpeedOwnership();
        internal readonly HoldInput State = new HoldInput();
        internal readonly AutosaveState Save = new AutosaveState();
        internal readonly HashSet<KeyCode> Keys = new HashSet<KeyCode>();
        internal readonly HashSet<KeyCode> Down = new HashSet<KeyCode>();
        internal KeyboardShortcut Binding = new KeyboardShortcut(KeyCode.F9);
        internal float Scale = 1f;
        internal int Requested = 4, Maximum = 8, ActivityMaximum = 2;
        internal string Blocked;
        internal HoldBoundary? Boundary;
        internal bool Focused = true, Busy;

        internal SpeedInputAction Step()
        {
            bool intent = State.HasIntent(Scale, Speed.SelectedSpeed, Focused, Binding, Down.Contains, Keys.Contains);
            string blocked = Blocked ?? (Boundary.HasValue && HoldPolicy.Blocks(Boundary.Value, intent) ? Boundary.ToString() : null);
            if (Save.BlocksBusySave(Busy, false, Eligible())) blocked = blocked ?? "save busy";
            var action = State.Dispatch(Speed, Scale, Maximum, ActivityMaximum, Math.Min(Maximum, ActivityMaximum),
                Focused, blocked, new KeyboardShortcut(KeyCode.F7), new KeyboardShortcut(KeyCode.F8), Binding, Requested,
                Down.Contains, Keys.Contains, Cancel, () => Cancel("reset"));
            if (action == SpeedInputAction.Hold || action == SpeedInputAction.Limit || action == SpeedInputAction.Cycle)
                Scale = Speed.SelectedSpeed;
            return action;
        }

        private void Cancel(string reason)
        {
            State.Invalidate();
            Save.Reset();
            if (Speed.Release(Scale)) Scale = 1f;
        }

        internal bool BeginManualSave() => Save.TryEnterSave(Eligible(), Busy);
        internal Exception Error(Exception error) => SaveErrorBoundary.Handle(error, () => Cancel("save error"));

        private bool Eligible() => HoldPolicy.CanContinue(State, Speed, Scale,
            HotkeyInput.IsHeld(Binding.MainKey, Binding.Modifiers, Keys.Contains),
            Boundary.HasValue && HoldPolicy.Blocks(Boundary.Value, true));
    }
}

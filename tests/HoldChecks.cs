using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using SailwindFastForward;
using UnityEngine;

internal static class HoldChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (int requested in new[] { 2, 4, 8 })
        foreach (int maximum in new[] { 2, 4, 8 })
        {
            var f = new Frames { Requested = requested, Maximum = maximum };
            f.Keys.UnionWith(new[] { KeyCode.F9, KeyCode.W, KeyCode.Mouse0 });
            f.Down.Add(KeyCode.F9);
            check(f.Step() == SpeedInputAction.Hold && f.Scale == Math.Min(requested, maximum),
                $"hold {requested} respects maximum {maximum} and unrelated controls");
            f.Down.Clear();
            check(f.Step() == SpeedInputAction.None && f.State.Active, "held key sustains without a repeat press");
            f.Keys.Remove(KeyCode.F9);
            check(f.Step() == SpeedInputAction.Cancel && f.Scale == 1f && !f.State.Active, "hold release returns directly to 1x");
            f.Keys.Add(KeyCode.F9);
            f.Down.Add(KeyCode.F9);
            check(f.Step() == SpeedInputAction.Hold, "first new press after observed release can acquire again");
        }
        foreach (int toggled in new[] { 2, 4, 8 })
        {
            var f = new Frames();
            f.Speed.TrySelect(1f, toggled);
            f.Scale = toggled;
            f.Keys.Add(KeyCode.F9); f.Down.Add(KeyCode.F9);
            check(f.Step() == SpeedInputAction.Hold && f.Scale == 4f, "hold selects fixed speed from toggled " + toggled);
            f.Keys.Clear(); f.Down.Clear(); f.Step();
            check(f.Scale == 1f, "hold release never restores previous toggled " + toggled);
        }
        foreach (string boundary in new[] { "reset", "pause", "external", "focus", "config", "release-modifier", "manual" })
        {
            var f = new Frames { Binding = new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl) };
            f.Keys.UnionWith(new[] { KeyCode.F9, KeyCode.LeftControl }); f.Down.Add(KeyCode.F9); f.Step(); f.Down.Clear();
            switch (boundary)
            {
                case "reset": f.Down.Add(KeyCode.F8); break;
                case "pause": f.Blocked = "pause"; break;
                case "external": f.Scale = 16f; break;
                case "focus": f.Focused = false; break;
                case "config": f.Requested = 8; break;
                case "release-modifier": f.Keys.Remove(KeyCode.LeftControl); break;
                case "manual": f.Cancel("manual boundary"); break;
            }
            f.Step();
            check(!f.State.Active && !f.Speed.Active, boundary + " cancels hold ownership");
            if (boundary == "external") check(f.Scale == 16f, "hold cancellation preserves external scale");
            f.Scale = 1f; f.Blocked = null; f.Focused = true; f.Keys.Add(KeyCode.LeftControl); f.Down.Clear();
            check(f.Step() == SpeedInputAction.None && f.Scale == 1f, boundary + " cannot reacquire while main key remains held");
            f.Down.Add(KeyCode.F9);
            check(f.Step() == SpeedInputAction.None && f.Scale == 1f, boundary + " rejects a repeated down signal until release");
            f.Keys.Remove(KeyCode.F9); f.Down.Clear(); f.Step();
            f.Keys.Add(KeyCode.F9); f.Down.Add(KeyCode.F9);
            check(f.Step() == SpeedInputAction.Hold, boundary + " rearms only after release/new press");
        }
        foreach (string unavailable in new[] { "blocked", "external", "cap", "modifier", "focus", "already-held" })
        {
            var f = new Frames { Binding = new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl) };
            f.Keys.UnionWith(new[] { KeyCode.F9, KeyCode.LeftControl }); f.Down.Add(KeyCode.F9);
            switch (unavailable)
            {
                case "blocked": f.Blocked = "loading"; break;
                case "external": f.Scale = 16f; break;
                case "cap": f.Effective = 1; break;
                case "modifier": f.Keys.Remove(KeyCode.LeftControl); break;
                case "focus": f.Focused = false; break;
                case "already-held": f.Down.Clear(); break;
            }
            f.Step();
            f.Blocked = null; f.Scale = 1f; f.Effective = 8; f.Focused = true; f.Keys.Add(KeyCode.LeftControl); f.Down.Clear();
            check(f.Step() == SpeedInputAction.None && !f.Speed.Active, unavailable + " press does not queue a later hold activation");
        }
        var limit = new Frames { Requested = 8 };
        limit.Keys.Add(KeyCode.F9); limit.Down.Add(KeyCode.F9); limit.Step(); limit.Down.Clear();
        limit.Effective = 2;
        check(limit.Step() == SpeedInputAction.Limit && limit.Scale == 2f, "movement limit lowers an active hold");
        limit.Effective = 8;
        check(limit.Step() == SpeedInputAction.None && limit.Scale == 2f, "ending movement does not raise the held speed");
        limit.Down.Add(KeyCode.F7);
        check(limit.Step() == SpeedInputAction.None && limit.Scale == 2f, "hold wins cycle while held");
        limit.Effective = 1; limit.Step(); limit.Effective = 8; limit.Down.Clear();
        check(limit.Step() == SpeedInputAction.None && limit.Scale == 1f, "activity cap one cancels and suppresses held reacquisition");
        var suppressed = new Frames();
        suppressed.Speed.TrySelect(1f, 8); suppressed.Scale = 8f; suppressed.Focused = false; suppressed.Step();
        suppressed.Focused = true; suppressed.Keys.Add(KeyCode.F9); suppressed.Effective = 2;
        check(suppressed.Step() == SpeedInputAction.Limit && suppressed.Scale == 2f,
            "suppressed held input cannot bypass safety limit on latched speed");
        var capOne = new Frames { Effective = 1 };
        capOne.Speed.TrySelect(1f, 4); capOne.Scale = 4f; capOne.Keys.Add(KeyCode.F9); capOne.Down.Add(KeyCode.F9);
        check(capOne.Step() == SpeedInputAction.Limit && capOne.Scale == 1f && !capOne.State.Active,
            "failed new hold at cap one still lowers existing latched speed");

        var overlap = new Frames { Binding = new KeyboardShortcut(KeyCode.F7) };
        overlap.Keys.Add(KeyCode.F7); overlap.Down.Add(KeyCode.F7);
        check(overlap.Step() == SpeedInputAction.Hold && overlap.Scale == 4f, "explicit overlapping hold wins cycle");
        overlap.Down.Add(KeyCode.F8);
        check(overlap.Step() == SpeedInputAction.Reset && overlap.Scale == 1f, "reset wins simultaneous hold and cycle");
        var disabled = new Frames { Binding = new KeyboardShortcut(KeyCode.None) };
        disabled.Keys.Add(KeyCode.F9); disabled.Down.Add(KeyCode.F9);
        check(disabled.Step() == SpeedInputAction.None, "None disables hold");

        var rebind = new Frames();
        rebind.Keys.Add(KeyCode.F9); rebind.Down.Add(KeyCode.F9); rebind.Step();
        rebind.Binding = new KeyboardShortcut(KeyCode.F10); rebind.Keys.Add(KeyCode.F10); rebind.Down.Clear();
        check(rebind.Step() == SpeedInputAction.Cancel && rebind.Scale == 1f, "rebinding cancels an active hold");
        check(rebind.Step() == SpeedInputAction.None, "held rebound key cannot acquire without release/new press");
        foreach (int requested in new[] { 2, 4, 8 })
        foreach (string end in new[] { "release", "reset", "late-error", "policy", "external" })
        {
            var f = new Frames { Requested = requested };
            f.Keys.Add(KeyCode.F9); f.Down.Add(KeyCode.F9); f.Step(); f.Down.Clear();
            f.Autosave.Begin(true);
            check(f.Autosave.TryEnterSave(), "held speed enters one permitted autosave prefix");
            f.Busy = true; f.Autosave.End(true);
            for (int frame = 0; frame < 3; frame++)
                check(f.Step() == SpeedInputAction.None && f.Scale == requested && f.Background,
                    $"continued autosave preserves held {requested}x/background at frame {frame}");
            if (end == "release") f.Keys.Clear();
            if (end == "reset") f.Down.Add(KeyCode.F8);
            if (end == "policy") f.CancelOnAutosave = true;
            if (end == "external") f.Scale = 16f;
            if (end == "late-error")
            {
                var error = new Exception("late save failure");
                check(ReferenceEquals(error, SaveErrorBoundary.Handle(error, () =>
                {
                    f.Cancel("late save error");
                    throw new Exception("logger failure after cleanup");
                })), "late coroutine failure preserves original exception after cleanup failure");
            }
            f.Step();
            check(!f.Speed.Active && !f.State.Active && !f.Background && f.Autosave.BlocksBusySave(true),
                end + " during autosave invalidates hold/save/background permission");
            check(f.Busy, "cancellation never writes vanilla busy");
            if (end == "external") check(f.Scale == 16f, "autosave cancellation preserves externally owned speed");
            f.Down.Clear(); f.Scale = 1f; f.CancelOnAutosave = false;
            f.Busy = false; f.Step();
            check(f.Scale == 1f && !f.Speed.Active, end + " save completion cannot write or reacquire speed");
            if (end != "release")
                check(f.Step() == SpeedInputAction.None && f.Scale == 1f, end + " completion cannot reacquire with key still held");
        }
        var busyPress = new Frames { Busy = true };
        busyPress.Keys.Add(KeyCode.F9); busyPress.Down.Add(KeyCode.F9); busyPress.Step();
        busyPress.Busy = false; busyPress.Down.Clear();
        check(busyPress.Step() == SpeedInputAction.None && busyPress.Scale == 1f, "new hold during unowned busy save cannot queue activation");
    }

    // Adapt production decisions to an in-memory timeScale; no Unity player is run.
    private sealed class Frames
    {
        internal readonly SpeedOwnership Speed = new SpeedOwnership();
        internal readonly HoldInput State = new HoldInput();
        internal readonly AutosaveState Autosave = new AutosaveState();
        private readonly BackgroundExecution background;
        internal readonly HashSet<KeyCode> Keys = new HashSet<KeyCode>();
        internal readonly HashSet<KeyCode> Down = new HashSet<KeyCode>();
        internal KeyboardShortcut Binding = new KeyboardShortcut(KeyCode.F9);
        internal float Scale = 1f;
        internal int Requested = 4, Maximum = 8, Effective = 8;
        internal bool Focused = true;
        internal bool Busy, CancelOnAutosave, Background;
        internal string Blocked;
        internal Frames() { background = new BackgroundExecution(() => Background, value => Background = value); }
        internal SpeedInputAction Step()
        {
            string blocked = Blocked ?? (Autosave.BlocksBusySave(Busy, CancelOnAutosave) ? "save busy" : null);
            var action = State.Dispatch(Speed, Scale, Maximum, 2, Math.Min(Maximum, Effective), Focused, blocked,
                new KeyboardShortcut(KeyCode.F7), new KeyboardShortcut(KeyCode.F8), Binding, Requested,
                Down.Contains, Keys.Contains, Cancel, () => Cancel("reset"));
            if (action == SpeedInputAction.Hold || action == SpeedInputAction.Limit || action == SpeedInputAction.Cycle)
            {
                Scale = Speed.SelectedSpeed;
                if (Speed.Active) background.Enable(); else background.Restore();
            }
            return action;
        }
        internal void Cancel(string reason)
        {
            State.Invalidate();
            Autosave.Reset();
            if (Speed.Release(Scale)) Scale = 1f;
            background.Restore();
        }
    }
}

using System;
using System.Collections.Generic;
using SailwindFastForward;
using UnityEngine;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine("PASS: " + name);
}

var speed = new SpeedOwnership();
Check(!speed.Release(2f), "unowned external 2x is not restored");
foreach (float native in new[] { 0f, 0.5f, 2f, 4f, 16f, float.NaN, float.PositiveInfinity })
    Check(!speed.TryCycle(native, 2) && !speed.Active, "cannot acquire external/native scale " + native);
Check(speed.TryCycle(1f, 2) && speed.Active, "normal gameplay permits acquisition");
Check(!speed.TryCycle(1f, 2), "cannot acquire twice");
Check(speed.Release(2f) && !speed.Active, "toggle-off restores owned 2x");
Check(!speed.Release(2f), "shutdown is idempotent");
foreach (float external in new[] { 0f, 1f, 0.5f, 4f, 16f })
{
    speed.TryCycle(1f, 2);
    Check(!speed.Release(external) && !speed.Active, "release preserves externally changed scale " + external);
}
// Model the observed synchronous StartMenu ordering. This is not an engine test.
speed.TryCycle(1f, 2);
float scale = 2f;
if (speed.Release(scale)) scale = 1f; // prefix before native capture
float unpausedTimescale = scale;
scale = 0f;
Check(!speed.Active && scale == 0f, "pause stays paused after boundary cancellation");
scale = unpausedTimescale;
Check(scale == 1f, "native menu restore uses 1x after prefix");
// FallAsleep prefix releases the mod before the delayed native 16x write.
speed.TryCycle(1f, 2);
Check(speed.Release(2f), "sleep entry releases the owned speed before fade");
Check(!speed.Release(16f), "later native sleep warp is untouched");
Check(speed.TryCycle(1f, 2), "fresh explicit activation is possible after native wake");
// Run the production hotkey matcher with controlled key states, not a Unity input loop.
var held = new HashSet<KeyCode>();
var pressed = new HashSet<KeyCode>();
bool Hotkey(KeyCode main = KeyCode.F7, params KeyCode[] modifiers) =>
    HotkeyInput.IsDown(main, modifiers, pressed.Contains, held.Contains);
Check(!Hotkey(), "no key press does not toggle");
pressed.Add(KeyCode.F7);
Check(Hotkey(), "F7 alone toggles");
foreach (KeyCode control in new[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Mouse0, KeyCode.LeftShift })
{
    held.Clear();
    held.Add(control);
    Check(Hotkey(), "F7 toggles while holding " + control);
}
held.Clear();
held.UnionWith(new[] { KeyCode.W, KeyCode.A, KeyCode.Mouse0 });
Check(Hotkey(), "several simultaneous controls do not block F7");
Check(!Hotkey(KeyCode.F7, KeyCode.LeftControl), "configured modifier remains required");
held.Add(KeyCode.LeftControl);
Check(Hotkey(KeyCode.F7, KeyCode.LeftControl), "configured chord works alongside sailing controls");
Check(!Hotkey(KeyCode.F7, KeyCode.LeftControl, KeyCode.LeftShift), "all configured modifiers must be held");
held.Add(KeyCode.LeftShift);
Check(Hotkey(KeyCode.F7, KeyCode.LeftControl, KeyCode.LeftShift), "multiple configured modifiers work");
Check(!Hotkey(KeyCode.None), "unbound shortcut is disabled");
pressed.Clear();
held.Add(KeyCode.F7);
Check(!Hotkey(), "holding F7 does not repeatedly toggle");
pressed.Add(KeyCode.F7);
held.Add(KeyCode.W);
speed.Release(2f);
Check(Hotkey() && speed.TryCycle(1f, 2), "held movement permits toggle on");
Check(Hotkey() && speed.Release(2f), "held movement permits toggle off");
Check(!Hotkey(KeyCode.F8), "unrelated key-down does not trigger rebound shortcut");
pressed.Add(KeyCode.F8);
Check(Hotkey(KeyCode.F8), "rebound shortcut works");
foreach (int maximum in new[] { 2, 4, 8 })
{
    var cycle = new SpeedOwnership();
    float current = 1f;
    for (int next = 2; next <= maximum; next *= 2)
    {
        Check(cycle.TryCycle(current, maximum) && cycle.SelectedSpeed == next && cycle.Active,
            $"cycle at cap {maximum}: {current}x -> {next}x");
        current = cycle.SelectedSpeed;
    }
    Check(cycle.TryCycle(current, maximum) && cycle.SelectedSpeed == 1f && !cycle.Active,
        $"cycle wraps from {maximum}x to 1x");
}
foreach (int owned in new[] { 2, 4, 8 })
{
    foreach (float external in new[] { 0f, 1f, 0.5f, 2f, 4f, 8f, 16f, float.NaN })
    {
        var cycle = new SpeedOwnership();
        while (cycle.SelectedSpeed < owned) cycle.TryCycle(cycle.SelectedSpeed, 8);
        Check(cycle.Release(external) == (external == owned) && !cycle.Active && cycle.SelectedSpeed == 1f,
            $"release owned {owned}x at observed {external}x restores only own scale");
    }
    var menuCycle = new SpeedOwnership();
    while (menuCycle.SelectedSpeed < owned) menuCycle.TryCycle(menuCycle.SelectedSpeed, 8);
    float cachedForResume = menuCycle.Release(owned) ? 1f : owned;
    Check(cachedForResume == 1f, $"settings prefix prevents cached {owned}x resume");
    Check(menuCycle.TryCycle(1f, 8) && menuCycle.SelectedSpeed == 2f,
        $"after cancel from {owned}x, next explicit activation starts at 2x");
}
foreach (int invalidMaximum in new[] { -1, 0, 1, 3, 16 })
    Check(!new SpeedOwnership().TryCycle(1f, invalidMaximum), $"reject unsupported cap {invalidMaximum}");
var stolen = new SpeedOwnership();
stolen.TryCycle(1f, 8);
Check(!stolen.TryCycle(16f, 8) && stolen.SelectedSpeed == 2f, "cycling cannot overwrite native sleep takeover");
bool backgroundValue = false;
int backgroundWrites = 0;
var background = new BackgroundExecution(() => backgroundValue, value => { backgroundValue = value; backgroundWrites++; });
background.Enable();
Check(backgroundValue && backgroundWrites == 1, "fast-forward enables background execution");
background.Enable();
Check(backgroundValue && backgroundWrites == 1, "cycling accelerated speeds retains original background setting");
background.Restore();
Check(!backgroundValue && backgroundWrites == 2, "ending fast-forward restores background setting");
background.Restore();
Check(backgroundWrites == 2, "repeated shutdown does not write background setting again");
backgroundValue = true;
background.Enable();
background.Restore();
Check(backgroundValue && backgroundWrites == 2, "existing native background execution is preserved");
backgroundValue = false;
background.Enable();
backgroundValue = false; // another owner changed it
background.Restore();
Check(!backgroundValue && backgroundWrites == 3, "release does not overwrite an external background-setting change");
background.Enable();
background.Restore();
Check(!backgroundValue && backgroundWrites == 5, "fresh activation after shutdown still restores correctly");
foreach (int owned in new[] { 1, 2, 4, 8 })
{
    foreach (int cap in new[] { 1, 2, 4, 8 })
    {
        var limited = new SpeedOwnership();
        while (limited.SelectedSpeed < owned) limited.TryCycle(limited.SelectedSpeed, 8);
        Check(limited.TryLimit(owned, cap) == (owned > cap) && limited.SelectedSpeed == Math.Min(owned, cap),
            $"activity cap {cap} lowers {owned}x only when needed");
        float result = limited.SelectedSpeed;
        Check(!limited.TryLimit(result, 8) && limited.SelectedSpeed == result,
            $"ending activity never raises speed after {owned}x/cap {cap}");
        Check(!limited.TryLimit(result, cap), $"unchanged activity cap {cap} does not repeatedly write speed");
    }
}
foreach (float external in new[] { 0f, 1f, 2f, 4f, 16f, float.NaN })
{
    var limited = new SpeedOwnership();
    while (limited.SelectedSpeed < 8) limited.TryCycle(limited.SelectedSpeed, 8);
    Check(!limited.TryLimit(external, 2) && limited.SelectedSpeed == 8f,
        $"activity cap does not overwrite external scale {external}");
}
var activityCycle = new SpeedOwnership();
while (activityCycle.SelectedSpeed < 8) activityCycle.TryCycle(activityCycle.SelectedSpeed, 8);
Check(activityCycle.TryLimit(8f, 2), "8x downsteps to 2x on movement/inventory entry");
Check(activityCycle.TryCycle(2f, 2) && activityCycle.SelectedSpeed == 1f, "F7 while limited wraps 2x to normal");
Check(activityCycle.TryCycle(1f, 2) && activityCycle.SelectedSpeed == 2f, "F7 while limited can enable permitted 2x");
Check(activityCycle.TryCycle(2f, 8) && activityCycle.SelectedSpeed == 4f, "higher speed requires explicit cycle after activity ends");
foreach (int invalidCap in new[] { -1, 0, 3, 16 })
    Check(!activityCycle.TryLimit(4f, invalidCap), $"reject invalid activity cap {invalidCap}");
Console.WriteLine($"{checks} ownership/input checks passed. Input states are simulated; live retest remains required.");

using System;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
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
// Model the real save's synchronous start and next-frame completion.
foreach (int selected in new[] { 2, 4, 8 })
{
    var savingSpeed = new SpeedOwnership();
    while (savingSpeed.SelectedSpeed < selected) savingSpeed.TryCycle(savingSpeed.SelectedSpeed, 8);
    var saving = new AutosaveState();
    saving.Begin(keepFastForward: true); // CancelOnAutosave=false, FF active, not already busy
    Check(!saving.ShouldCancelSave, $"default autosave preserves owned {selected}x at save prefix");
    saving.End(busy: true); // DoSaveGame reaches WaitForEndOfFrame before SaveGame returns
    Check(!saving.BlocksBusySave(true) && savingSpeed.SelectedSpeed == selected,
        $"autosave busy period preserves {selected}x on the next Update");
    Check(!saving.BlocksBusySave(false) && savingSpeed.SelectedSpeed == selected,
        $"autosave completion keeps {selected}x without restoring or reacquiring speed");
    Check(saving.BlocksBusySave(true), "later unrelated busy state is not exempt");

    saving.Begin(keepFastForward: false); // CancelOnAutosave=true
    Check(saving.ShouldCancelSave && savingSpeed.Release(selected),
        $"enabled autosave cancellation releases owned {selected}x before save starts");
    saving.Reset();
    saving.End(busy: true);
    Check(saving.BlocksBusySave(true) && !savingSpeed.Active, "cancelled autosave stays at normal speed");
}
var autosaveState = new AutosaveState();
autosaveState.Begin(true);
autosaveState.End(true);
Check(autosaveState.ShouldCancelSave, "manual/bed/quit save after autosave still cancels");
autosaveState.Reset();
Check(autosaveState.BlocksBusySave(true), "manual cancellation clears autosave busy permission");
autosaveState.Begin(true);
autosaveState.End(false);
Check(autosaveState.BlocksBusySave(true), "rejected autosave cannot exempt a later save");
autosaveState.Begin(false);
Check(autosaveState.ShouldCancelSave, "autosave at normal speed cannot acquire acceleration");
autosaveState.End(true);
Check(autosaveState.BlocksBusySave(true), "activation stays blocked during an unowned save");
autosaveState.Begin(true);
autosaveState.Reset(); // save threw, or pause/bed/load/disable interrupted it
autosaveState.End(true);
Check(autosaveState.BlocksBusySave(true), "error or gameplay boundary clears permission even before save returns");
foreach (float native in new[] { 0f, 1f, 16f })
{
    var interrupted = new SpeedOwnership();
    interrupted.TryCycle(1f, 8);
    autosaveState.Begin(true);
    autosaveState.End(true);
    Check(!interrupted.Release(native), $"autosave continuation never overwrites external scale {native}");
    autosaveState.Reset();
    Check(autosaveState.BlocksBusySave(true), "external scale change revokes autosave permission");
}

// Read installed game IL without executing Unity; verify the exact patched call site.
var saveMethod = typeof(SaveLoadManager).GetMethod("SaveGame", new[] { typeof(bool) });
var replacement = typeof(SaveCallFixture).GetMethod(nameof(SaveCallFixture.Autosave));
// Cecil reads game IL without invoking Harmony's Mono-specific runtime helpers on .NET 10.
using var gameAssembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(typeof(SaveLoadManager).Assembly.Location);
var updateMethod = gameAssembly.MainModule.Types.Single(type => type.FullName == "SaveLoadManager")
    .Methods.Single(method => method.Name == "Update");
var opcodes = typeof(OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
    .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null))
    .ToDictionary(opcode => opcode.Value);
var originalCode = updateMethod.Body.Instructions.Select(instruction =>
{
    object operand = instruction.Operand;
    if (operand is Mono.Cecil.MethodReference method && method.FullName == "System.Void SaveLoadManager::SaveGame(System.Boolean)")
        operand = saveMethod;
    return new CodeInstruction(opcodes[instruction.OpCode.Value], operand);
}).ToList();
var rewritten = AutosavePatch.Rewrite(originalCode, saveMethod, replacement).ToList();
var originalCalls = originalCode.Select((instruction, index) => (instruction, index))
    .Where(item => item.instruction.Calls(saveMethod)).Select(item => item.index).ToArray();
Check(originalCalls.Length == 3, "installed game has two explicit saves followed by timer autosave");
Check(originalCalls.Take(2).All(index => rewritten[index].Calls(saveMethod)),
    "installed game's two explicit save calls are untouched");
Check(rewritten[originalCalls[2]].Calls(replacement) && rewritten[originalCalls[2]].opcode == OpCodes.Call,
    "only installed game's timer autosave is routed through the wrapper");
Check(originalCode.Count == rewritten.Count && originalCode[originalCalls[2]].Calls(saveMethod),
    "rewriting preserves instruction count and leaves original IL unchanged");
Check(originalCode.Select((instruction, index) => (instruction, index)).All(item =>
    item.index == originalCalls[2] || (item.instruction.opcode == rewritten[item.index].opcode &&
    Equals(item.instruction.operand, rewritten[item.index].operand))), "all other installed game instructions remain unchanged");
var sample = originalCode.Select(instruction => new CodeInstruction(instruction)).ToList();
var label = new DynamicMethod("labels", typeof(void), Type.EmptyTypes).GetILGenerator().DefineLabel();
sample[originalCalls[2]].labels.Add(label);
sample[originalCalls[2]].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
var labeled = AutosavePatch.Rewrite(sample, saveMethod, replacement).ToList();
Check(labeled[originalCalls[2]].labels.Contains(label) && labeled[originalCalls[2]].blocks.Count == 1,
    "autosave replacement retains branch labels and exception boundaries");
foreach (int count in new[] { 0, 1, 2, 4 })
{
    bool rejected = false;
    try
    {
        AutosavePatch.Rewrite(Enumerable.Range(0, count).Select(_ => new CodeInstruction(OpCodes.Callvirt, saveMethod)),
            saveMethod, replacement).ToList();
    }
    catch (InvalidOperationException) { rejected = true; }
    Check(rejected, $"unexpected {count}-save layout is rejected instead of patching another save");
}
Console.WriteLine($"{checks} ownership/input/autosave checks passed. Game IL is inspected; Unity gameplay is not executed.");

static class SaveCallFixture
{
    public static void Autosave(SaveLoadManager manager, bool compressed) { }
}

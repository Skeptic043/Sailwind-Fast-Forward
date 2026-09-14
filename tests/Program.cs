using System;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
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
// Exercise the production dispatcher; reset must run before limiting or cycling.
var cycleShortcut = new KeyboardShortcut(KeyCode.F7);
var resetShortcut = new KeyboardShortcut(KeyCode.F8);
foreach (int owned in new[] { 1, 2, 4, 8 })
{
    foreach (int cap in new[] { 1, 2, 4, 8 })
    {
        var resetting = new SpeedOwnership();
        while (resetting.SelectedSpeed < owned) resetting.TryCycle(resetting.SelectedSpeed, 8);
        pressed.Clear();
        pressed.UnionWith(new[] { KeyCode.F7, KeyCode.F8 });
        held.Clear();
        held.UnionWith(new[] { KeyCode.W, KeyCode.A, KeyCode.Mouse0, KeyCode.LeftShift });
        int cancelled = 0;
        float observed = owned;
        var action = HotkeyInput.Dispatch(resetting, observed, cap, true, cycleShortcut, resetShortcut,
            pressed.Contains, held.Contains, () =>
            {
                cancelled++;
                Check(resetting.SelectedSpeed == owned, $"reset runs before cap {cap} changes owned {owned}x");
                if (resetting.Release(observed)) observed = 1f;
            });
        Check(action == SpeedInputAction.Reset && cancelled == 1 && observed == 1f && !resetting.Active,
            $"reset wins cycle and cap {cap} from {owned}x with unrelated controls held");
    }
}
foreach (float external in new[] { 0f, 0.5f, 1f, 2f, 4f, 16f, float.NaN })
{
    var resetting = new SpeedOwnership();
    while (resetting.SelectedSpeed < 8) resetting.TryCycle(resetting.SelectedSpeed, 8);
    float observed = external;
    var action = HotkeyInput.Dispatch(resetting, observed, 2, true, cycleShortcut, resetShortcut,
        pressed.Contains, held.Contains, () => { if (resetting.Release(observed)) observed = 1f; });
    Check(action == SpeedInputAction.Reset && observed.Equals(external) && !resetting.Active,
        $"reset callback releases ownership without overwriting external/native {external}x");
}
var routed = new SpeedOwnership();
routed.TryCycle(1f, 8);
int resetCalls = 0;
Check(HotkeyInput.Dispatch(routed, 2f, 8, false, cycleShortcut, resetShortcut,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.None && resetCalls == 0 && routed.Active,
    "unfocused input triggers neither reset nor cycle");
Check(HotkeyInput.Dispatch(routed, 2f, 1, false, cycleShortcut, resetShortcut,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.Limit && resetCalls == 0 && !routed.Active,
    "background activity limiting still works without accepting keys");
pressed.Clear();
pressed.Add(KeyCode.F8);
Check(HotkeyInput.Dispatch(routed, 1f, 8, true, cycleShortcut, new KeyboardShortcut(KeyCode.None),
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.None && resetCalls == 0,
    "disabled reset binding ignores F8");
var chordReset = new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl);
Check(HotkeyInput.Dispatch(routed, 1f, 8, true, cycleShortcut, chordReset,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.None,
    "reset chord requires configured modifier");
held.Add(KeyCode.LeftControl);
Check(HotkeyInput.Dispatch(routed, 1f, 8, true, cycleShortcut, chordReset,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.Reset && resetCalls == 1,
    "reset chord accepts modifiers and unrelated held controls");
Check(HotkeyInput.Dispatch(routed, 1f, 8, true, chordReset, chordReset,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.Reset && resetCalls == 2 && !routed.Active,
    "identical cycle and reset chords deterministically choose reset");
pressed.Clear();
held.Add(KeyCode.F8);
Check(HotkeyInput.Dispatch(routed, 1f, 8, true, cycleShortcut, resetShortcut,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.None && resetCalls == 2,
    "held reset key does not repeatedly dispatch");
pressed.Add(KeyCode.F7);
Check(HotkeyInput.Dispatch(routed, 1f, 8, true, cycleShortcut, resetShortcut,
    pressed.Contains, held.Contains, () => resetCalls++) == SpeedInputAction.Cycle && routed.SelectedSpeed == 2f,
    "cycle still acquires speed when reset is not pressed");

// Initialize BepInEx only inside a retained scratch directory before ConfigFile's
// static CoreConfig can run. No installed loader/profile paths are used or changed.
string configScratch = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
    ".local", "config-checks", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(configScratch);
typeof(Paths).GetMethod("SetExecutablePath", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
    .Invoke(null, new object[] { Path.Combine(configScratch, "Fixture.exe"), Path.Combine(configScratch, "BepInEx"),
        Path.Combine(configScratch, "Managed"), Array.Empty<string>() });
Check(Paths.ConfigPath.StartsWith(configScratch + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
    "BepInEx config paths are isolated in test scratch");
ConfigurationChecks.Run(configScratch, Check);
ShortcutChecks.Run(configScratch, Check);
HoldChecks.Run(Check);
HoldOverrideChecks.Run(Check);
AutosaveChecks.Run(Check);
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
    Check(saving.TryEnterSave(), $"default autosave preserves owned {selected}x at save prefix");
    saving.End(busy: true); // DoSaveGame reaches WaitForEndOfFrame before SaveGame returns
    Check(!saving.BlocksBusySave(true) && savingSpeed.SelectedSpeed == selected,
        $"autosave busy period preserves {selected}x on the next Update");
    Check(!saving.BlocksBusySave(false) && savingSpeed.SelectedSpeed == selected,
        $"autosave completion keeps {selected}x without restoring or reacquiring speed");
    Check(saving.BlocksBusySave(true), "later unrelated busy state is not exempt");

    saving.Begin(keepFastForward: false); // CancelOnAutosave=true
    Check(!saving.TryEnterSave() && savingSpeed.Release(selected),
        $"enabled autosave cancellation releases owned {selected}x before save starts");
    saving.Reset();
    saving.End(busy: true);
    Check(saving.BlocksBusySave(true) && !savingSpeed.Active, "cancelled autosave stays at normal speed");
}
var autosaveState = new AutosaveState();
autosaveState.Begin(true);
Check(autosaveState.TryEnterSave(), "continued autosave consumes its single prefix permission");
autosaveState.End(true);
Check(!autosaveState.TryEnterSave(), "manual/bed/quit save after autosave still cancels");
autosaveState.Reset();
Check(autosaveState.BlocksBusySave(true), "manual cancellation clears autosave busy permission");
autosaveState.Begin(true);
autosaveState.TryEnterSave();
autosaveState.End(false);
Check(autosaveState.BlocksBusySave(true), "rejected autosave cannot exempt a later save");
autosaveState.Begin(false);
Check(!autosaveState.TryEnterSave(), "autosave at normal speed cannot acquire acceleration");
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
    autosaveState.TryEnterSave();
    autosaveState.End(true);
    Check(!interrupted.Release(native), $"autosave continuation never overwrites external scale {native}");
    autosaveState.Reset();
    Check(autosaveState.BlocksBusySave(true), "external scale change revokes autosave permission");
}

if (args.Contains("--config-input-only"))
{
    Console.WriteLine($"{checks} ownership/input/config/autosave checks passed; game IL checks skipped by request.");
    return;
}

// Read installed game IL without executing Unity; verify the exact patched call site.
var saveMethod = typeof(SaveLoadManager).GetMethod("SaveGame", new[] { typeof(bool) });
CompatibilityChecks.Run(Check);
var replacement = typeof(SaveCallFixture).GetMethod(nameof(SaveCallFixture.Autosave));
// Cecil reads game IL without invoking Harmony's Mono-specific runtime helpers on .NET 10.
using var gameAssembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(typeof(SaveLoadManager).Assembly.Location);
var updateMethod = gameAssembly.MainModule.Types.Single(type => type.FullName == "SaveLoadManager")
    .Methods.Single(method => method.Name == "Update");
var opcodes = typeof(OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
    .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null))
    .ToDictionary(opcode => opcode.Value);
var labelGenerator = new DynamicMethod("installedLabels", typeof(void), Type.EmptyTypes).GetILGenerator();
var labels = updateMethod.Body.Instructions.ToDictionary(instruction => instruction, _ => labelGenerator.DefineLabel());
var originalCode = updateMethod.Body.Instructions.Select(instruction =>
{
    object operand = instruction.Operand;
    if (operand is Mono.Cecil.MethodReference method)
        operand = typeof(SaveLoadManager).Module.ResolveMethod(method.MetadataToken.ToInt32());
    else if (operand is Mono.Cecil.FieldReference field)
        operand = typeof(SaveLoadManager).Module.ResolveField(field.MetadataToken.ToInt32());
    else if (operand is Mono.Cecil.Cil.Instruction target)
        operand = labels[target];
    var result = new CodeInstruction(opcodes[instruction.OpCode.Value], operand);
    result.labels.Add(labels[instruction]);
    return result;
}).ToList();
HarmonyNormalizationChecks.Run(updateMethod.Body,
    typeof(SaveLoadManager).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
    saveMethod, replacement, Check);
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
    var changed = Enumerable.Range(0, count).Select(_ => new CodeInstruction(OpCodes.Callvirt, saveMethod)).ToList();
    var retained = AutosavePatch.Rewrite(changed, saveMethod, replacement, out string failure).ToList();
    Check(failure != null && retained.SequenceEqual(changed),
        $"unexpected {count}-save layout returns every original instruction without patching another save");
}
foreach (string mutation in new[] { "reorder", "branch", "field", "call", "timer-argument" })
{
    var changed = originalCode.Select(instruction => new CodeInstruction(instruction)).ToList();
    switch (mutation)
    {
        case "reorder":
            var first = changed[originalCalls[0] - 1];
            changed[originalCalls[0] - 1] = changed[originalCalls[2] - 1];
            changed[originalCalls[2] - 1] = first;
            break;
        case "branch": changed.First(instruction => instruction.opcode == OpCodes.Ble_S).operand = changed[0].labels[0]; break;
        case "field": changed.First(instruction => instruction.operand is System.Reflection.FieldInfo f && f.Name == "enableAutosave").operand =
            typeof(SaveLoadManager).GetField("save"); break;
        case "call": changed.First(instruction => instruction.operand is System.Reflection.MethodInfo m && m.Name == "get_deltaTime").operand = saveMethod; break;
        case "timer-argument": changed[originalCalls[2] - 1].opcode = OpCodes.Ldc_I4_0; break;
    }
    var retained = AutosavePatch.Rewrite(changed, saveMethod, replacement, out string failure).ToList();
    Check(failure != null && retained.SequenceEqual(changed),
        "semantic Update guard retains original IL for " + mutation + " mutation");
}
Console.WriteLine($"{checks} ownership/input/config/autosave checks passed. Game IL is inspected; Unity gameplay is not executed.");

static class SaveCallFixture
{
    public static void Autosave(SaveLoadManager manager, bool compressed) { }
}

static class ConfigurationChecks
{
    // Keep real ConfigFile initialization after the scratch-only Paths setup.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Run(string scratch, Action<bool, string> check)
    {
        string freshPath = Path.Combine(scratch, "fresh.cfg");
        var freshFile = new ConfigFile(freshPath, false);
        var fresh = new PluginSettings(freshFile);
        freshFile.Save();
        string generated = File.ReadAllText(freshPath);
        check(fresh.Hotkey.Value.Equals(new KeyboardShortcut(KeyCode.F7)) &&
            fresh.ResetHotkey.Value.Equals(new KeyboardShortcut(KeyCode.F8)), "real config binds F7 cycle and F8 reset defaults");
        check(fresh.HoldHotkey.Value.Equals(new KeyboardShortcut(KeyCode.F9)) && fresh.HoldSpeed.Value == 4,
            "real config binds F9 hold and 4x hold defaults");
        check(fresh.MaxSpeed.Value == 4 && fresh.MovementInventoryMaxSpeed.Value == 2 &&
            !fresh.CancelOnAutosave.Value && fresh.ShowIndicator.Value, "all previous config defaults are preserved");
        string[] entries = { "[Controls]", "\nHotkey =", "\nResetHotkey =", "\nHoldHotkey =", "[Display]", "\nShowIndicator =",
            "[Simulation]", "\nMaxSpeed =", "\nHoldSpeed =", "\nMovementInventoryMaxSpeed =", "\nCancelOnAutosave =" };
        int previousIndex = -1;
        foreach (string entry in entries)
        {
            int index = generated.IndexOf(entry, StringComparison.Ordinal);
            check(index > previousIndex, "generated config has ordered " + entry.Trim());
            previousIndex = index;
        }
        check(generated.Contains("# Acceptable values: 2, 4, 8") && generated.Contains("# Acceptable values: 1, 2, 4, 8"),
            "real serializer documents both supported speed limits");

        string independentPath = Path.Combine(scratch, "independent-cycle-hold.cfg");
        File.WriteAllText(independentPath, "[Simulation]\nMaxSpeed = 4\nHoldSpeed = 8\nMovementInventoryMaxSpeed = 2\n");
        var independentFile = new ConfigFile(independentPath, false);
        var independent = new PluginSettings(independentFile);
        independentFile.Save();
        var independentReloaded = new PluginSettings(new ConfigFile(independentPath, false));
        check(independent.MaxSpeed.Value == 4 && independent.HoldSpeed.Value == 8 &&
            independentReloaded.MaxSpeed.Value == 4 && independentReloaded.HoldSpeed.Value == 8 &&
            independentReloaded.MovementInventoryMaxSpeed.Value == 2,
            "existing cycle 4 and hold 8 settings remain independent after real config save/reload");

        string oldPath = Path.Combine(scratch, "existing-custom.cfg");
        File.WriteAllText(oldPath, "[Controls]\nHotkey = F6 + LeftControl\nLegacyControl = retained\n\n" +
            "[Display]\nShowIndicator = false\n\n[Simulation]\nMaxSpeed = 8\nMovementInventoryMaxSpeed = 4\nCancelOnAutosave = true\n\n" +
            "[OtherOwner]\nExtraSetting = preserve me\n");
        var oldFile = new ConfigFile(oldPath, false);
        var old = new PluginSettings(oldFile);
        check(old.Hotkey.Value.Equals(new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl)) &&
            old.MaxSpeed.Value == 8 && old.MovementInventoryMaxSpeed.Value == 4 && old.CancelOnAutosave.Value && !old.ShowIndicator.Value,
            "binding preserves every existing customized setting");
        check(old.ResetHotkey.Value.Equals(new KeyboardShortcut(KeyCode.F8)), "existing config gains only the new F8 default");
        foreach (var shortcut in new[] { new KeyboardShortcut(KeyCode.F9), new KeyboardShortcut(KeyCode.F10, KeyCode.LeftControl, KeyCode.LeftShift),
            new KeyboardShortcut(KeyCode.None) })
        {
            old.ResetHotkey.Value = shortcut;
            oldFile.Save();
            var reloadedFile = new ConfigFile(oldPath, false);
            var reloaded = new PluginSettings(reloadedFile);
            reloadedFile.Reload();
            check(reloaded.ResetHotkey.Value.Equals(shortcut), "real bind/save/reload preserves reset " + shortcut);
            check(reloaded.Hotkey.Value.Equals(old.Hotkey.Value) && reloaded.MaxSpeed.Value == 8 &&
                reloaded.MovementInventoryMaxSpeed.Value == 4 && reloaded.CancelOnAutosave.Value && !reloaded.ShowIndicator.Value,
                "reset changes preserve original custom values after reload");
            string retained = File.ReadAllText(oldPath);
            check(retained.Contains("LegacyControl = retained") && retained.Contains("ExtraSetting = preserve me"),
                "unknown entries survive binding and serialization");
        }
        foreach (var legacyCycle in new[] { new KeyboardShortcut(KeyCode.F8), new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl) })
        {
            string legacyPath = Path.Combine(scratch, "legacy-f8-" + legacyCycle.Modifiers.Count() + ".cfg");
            File.WriteAllText(legacyPath, "[Controls]\nHotkey = " + legacyCycle.Serialize() + "\n");
            var legacyFile = new ConfigFile(legacyPath, false);
            var legacy = new PluginSettings(legacyFile);
            check(legacy.Hotkey.Value.Equals(legacyCycle) && legacy.ResetHotkey.Value.MainKey == KeyCode.None,
                "legacy " + legacyCycle + " cycle gets disabled reset default without losing its binding");
            var legacySpeed = new SpeedOwnership();
            var keys = new HashSet<KeyCode>(legacyCycle.Modifiers) { KeyCode.W, KeyCode.Mouse0 };
            var routedAction = HotkeyInput.Dispatch(legacySpeed, 1f, 4, true, legacy.Hotkey.Value, legacy.ResetHotkey.Value,
                key => key == KeyCode.F8, keys.Contains, () => throw new Exception("legacy cycle was swallowed by reset"));
            check(routedAction == SpeedInputAction.Cycle && legacySpeed.SelectedSpeed == 2f,
                "real loaded legacy " + legacyCycle + " still cycles through production dispatcher");
            legacyFile.Save();
            var legacyReloaded = new PluginSettings(new ConfigFile(legacyPath, false));
            check(legacyReloaded.Hotkey.Value.Equals(legacyCycle) && legacyReloaded.ResetHotkey.Value.MainKey == KeyCode.None,
                "legacy F8 cycle and disabled reset survive save/reload");

            File.WriteAllText(legacyPath, "[Controls]\nHotkey = " + legacyCycle.Serialize() + "\nResetHotkey = F8\n");
            var explicitReset = new PluginSettings(new ConfigFile(legacyPath, false));
            check(explicitReset.ResetHotkey.Value.MainKey == KeyCode.F8,
                "explicit reset remains configured despite legacy F8 cycle overlap");
            int cancelled = 0;
            check(HotkeyInput.Dispatch(legacySpeed, 2f, 4, true, explicitReset.Hotkey.Value, explicitReset.ResetHotkey.Value,
                key => key == KeyCode.F8, keys.Contains, () => cancelled++) == SpeedInputAction.Reset && cancelled == 1,
                "explicit overlapping reset still takes priority");
        }
        foreach (string existing in new[] { "Hotkey = F9", "Hotkey = F9 + LeftControl", "ResetHotkey = F9", "ResetHotkey = F9 + LeftShift" })
        {
            string collisionPath = Path.Combine(scratch, "hold-collision.cfg");
            File.WriteAllText(collisionPath, "[Controls]\n" + existing + "\n");
            var collision = new PluginSettings(new ConfigFile(collisionPath, false));
            check(collision.HoldHotkey.Value.MainKey == KeyCode.None, "new hold default preserves existing " + existing);
            File.WriteAllText(collisionPath, "[Controls]\n" + existing + "\nHoldHotkey = F9 + LeftAlt\n[Simulation]\nHoldSpeed = 8\n");
            var explicitFile = new ConfigFile(collisionPath, false);
            var explicitHold = new PluginSettings(explicitFile);
            explicitFile.Save(); explicitFile.Reload();
            check(explicitHold.HoldHotkey.Value.Equals(new KeyboardShortcut(KeyCode.F9, KeyCode.LeftAlt)) && explicitHold.HoldSpeed.Value == 8,
                "explicit hold chord/speed survive collision binding and reload");
            explicitHold.HoldHotkey.Value = new KeyboardShortcut(KeyCode.None);
            explicitFile.Save();
            check(new PluginSettings(new ConfigFile(collisionPath, false)).HoldHotkey.Value.MainKey == KeyCode.None,
                "disabled hold binding survives real save/reload");
        }
        string blankPath = Path.Combine(scratch, "blank-shortcuts.cfg");
        File.WriteAllText(blankPath, "[Controls]\nHotkey =\nResetHotkey =\nHoldHotkey =\n");
        var blankFile = new ConfigFile(blankPath, false);
        var blank = new PluginSettings(blankFile);
        check(blank.Hotkey.Value.MainKey == KeyCode.None && blank.ResetHotkey.Value.MainKey == KeyCode.None &&
            blank.HoldHotkey.Value.MainKey == KeyCode.None, "real config accepts blank values to disable all three shortcuts");
        blankFile.Save(); blankFile.Reload();
        check(blank.Hotkey.Value.MainKey == KeyCode.None && blank.ResetHotkey.Value.MainKey == KeyCode.None &&
            blank.HoldHotkey.Value.MainKey == KeyCode.None, "blank-disabled shortcuts remain disabled after real save/reload");
        check(File.ReadAllText(blankPath).Contains("Hotkey = " + Environment.NewLine), "blank-disabled shortcuts keep their blank text");
        Console.WriteLine("Generated config sample: " + freshPath);
        Console.WriteLine("Preserved custom config sample: " + oldPath);
    }
}

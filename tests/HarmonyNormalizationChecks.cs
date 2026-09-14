using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SailwindFastForward;

internal static class HarmonyNormalizationChecks
{
    private static MethodInfo save, replacement;
    private static List<CodeInstruction> captured;
    private static Action<bool, string> check;

    internal static void Run(Mono.Cecil.Cil.MethodBody body, MethodInfo original, MethodInfo saveMethod,
        MethodInfo replacementMethod, Action<bool, string> assertion)
    {
        save = saveMethod;
        replacement = replacementMethod;
        check = assertion;
        // Exercise the installed HarmonyX reader -> Prepare -> NormalizeInstructions
        // -> real transpiler invocation. This only transforms in-memory IL; no detour,
        // game method invocation, or Unity player is involved.
        Type manipulatorType = typeof(CodeInstruction).Assembly.GetType("HarmonyLib.Internal.Patching.ILManipulator", true);
        object manipulator = Activator.CreateInstance(manipulatorType, new object[] { body, false });
        manipulatorType.GetMethod("AddTranspiler").Invoke(manipulator,
            new object[] { typeof(HarmonyNormalizationChecks).GetMethod(nameof(Transpile), BindingFlags.Static | BindingFlags.Public) });
        var generator = new DynamicMethod("normalizationFixture", typeof(void), Type.EmptyTypes).GetILGenerator();
        var result = (List<CodeInstruction>)manipulatorType.GetMethod("GetInstructions", new[] { typeof(ILGenerator), typeof(MethodBase) })
            .Invoke(manipulator, new object[] { generator, original });
        var calls = captured.Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.Calls(save)).Select(item => item.index).ToArray();
        check(calls.Length == 3 && result[calls[0]].Calls(save) && result[calls[1]].Calls(save) &&
            result[calls[2]].Calls(replacement), "actual HarmonyX pipeline accepts guard and rewrites only timer autosave");
        check(result.Count == captured.Count, "actual HarmonyX pipeline preserves instruction count");
        check(result.Select((instruction, index) => (instruction, index)).All(item =>
            (item.index == calls[2] || (item.instruction.opcode == captured[item.index].opcode &&
                Equals(item.instruction.operand, captured[item.index].operand))) &&
            item.instruction.labels.SequenceEqual(captured[item.index].labels) &&
            item.instruction.blocks.SequenceEqual(captured[item.index].blocks)),
            "actual HarmonyX pipeline preserves every other instruction, label and exception block");

        foreach (string mutation in new[] { "target", "condition", "unordered-condition", "member" })
        {
            var changed = captured.Select(instruction => new CodeInstruction(instruction)).ToList();
            if (mutation == "target")
                changed.First(instruction => instruction.opcode == OpCodes.Brfalse).operand = changed.Last().labels.First();
            if (mutation == "condition")
                changed.First(instruction => instruction.opcode == OpCodes.Brfalse).opcode = OpCodes.Brtrue;
            if (mutation == "unordered-condition")
                changed.First(instruction => instruction.opcode == OpCodes.Bgt_Un).opcode = OpCodes.Bgt;
            if (mutation == "member")
                changed.First(instruction => instruction.operand is FieldInfo field && field.Name == "enableAutosave").operand =
                    typeof(SaveLoadManager).GetField("save");
            bool rejected = false;
            try { AutosavePatch.Rewrite(changed, save, replacement).ToList(); }
            catch (InvalidOperationException) { rejected = true; }
            check(rejected, "normalized HarmonyX input still rejects changed " + mutation);
        }
    }

    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        captured = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        check(captured[2].opcode == OpCodes.Brfalse, "actual HarmonyX widens original IL_0006 brfalse.s before transpiler");
        check(captured.All(instruction => instruction.opcode.OperandType != OperandType.ShortInlineBrTarget),
            "actual HarmonyX normalizes every short branch before transpiler");
        return AutosavePatch.Rewrite(captured, save, replacement);
    }
}

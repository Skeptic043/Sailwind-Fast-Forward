using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SailwindFastForward
{
    internal static class AutosavePatch
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static FieldInfo Field(Type type, string name) => type.GetField(name, Members);
        internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions,
            MethodInfo saveMethod, MethodInfo autosaveMethod)
            => Rewrite(instructions, saveMethod, autosaveMethod, out _);

        internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions,
            MethodInfo saveMethod, MethodInfo autosaveMethod, out string failure)
        {
            var original = instructions.ToList();
            try
            {
                var rewritten = RewriteVerified(original, saveMethod, autosaveMethod);
                failure = null;
                return rewritten;
            }
            catch (Exception error)
            {
                failure = error.Message;
                // No partially modified IL escapes a failed semantic check.
                return original;
            }
        }

        private static IEnumerable<CodeInstruction> RewriteVerified(IEnumerable<CodeInstruction> instructions,
            MethodInfo saveMethod, MethodInfo autosaveMethod)
        {
            if (saveMethod == null || autosaveMethod == null)
                throw new MissingMethodException("Save wrapper methods are unavailable.");
            var code = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
            var expected = ExpectedUpdate(saveMethod);
            if (code.Count != expected.Length)
                throw new InvalidOperationException("Unexpected SaveLoadManager.Update instruction count.");
            for (int i = 0; i < code.Count; i++)
            {
                var shape = expected[i];
                bool operandMatches;
                if (shape.Operand is Branch branch)
                {
                    int target = Array.FindIndex(expected, entry => entry.Offset == branch.Offset);
                    // Label is absent from this project's netstandard2 reference surface;
                    // compare Harmony's boxed branch label with its target labels.
                    var labels = target < 0 ? null : (IEnumerable)typeof(CodeInstruction).GetField("labels").GetValue(code[target]);
                    operandMatches = labels != null && code[i].operand?.GetType().FullName == "System.Reflection.Emit.Label" &&
                        labels.Cast<object>().Any(label => Equals(label, code[i].operand));
                }
                else operandMatches = Equals(code[i].operand, shape.Operand);
                bool opcodeMatches = code[i].opcode == shape.Opcode ||
                    (shape.Operand is Branch && CanonicalBranch(code[i].opcode) == CanonicalBranch(shape.Opcode));
                if (!opcodeMatches || !operandMatches)
                    throw new InvalidOperationException($"Unexpected SaveLoadManager.Update shape at IL_{shape.Offset:x4}.");
            }
            int timerSave = Array.FindIndex(expected, entry => entry.Offset == 0x008d);
            code[timerSave].opcode = OpCodes.Call;
            code[timerSave].operand = autosaveMethod;
            return code;
        }

        // HarmonyX widens branches before invoking transpilers. Accept only the
        // equivalent encodings used by this inspected method; conditions and
        // exact target labels are still checked independently above.
        private static OpCode CanonicalBranch(OpCode opcode)
        {
            if (opcode == OpCodes.Brfalse_S) return OpCodes.Brfalse;
            if (opcode == OpCodes.Brtrue_S) return OpCodes.Brtrue;
            if (opcode == OpCodes.Ble_S) return OpCodes.Ble;
            if (opcode == OpCodes.Bgt_Un_S) return OpCodes.Bgt_Un;
            return opcode;
        }

        // Exact inspected semantic layout, including the first two explicit saves,
        // timer conditions and branch destinations. Runtime metadata tokens are not hashes.
        private static Shape[] ExpectedUpdate(MethodInfo save)
        {
            var saveFlag = Field(typeof(SaveLoadManager), "save");
            var compressed = Field(typeof(SaveLoadManager), "saveCompressed");
            var load = Field(typeof(SaveLoadManager), "load");
            var timer = Field(typeof(SaveLoadManager), "gctimer");
            var interval = Field(typeof(Settings), "autosaveInterval");
            return new[] {
                S(0x00, OpCodes.Ldarg_0), S(0x01, OpCodes.Ldfld, saveFlag), S(0x06, OpCodes.Brfalse_S, new Branch(0x16)),
                S(0x08, OpCodes.Ldarg_0), S(0x09, OpCodes.Ldc_I4_0), S(0x0a, OpCodes.Stfld, saveFlag),
                S(0x0f, OpCodes.Ldarg_0), S(0x10, OpCodes.Ldc_I4_0), S(0x11, OpCodes.Call, save),
                S(0x16, OpCodes.Ldarg_0), S(0x17, OpCodes.Ldfld, compressed), S(0x1c, OpCodes.Brfalse_S, new Branch(0x2c)),
                S(0x1e, OpCodes.Ldarg_0), S(0x1f, OpCodes.Ldc_I4_0), S(0x20, OpCodes.Stfld, compressed),
                S(0x25, OpCodes.Ldarg_0), S(0x26, OpCodes.Ldc_I4_1), S(0x27, OpCodes.Call, save),
                S(0x2c, OpCodes.Ldarg_0), S(0x2d, OpCodes.Ldfld, load), S(0x32, OpCodes.Brfalse_S, new Branch(0x42)),
                S(0x34, OpCodes.Ldarg_0), S(0x35, OpCodes.Ldc_I4_0), S(0x36, OpCodes.Stfld, load),
                S(0x3b, OpCodes.Ldarg_0), S(0x3c, OpCodes.Ldc_I4_0),
                S(0x3d, OpCodes.Call, typeof(SaveLoadManager).GetMethod("LoadGame", Members, null, new[] { typeof(int) }, null)),
                S(0x42, OpCodes.Ldsfld, interval), S(0x47, OpCodes.Ldc_I4_0), S(0x48, OpCodes.Ble_S, new Branch(0xa4)),
                S(0x4a, OpCodes.Ldarg_0), S(0x4b, OpCodes.Ldfld, timer), S(0x50, OpCodes.Ldc_R4, 0f),
                S(0x55, OpCodes.Bgt_Un_S, new Branch(0x92)),
                S(0x57, OpCodes.Ldsfld, Field(typeof(CrateInventoryUI), "instance")),
                S(0x5c, OpCodes.Ldfld, Field(typeof(CrateInventoryUI), "showingUI")),
                S(0x61, OpCodes.Brtrue_S, new Branch(0x92)), S(0x63, OpCodes.Ldarg_0), S(0x64, OpCodes.Ldsfld, interval),
                S(0x69, OpCodes.Conv_R4), S(0x6a, OpCodes.Ldc_R4, 60f), S(0x6f, OpCodes.Mul), S(0x70, OpCodes.Stfld, timer),
                S(0x75, OpCodes.Ldarg_0), S(0x76, OpCodes.Ldfld, Field(typeof(SaveLoadManager), "enableAutosave")),
                S(0x7b, OpCodes.Brfalse_S, new Branch(0x92)),
                S(0x7d, OpCodes.Ldsfld, Field(typeof(GameState), "sleeping")), S(0x82, OpCodes.Brtrue_S, new Branch(0x92)),
                S(0x84, OpCodes.Ldsfld, Field(typeof(GameState), "recovering")), S(0x89, OpCodes.Brtrue_S, new Branch(0x92)),
                S(0x8b, OpCodes.Ldarg_0), S(0x8c, OpCodes.Ldc_I4_1), S(0x8d, OpCodes.Call, save),
                S(0x92, OpCodes.Ldarg_0), S(0x93, OpCodes.Ldarg_0), S(0x94, OpCodes.Ldfld, timer),
                S(0x99, OpCodes.Call, typeof(Time).GetProperty("deltaTime", Members).GetGetMethod(true)),
                S(0x9e, OpCodes.Sub), S(0x9f, OpCodes.Stfld, timer), S(0xa4, OpCodes.Ret)
            };
        }

        private static Shape S(int offset, OpCode opcode, object operand = null) => new Shape(offset, opcode, operand);
        private sealed class Branch { internal readonly int Offset; internal Branch(int offset) { Offset = offset; } }
        private sealed class Shape
        {
            internal readonly int Offset;
            internal readonly OpCode Opcode;
            internal readonly object Operand;
            internal Shape(int offset, OpCode opcode, object operand) { Offset = offset; Opcode = opcode; Operand = operand; }
        }
    }
}

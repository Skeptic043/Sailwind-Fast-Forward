using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SailwindFastForward
{
    internal static class AutosavePatch
    {
        internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions,
            MethodInfo saveMethod, MethodInfo autosaveMethod)
        {
            var code = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
            var calls = code.Where(instruction => instruction.Calls(saveMethod)).ToList();
            // Inspected Update: save flag, compressed-save flag, then timer autosave.
            // Reject a changed layout rather than classify a different save as an autosave.
            if (calls.Count != 3)
                throw new InvalidOperationException("Unexpected SaveLoadManager.Update save-call layout.");
            calls[2].opcode = OpCodes.Call;
            calls[2].operand = autosaveMethod;
            return code;
        }
    }
}

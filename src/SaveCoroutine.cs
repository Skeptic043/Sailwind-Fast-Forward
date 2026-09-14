using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SailwindFastForward
{
    internal static class SaveCoroutine
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<short, OpCode> Opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(opcode => opcode.Value);

        internal static MethodInfo FindMoveNext(Type manager)
        {
            var factories = manager.GetMethods(Declared).Where(method => method.Name == "DoSaveGame").ToArray();
            if (factories.Length != 1 || !typeof(IEnumerator).IsAssignableFrom(factories[0].ReturnType))
                throw new MissingMethodException("Expected one IEnumerator DoSaveGame factory.");
            var factory = factories[0];
            var constructors = ReadConstructors(factory).ToArray();
            if (constructors.Length != 1)
                throw new NotSupportedException("DoSaveGame must construct exactly one iterator.");
            Type iterator = constructors[0].DeclaringType;
            var attribute = factory.GetCustomAttribute<IteratorStateMachineAttribute>();
            if (iterator.DeclaringType != manager || !typeof(IEnumerator).IsAssignableFrom(iterator) ||
                !iterator.IsDefined(typeof(CompilerGeneratedAttribute), false) ||
                (attribute != null && attribute.StateMachineType != iterator))
                throw new NotSupportedException("DoSaveGame iterator mapping is not a unique nested compiler-generated state machine.");
            // Validate the actual IEnumerator entry point, including explicit implementations.
            var map = iterator.GetInterfaceMap(typeof(IEnumerator));
            var target = map.TargetMethods[Array.FindIndex(map.InterfaceMethods, method => method.Name == "MoveNext")];
            if (target.DeclaringType != iterator || target.IsStatic || target.ReturnType != typeof(bool) ||
                target.GetParameters().Length != 0 || target.GetMethodBody() == null)
                throw new MissingMethodException("DoSaveGame iterator has no patchable IEnumerator.MoveNext.");
            return target;
        }

        private static IEnumerable<ConstructorInfo> ReadConstructors(MethodInfo factory)
        {
            byte[] il = factory.GetMethodBody()?.GetILAsByteArray();
            if (il == null) throw new NotSupportedException("DoSaveGame has no readable factory body.");
            var stack = new Stack<bool>();
            ConstructorInfo constructed = null;
            bool returned = false;
            for (int position = 0; position < il.Length;)
            {
                short value = il[position++];
                if (value == 0xfe)
                {
                    if (position == il.Length) throw new NotSupportedException("Truncated factory opcode.");
                    value = unchecked((short)(0xfe00 | il[position++]));
                }
                if (!Opcodes.TryGetValue(value, out var opcode)) throw new NotSupportedException("Unknown factory opcode.");
                int size;
                switch (opcode.OperandType)
                {
                    case OperandType.InlineNone: size = 0; break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: size = 1; break;
                    case OperandType.InlineVar: size = 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: size = 8; break;
                    case OperandType.InlineSwitch:
                        if (il.Length - position < 4) throw new NotSupportedException("Truncated factory switch.");
                        int count = BitConverter.ToInt32(il, position);
                        if (count < 0 || count > (il.Length - position - 4) / 4)
                            throw new NotSupportedException("Invalid factory switch.");
                        size = 4 + count * 4;
                        break;
                    default: size = 4; break;
                }
                if (size > il.Length - position) throw new NotSupportedException("Truncated factory operand.");
                if (returned && opcode != OpCodes.Nop)
                    throw new NotSupportedException("DoSaveGame has instructions after its iterator return.");
                if (opcode == OpCodes.Newobj)
                {
                    if (constructed != null) throw new NotSupportedException("DoSaveGame constructs multiple objects.");
                    constructed = (ConstructorInfo)factory.Module.ResolveMethod(BitConverter.ToInt32(il, position),
                        factory.DeclaringType.GetGenericArguments(), factory.GetGenericArguments());
                    foreach (var parameter in constructed.GetParameters()) stack.Pop();
                    stack.Push(true);
                }
                else if (opcode == OpCodes.Dup) stack.Push(stack.Peek());
                else if (opcode == OpCodes.Stfld)
                {
                    bool valueIsIterator = stack.Pop();
                    bool targetIsIterator = stack.Pop();
                    var field = factory.Module.ResolveField(BitConverter.ToInt32(il, position),
                        factory.DeclaringType.GetGenericArguments(), factory.GetGenericArguments());
                    if (valueIsIterator || !targetIsIterator || field.DeclaringType != constructed?.DeclaringType)
                        throw new NotSupportedException("DoSaveGame writes outside its iterator captures.");
                }
                else if (opcode == OpCodes.Ret)
                {
                    if (stack.Count != 1 || !stack.Pop())
                        throw new NotSupportedException("DoSaveGame does not return its constructed iterator.");
                    returned = true;
                }
                else if (opcode == OpCodes.Ldarg || opcode == OpCodes.Ldarg_S ||
                    opcode == OpCodes.Ldarg_0 || opcode == OpCodes.Ldarg_1 || opcode == OpCodes.Ldarg_2 || opcode == OpCodes.Ldarg_3 ||
                    opcode == OpCodes.Ldc_I4 || opcode == OpCodes.Ldc_I4_S || opcode == OpCodes.Ldc_I4_M1 ||
                    opcode == OpCodes.Ldc_I4_0 || opcode == OpCodes.Ldc_I4_1 || opcode == OpCodes.Ldc_I4_2 ||
                    opcode == OpCodes.Ldc_I4_3 || opcode == OpCodes.Ldc_I4_4 || opcode == OpCodes.Ldc_I4_5 ||
                    opcode == OpCodes.Ldc_I4_6 || opcode == OpCodes.Ldc_I4_7 || opcode == OpCodes.Ldc_I4_8 || opcode == OpCodes.Ldnull)
                    stack.Push(false);
                else if (opcode != OpCodes.Nop)
                    throw new NotSupportedException("DoSaveGame is no longer a direct iterator factory.");
                position += size;
            }
            if (!returned || constructed == null) throw new NotSupportedException("DoSaveGame has no iterator return.");
            yield return constructed;
        }
    }
}

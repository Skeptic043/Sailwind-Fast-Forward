using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using SailwindFastForward;
using UnityEngine;

internal static class CompatibilityChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var installed = SaveCoroutine.FindMoveNext(typeof(SaveLoadManager));
        check(installed.ReturnType == typeof(bool) && installed.DeclaringType.DeclaringType == typeof(SaveLoadManager),
            "production discovery resolves installed game save iterator without a build or hash gate");
        foreach (int revision in new[] { 1, 901 })
        {
            var manager = BuildFixture(revision);
            var method = SaveCoroutine.FindMoveNext(manager);
            check(method.DeclaringType.Name == "<DoSaveGame>d__" + revision,
                "production discovery accepts unrelated assembly version and renumbered iterator " + revision);
        }
        foreach (Type invalid in new[] { typeof(Missing), typeof(WrongReturn), typeof(Ambiguous), typeof(MultipleObjects),
            typeof(Unmarked), typeof(WrongAttribute), typeof(Discarded) })
        {
            string warning = null;
            bool installedHook = SaveContinuation.TryInstall("Save coroutine error hook",
                () => SaveCoroutine.FindMoveNext(invalid), message => warning = message);
            check(!installedHook && warning.Contains("Fast-forward remains available") && warning.Contains("including held"),
                "production optional hook setup degrades with actionable warning for " + invalid.Name);
        }
        check(SaveContinuation.TryInstall("hook", () => { }, _ => throw new Exception("Unexpected warning")),
            "successful optional patch setup reports installation");

        var pending = new SaveContinuation(null);
        pending.SetTimerSupport(true);
        pending.SetCoroutineSupport(true);
        check(!pending.Supported && !pending.TryEnterSave(true),
            "transpiler validation before completed installation cannot permit held save continuation");
        bool installation = SaveContinuation.TryInstall("Timer hook", () =>
        {
            pending.SetTimerSupport(true);
            throw new InvalidOperationException("simulated later Harmony emission failure");
        }, _ => { });
        pending.SetTimerInstalled(installation);
        pending.SetTimerSupport(true);
        check(!pending.Supported && !pending.TryEnterSave(true),
            "later transpiler retries cannot override failed initial Harmony installation");

        foreach (bool timer in new[] { false, true })
        foreach (bool coroutine in new[] { false, true })
        foreach (bool held in new[] { false, true })
        foreach (bool timerSave in new[] { false, true })
        {
            var fixture = new Frames(held);
            fixture.Save.SetTimerSupport(timer);
            fixture.Save.SetTimerInstalled(true);
            fixture.Save.SetCoroutineSupport(coroutine);
            fixture.Activate();
            check(fixture.Speed.Active, $"core {(held ? "hold" : "cycle")} activation works with hooks {timer}/{coroutine}");
            if (timerSave) fixture.Save.Begin(true);
            bool permitted = fixture.Save.TryEnterSave(held, false);
            if (!permitted) fixture.Cancel();
            if (timerSave) fixture.Save.End(true);
            bool expected = timer && coroutine && (timerSave || held);
            check(permitted == expected && fixture.Speed.Active == expected,
                $"save boundary continuation policy: hooks {timer}/{coroutine}, held {held}, timer {timerSave}");
            check(fixture.Save.BlocksBusySave(true, false, held) == !expected,
                "busy-save fallback uses the same continuation requirement");
            if (!expected)
            {
                fixture.Step(false);
                check(!fixture.Speed.Active && fixture.Scale == 1f,
                    "save cancellation keeps cycle off and held input suppressed on the following frame");
            }
        }
        foreach (bool held in new[] { false, true })
        foreach (bool loseTimer in new[] { false, true })
        {
            var fixture = new Frames(held);
            fixture.Save.SetTimerSupport(true);
            fixture.Save.SetTimerInstalled(true);
            fixture.Save.SetCoroutineSupport(true);
            fixture.Activate();
            fixture.Save.Begin(true);
            check(fixture.Save.TryEnterSave(held), "supported timer enters continuation before support loss");
            fixture.Save.End(true);
            if (loseTimer) fixture.Save.SetTimerSupport(false);
            else fixture.Save.SetCoroutineSupport(false);
            check(!fixture.Speed.Active && fixture.Scale == 1f && fixture.Save.BlocksBusySave(true, false, held),
                "losing either hook cancels active speed and revokes busy-save continuation immediately");
            fixture.Save.SetTimerSupport(true);
            fixture.Save.SetCoroutineSupport(true);
            fixture.Step(false);
            check(!fixture.Speed.Active && fixture.Save.BlocksBusySave(true, false, held),
                "fresh support cannot resurrect old save continuation or a cancelled held key");
        }
    }

    private sealed class Frames
    {
        internal readonly SpeedOwnership Speed = new SpeedOwnership();
        private readonly HoldInput input = new HoldInput();
        internal readonly SaveContinuation Save;
        internal float Scale = 1f;
        private readonly bool held;
        internal Frames(bool held) { this.held = held; Save = new SaveContinuation(Cancel); }
        internal void Activate() => Step(true);
        internal void Step(bool down)
        {
            var action = input.Dispatch(Speed, Scale, 8, 2, 4, true, null,
                new KeyboardShortcut(KeyCode.F7), new KeyboardShortcut(KeyCode.F8), new KeyboardShortcut(KeyCode.F9), 4,
                key => down && key == (held ? KeyCode.F9 : KeyCode.F7), key => held && key == KeyCode.F9,
                _ => Cancel(), Cancel);
            if (action == SpeedInputAction.Hold || action == SpeedInputAction.Cycle || action == SpeedInputAction.Limit)
                Scale = Speed.SelectedSpeed;
        }
        internal void Cancel()
        {
            input.Invalidate();
            Save.Reset();
            if (Speed.Release(Scale)) Scale = 1f;
        }
    }

    // Distinct in-memory assembly versions and deliberately renamed compiler state
    // machines exercise production reflection discovery without invoking game code.
    private static Type BuildFixture(int revision)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("GameUpdate" + revision)
            { Version = new Version(revision, 0) }, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("fixture");
        var manager = module.DefineType("SaveManager", TypeAttributes.Public);
        var iterator = manager.DefineNestedType("<DoSaveGame>d__" + revision,
            TypeAttributes.NestedPrivate | TypeAttributes.Sealed, typeof(object), new[] { typeof(IEnumerator) });
        iterator.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));
        var constructor = iterator.DefineDefaultConstructor(MethodAttributes.Public);
        foreach (var contract in typeof(IEnumerator).GetMethods())
        {
            var member = iterator.DefineMethod(contract.Name, MethodAttributes.Public | MethodAttributes.Virtual |
                MethodAttributes.Final, contract.ReturnType, Type.EmptyTypes);
            var generator = member.GetILGenerator();
            if (contract.ReturnType == typeof(bool)) generator.Emit(OpCodes.Ldc_I4_0);
            else if (contract.ReturnType == typeof(object)) generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ret);
            iterator.DefineMethodOverride(member, contract);
        }
        var factory = manager.DefineMethod("DoSaveGame", MethodAttributes.Public, typeof(IEnumerator), Type.EmptyTypes);
        var il = factory.GetILGenerator();
        il.Emit(OpCodes.Newobj, constructor);
        il.Emit(OpCodes.Ret);
        iterator.CreateType();
        return manager.CreateType();
    }

    private sealed class Missing { }
    private sealed class WrongReturn { public object DoSaveGame() => null; }
    private sealed class Ambiguous
    {
        public IEnumerator DoSaveGame() { yield break; }
        public IEnumerator DoSaveGame(bool compressed) { yield break; }
    }
    private sealed class MultipleObjects
    {
        public IEnumerator DoSaveGame(bool choice) => choice ? new Iterator() : new Iterator();
        [CompilerGenerated] private sealed class Iterator : EmptyIterator { }
    }
    private sealed class Unmarked
    {
        public IEnumerator DoSaveGame() => new Iterator();
        private sealed class Iterator : EmptyIterator { }
    }
    private sealed class WrongAttribute
    {
        [IteratorStateMachine(typeof(EmptyIterator))]
        public IEnumerator DoSaveGame() => new Iterator();
        [CompilerGenerated] private sealed class Iterator : EmptyIterator { }
    }
    private sealed class Discarded
    {
        public IEnumerator DoSaveGame() { _ = new Iterator(); return null; }
        [CompilerGenerated] private sealed class Iterator : IEnumerator
        {
            public bool MoveNext() => false;
            public object Current => null;
            public void Reset() { }
        }
    }
    private class EmptyIterator : IEnumerator
    {
        public bool MoveNext() => false;
        public object Current => null;
        public void Reset() { }
    }
}

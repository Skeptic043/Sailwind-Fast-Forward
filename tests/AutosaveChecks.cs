using System;
using SailwindFastForward;

internal static class AutosaveChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var state = new AutosaveState();
        state.Begin(true);
        check(state.TryEnterSave(), "timer wrapper grants its one prefix exemption");
        check(!state.TryEnterSave(), "nested synchronous save cannot reuse prefix exemption");
        state.End(true);
        check(state.BlocksBusySave(true), "outer finally cannot revive permission invalidated by nested save");
        state.Reset(); state.Begin(true); state.Begin(true);
        check(!state.TryEnterSave(), "nested timer wrapper is rejected conservatively");
        state.End(true);
        check(state.BlocksBusySave(true), "nested wrapper finalization cannot establish continuation");
        state.Reset(); state.Begin(true); state.End(true);
        check(state.BlocksBusySave(true), "wrapper without observed prefix cannot establish continuation");
        state.Reset(); state.Begin(true); state.TryEnterSave(); state.Reset(); state.End(true);
        check(state.BlocksBusySave(true), "cancellation during wrapper survives finally");
        state.Reset(); state.Begin(false);
        check(!state.TryEnterSave(), "rejected or unowned save cannot acquire permission");
        state.End(true);
        check(state.BlocksBusySave(true), "rejected save busy state remains blocked");
        state.Reset(); state.Begin(true); state.TryEnterSave(); state.End(true);
        for (int frame = 0; frame < 4; frame++)
            check(!state.BlocksBusySave(true), "continued autosave remains permitted for busy frame " + frame);
        check(state.BlocksBusySave(true, true), "enabling cancel-on-autosave revokes an in-flight continuation");
        check(state.BlocksBusySave(true), "disabling policy again cannot resurrect old continuation");
        check(!state.BlocksBusySave(false), "completion clears save permission without restoring any speed");
        state.End(true);
        check(state.BlocksBusySave(true), "duplicate or stale End cannot reacquire busy permission");

        var error = new InvalidOperationException("synthetic serialization failure");
        int cancellations = 0;
        check(SaveErrorBoundary.Handle(null, () => cancellations++) == null && cancellations == 0,
            "successful coroutine step has no cancellation side effect");
        check(ReferenceEquals(error, SaveErrorBoundary.Handle(error, () => cancellations++)) && cancellations == 1,
            "save exception handler cancels once and preserves exact original exception");
        check(ReferenceEquals(error, SaveErrorBoundary.Handle(error, () => throw new Exception("cleanup failure"))),
            "cleanup failure cannot mask original save exception");
        check(ReferenceEquals(error, SaveErrorBoundary.Handle(error, null)), "absent plugin still propagates original save exception");
    }
}

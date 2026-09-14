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

        state.Reset();
        check(state.TryEnterSave(true, false), "eligible owned hold may enter an idle manual save");
        check(!state.BlocksBusySave(true, false, true), "eligible held manual save permits its busy phase");
        check(!state.BlocksBusySave(true, true, true), "timer cancellation policy does not cancel a held manual save");
        check(state.BlocksBusySave(true, false, false), "manual busy permission ends immediately when held eligibility ends");
        check(state.BlocksBusySave(true, false, true), "renewed eligibility cannot resurrect old manual save permission");
        state.Reset();
        check(!state.TryEnterSave(true, true), "already-busy save cannot gain a manual hold exemption");
        check(state.BlocksBusySave(true, false, true), "rejected busy save leaves no manual permission");
        state.Reset(); state.Begin(false);
        check(!state.TryEnterSave(true, false), "cancelled timer origin cannot masquerade as a held manual save");
        state.End(true);
        check(state.BlocksBusySave(true, false, true), "cancelled timer finally cannot create manual busy permission");
        state.Reset(); state.Begin(true);
        check(state.TryEnterSave(true, false), "continuing timer consumes timer prefix even with held manual eligibility");
        state.End(true);
        check(state.BlocksBusySave(true, true, true), "timer cancellation still revokes timer permission while hold is eligible");
        state.Reset();
        check(state.TryEnterSave(true, false), "new independent manual save may acquire fresh hold permission");
        check(!state.BlocksBusySave(false, false, true), "manual completion clears its permission");
        check(state.BlocksBusySave(true, false, true), "manual completion cannot exempt a later unrelated busy save");

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

using System;

namespace SailwindFastForward
{
    // Scope background execution to the time this mod is accelerating gameplay.
    internal sealed class BackgroundExecution
    {
        private readonly Func<bool> read;
        private readonly Action<bool> write;
        private bool changed;

        internal BackgroundExecution(Func<bool> read, Action<bool> write)
        {
            this.read = read;
            this.write = write;
        }

        internal void Enable()
        {
            if (changed || read()) return;
            write(true);
            changed = true;
        }

        internal void Restore()
        {
            if (!changed) return;
            changed = false;
            if (read()) write(false);
        }
    }
}

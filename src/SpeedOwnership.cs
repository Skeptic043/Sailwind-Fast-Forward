namespace SailwindFastForward
{
    // Only restore a speed we actually set. Never overwrite a pause or native sleep.
    internal sealed class SpeedOwnership
    {
        internal float SelectedSpeed { get; private set; } = 1f;
        internal bool Active => SelectedSpeed > 1f;

        internal bool TryCycle(float current, int maximum)
        {
            if ((maximum != 2 && maximum != 4 && maximum != 8) || current != SelectedSpeed)
                return false;
            SelectedSpeed = SelectedSpeed >= maximum ? 1f : SelectedSpeed * 2f;
            return true;
        }

        internal bool Release(float current)
        {
            bool restore = Active && current == SelectedSpeed;
            SelectedSpeed = 1f;
            return restore;
        }

        internal bool TryLimit(float current, int maximum)
        {
            if ((maximum != 1 && maximum != 2 && maximum != 4 && maximum != 8) ||
                !Active || current != SelectedSpeed || SelectedSpeed <= maximum)
                return false;
            SelectedSpeed = maximum;
            return true;
        }
    }
}

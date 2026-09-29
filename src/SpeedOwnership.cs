namespace SailwindFastForward
{
    // Only restore a speed we actually set. Never overwrite a pause or native sleep.
    internal sealed class SpeedOwnership
    {
        internal float SelectedSpeed { get; private set; } = 1f;
        internal bool Active => SelectedSpeed > 1f;

        internal bool TrySelect(float current, int requested)
        {
            if ((requested != 2 && requested != 4 && requested != 8 && requested != 16 && requested != 32) || current != SelectedSpeed)
                return false;
            SelectedSpeed = requested;
            return true;
        }

        internal bool TryCycle(float current, int maximum)
        {
            if ((maximum != 2 && maximum != 4 && maximum != 8 && maximum != 16 && maximum != 32) || current != SelectedSpeed)
                return false;
            SelectedSpeed = SelectedSpeed >= maximum ? 1f : SelectedSpeed * 2f;
            return true;
        }

        internal bool Release(float current, bool externallyOwned = false)
        {
            bool restore = Active && !externallyOwned && current == SelectedSpeed;
            SelectedSpeed = 1f;
            return restore;
        }

        internal bool TryLimit(float current, int maximum)
        {
            if ((maximum != 1 && maximum != 2 && maximum != 4 && maximum != 8 && maximum != 16 && maximum != 32) ||
                !Active || current != SelectedSpeed || SelectedSpeed <= maximum)
                return false;
            SelectedSpeed = maximum;
            return true;
        }
    }
}

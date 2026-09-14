namespace SailwindFastForward
{
    internal enum HoldBoundary { SleepOrBed, Recovery, Shipyard, Economy, CursorMenu, Pause, Loading, OutsideGameplay, SaveUnavailable }

    internal static class HoldPolicy
    {
        internal static bool CanContinue(HoldInput hold, SpeedOwnership speed, float current,
            bool chordHeld, bool hardBlocked) =>
            !hardBlocked && hold.Active && speed.Active && current == speed.SelectedSpeed && chordHeld;

        internal static bool Blocks(HoldBoundary boundary, bool intentionalHold)
        {
            switch (boundary)
            {
                case HoldBoundary.SleepOrBed:
                case HoldBoundary.Recovery:
                case HoldBoundary.Shipyard:
                case HoldBoundary.Economy:
                case HoldBoundary.CursorMenu:
                    return !intentionalHold;
                default:
                    return true;
            }
        }
    }
}

using System;
using BepInEx.Configuration;

namespace SailwindFastForward
{
    // Retain range metadata for Configuration Manager's slider. Clamp is also
    // applied by the real ConfigEntry setter when loading or editing a file.
    internal sealed class IndicatorScaleRange : AcceptableValueRange<float>
    {
        internal IndicatorScaleRange() : base(0.5f, 5f) { }

        public override object Clamp(object value)
        {
            float scale = (float)value;
            if (float.IsNaN(scale)) return 1f;
            scale = Math.Max(MinValue, Math.Min(MaxValue, scale));
            return (float)(Math.Round(scale * 2, MidpointRounding.AwayFromZero) / 2);
        }

        public override bool IsValid(object value) => value is float scale && scale.Equals(Clamp(scale));
        public override string ToDescriptionString() => base.ToDescriptionString() + " in steps of 0.5";
    }

    internal enum IndicatorBackground { Simple = 0, Scroll = 1, None = 2 }
}

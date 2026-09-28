using System;
using System.IO;
using BepInEx.Configuration;
using SailwindFastForward;

internal static class IndicatorSettingsChecks
{
    internal static void Run(string scratch, Action<bool, string> check)
    {
        string path = Path.Combine(scratch, "indicator.cfg");
        var file = new ConfigFile(path, false);
        var settings = new PluginSettings(file);
        check(settings.IndicatorScale.Value == 1f && settings.IndicatorBackground.Value == IndicatorBackground.Simple,
            "indicator upgrade defaults preserve original size and background");
        var range = settings.IndicatorScale.Description.AcceptableValues;
        // Configuration Manager 19 discovers these inherited public properties.
        check((float)range.GetType().GetProperty("MinValue").GetValue(range) == .5f &&
            (float)range.GetType().GetProperty("MaxValue").GetValue(range) == 5f,
            "indicator scale exposes slider bounds to Configuration Manager");
        foreach (var pair in new[] {
            ("0.1", .5f), ("0.5", .5f), ("1.24", 1f), ("1.25", 1.5f),
            ("4.74", 4.5f), ("4.75", 5f), ("5", 5f), ("100", 5f), ("NaN", 1f),
            ("Infinity", 5f), ("-Infinity", .5f) })
        {
            File.WriteAllText(path, "[Display]\nIndicatorScale = " + pair.Item1 + "\nIndicatorBackground = Scroll\n");
            file.Reload();
            check(settings.IndicatorScale.Value == pair.Item2, "actual loader normalizes indicator scale: " + pair.Item1);
            file.Save();
            var reloaded = new PluginSettings(new ConfigFile(path, false));
            check(reloaded.IndicatorScale.Value == pair.Item2 && reloaded.IndicatorBackground.Value == IndicatorBackground.Scroll,
                "normalized indicator scale and background survive save/rebind: " + pair.Item1);
        }
        settings.IndicatorScale.Value = 2.27f;
        check(settings.IndicatorScale.Value == 2.5f, "live slider assignment snaps before the change notification");
        foreach (IndicatorBackground background in Enum.GetValues(typeof(IndicatorBackground)))
        {
            settings.IndicatorBackground.Value = background;
            file.Save();
            file.Reload();
            check(settings.IndicatorBackground.Value == background, "background enum survives actual loader round trip: " + background);
        }
        File.WriteAllText(path, "[Display]\nIndicatorScale = 3.5\n");
        var fresh = new PluginSettings(new ConfigFile(path, false));
        check(fresh.IndicatorScale.Value == 3.5f, "initial bind applies indicator scale from an existing file");
        foreach (bool saveOnSet in new[] { false, true })
        {
            File.WriteAllText(path, "[Display]\nIndicatorBackground = Current\nIndicatorScale = 2.5\n[Other]\nPreserved = untouched\n");
            var migrationFile = new ConfigFile(path, false) { SaveOnConfigSet = saveOnSet };
            var migrated = new PluginSettings(migrationFile);
            check(migrated.IndicatorBackground.Value == IndicatorBackground.Simple && migrated.IndicatorScale.Value == 2.5f,
                "trial Current background upgrades to Simple and preserves scale");
            check(migrationFile.SaveOnConfigSet == saveOnSet, "indicator migration restores automatic-save preference");
            if (!saveOnSet) check(File.ReadAllText(path).Contains("IndicatorBackground = Current"),
                "indicator migration does not save when automatic saves are disabled");
            migrationFile.Save();
            string saved = File.ReadAllText(path);
            check(saved.Contains("IndicatorBackground = Simple") && saved.Contains("Preserved = untouched"),
                "indicator migration saves canonical name and preserves unrelated orphaned settings");
        }
        check(Enum.GetNames(typeof(IndicatorBackground)).Length == 3 && (int)IndicatorBackground.Simple == 0 &&
            (int)IndicatorBackground.Scroll == 1 && (int)IndicatorBackground.None == 2,
            "indicator dropdown contains only Simple Scroll None with stable numeric values");
    }
}

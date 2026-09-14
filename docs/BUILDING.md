# Building from source

The local repository is `E:\Projects\Unity\Sailwind\FastForward`. Run development commands there. The parent Sailwind folder holds independent sibling mods and shared game research. It is not this mod's Git repository.

Requires Windows PowerShell, the .NET 10 SDK (for the check executable), an installed Mono version of Sailwind and BepInEx 5. The plugin itself targets .NET Standard 2.0.

Pass your game installation directory and the BepInEx `core` directory containing `BepInEx.dll`, `0Harmony.dll`, `Mono.Cecil.dll` and `MonoMod.Utils.dll` (used by the game IL and real configuration checks):

```powershell
./Build.ps1 -GameDir 'D:\Games\Sailwind' -BepInExCore 'D:\ModProfile\BepInEx\core'
```

This builds the plugin, runs the behavior checks and copies the DLL to `artifacts/build/<version>/`. Local dependencies, build outputs and game assemblies are excluded from Git. Game and loader DLLs are references only. They are not distributed.

Configuration checks use the actual BepInEx library with fresh and customized config fixtures under `.local/config-checks/<run>/`. They verify default grouping, all three shortcuts, lowercase and spaced names, modifier order, invalid text retention and save/reload preservation. Shortcuts bind as text and parse separately within this plugin. No global loader converter is replaced. Behavior checks exercise held activity overrides, input-session cancellation and save continuation/error handling. These checks run under .NET 10, not the live Unity/Mono game loop.

There is no game-version or assembly-hash allowlist. Required pause, sleep and save/load boundaries still need compatible game members. The optional timer-save hook checks the semantic IL layout before changing it. Save-coroutine discovery tolerates compiler renumbering. If either optional hook is unavailable, FF remains usable and cancels on all saves, including hold mode. Unexpected timer IL is left unchanged. Inspect a warning when restoring save continuation support, rather than adding a new accepted build hash. Other mods' runtime patches still require separate compatibility testing.

To build and verify the distributable ZIP and source ZIP:

```powershell
./Package.ps1 -GameDir 'D:\Games\Sailwind' -BepInExCore 'D:\ModProfile\BepInEx\core'
```

Packages and SHA-256 checksums appear in `artifacts/release/`. Packaging uses an explicit file list and verifies every ZIP entry against its source file. It checks metadata, versions, required files and the 256x256 PNG icon. The package does not contain user configuration, logs or game/loader assemblies.

`assets/icon.svg` is the editable icon source. `tools/Render-Icon.ps1` renders its two-chevron geometry as `icon.png` using Windows System.Drawing. If you change the SVG geometry, update the renderer to match.

Before a version change, update the project version, `BepInPlugin` version, manifest and changelog. Keep the plugin ID `local.sailwind.fastforward` stable so existing configuration carries over.

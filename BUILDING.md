# Building Sailwind Fast Forward

## Requirements

- Windows with PowerShell and the .NET 10 SDK (the checks target .NET 10; the plugin targets .NET Standard 2.0).
- A local Sailwind installation with BepInEx 5. The BepInEx `core` folder must contain `BepInEx.dll`, `0Harmony.dll`, `Mono.Cecil.dll` and `MonoMod.Utils.dll`.

Game and loader assemblies are build references only. They are not included in this repository or its packages.

## Build and test

From the repository folder:

```powershell
./Build.ps1 -GameDir 'C:\Games\Sailwind' -BepInExCore 'C:\Games\Sailwind\BepInEx\core'
```

Use the paths for your own installation or mod profile. This builds the plugin, runs the checks and copies the DLL to `artifacts/build/<version>/`. Any failed check stops the build.

The checks run under .NET 10, not the live game loop. Configuration checks use the real BepInEx library with fresh and customized config files under `.local/config-checks/<run>/`, covering default grouping, all three shortcuts, lowercase and spaced key names, modifier order, keeping invalid text, and save/reload. Behaviour checks cover held activity overrides, input cancellation, and save continuation and error handling. Game checks read the installed `Assembly-CSharp.dll`: the optional timer-save hook checks its IL layout before patching, and save-coroutine discovery tolerates compiler renumbering. If either optional hook doesn't match, Fast Forward stays usable and cancels on all saves.

## Package

```powershell
./Package.ps1 -GameDir 'C:\Games\Sailwind' -BepInExCore 'C:\Games\Sailwind\BepInEx\core'
```

This builds the mod and writes `artifacts/release/SailwindFastForward-<version>.zip` with `BepInEx/plugins/SailwindFastForward/`, `manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md` and `LICENSE`. Every ZIP entry is checked against its source file.

`assets/icon.svg` is the editable icon source. `tools/Render-Icon.ps1` renders its two-chevron geometry as `icon.png`. If you change the SVG geometry, update the renderer to match.

Before a version change, update the project version, `BepInPlugin` version, manifest and changelog. Keep the plugin ID `local.sailwind.fastforward` stable so existing configuration carries over.

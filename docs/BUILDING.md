# Building from source

Requires Windows PowerShell, the .NET 10 SDK (for the check executable), an installed Mono version of Sailwind and BepInEx 5. The plugin itself targets .NET Standard 2.0.

Pass your game installation directory and the BepInEx `core` directory containing `BepInEx.dll`, `0Harmony.dll` and `Mono.Cecil.dll` (used by the game IL checks):

```powershell
./Build.ps1 -GameDir 'D:\Games\Sailwind' -BepInExCore 'D:\ModProfile\BepInEx\core'
```

This builds the plugin, runs the behavior checks and copies the DLL to `artifacts/build/<version>/`. Local dependencies, build outputs and game assemblies are excluded from Git. Game and loader DLLs are references only; they are not distributed.

To build and verify the distributable ZIP and source ZIP:

```powershell
./Package.ps1 -GameDir 'D:\Games\Sailwind' -BepInExCore 'D:\ModProfile\BepInEx\core'
```

Packages and SHA-256 checksums appear in `artifacts/release/`. Packaging uses an explicit file list and verifies every ZIP entry against its source file. It checks metadata, versions, required files and the 256x256 PNG icon. The package does not contain user configuration, logs or game/loader assemblies.

`assets/icon.svg` is the editable icon source. `tools/Render-Icon.ps1` renders its two-chevron geometry as `icon.png` using Windows System.Drawing. If you change the SVG geometry, update the renderer to match.

Before a version change, update the project version, `BepInPlugin` version, manifest and changelog. Keep the plugin ID `local.sailwind.fastforward` stable so existing configuration carries over.

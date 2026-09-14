# Building from source

The local repository is `E:\Projects\Unity\Sailwind\FastForward`. Run development commands there. The parent Sailwind folder holds independent sibling mods and shared game research. It is not this mod's Git repository.

Requires Windows PowerShell, the .NET 10 SDK (for the check executable), an installed Mono version of Sailwind and BepInEx 5. The plugin itself targets .NET Standard 2.0.

Pass your game installation directory and the BepInEx `core` directory containing `BepInEx.dll`, `0Harmony.dll`, `Mono.Cecil.dll` and `MonoMod.Utils.dll` (used by the game IL and real configuration checks):

```powershell
./Build.ps1 -GameDir 'D:\Games\Sailwind' -BepInExCore 'D:\ModProfile\BepInEx\core'
```

This builds the plugin, runs the behavior checks and copies the DLL to `artifacts/build/<version>/`. Local dependencies, build outputs and game assemblies are excluded from Git. Game and loader DLLs are references only. They are not distributed.

Configuration checks use the actual BepInEx library with fresh and customized config fixtures under `.local/config-checks/<run>/`. They verify default grouping, reset/hold rebinding and preservation of existing values. Behavior checks exercise input-session cancellation and autosave continuation/error handling. These checks run under .NET 10, not the live Unity/Mono game loop.

The candidate deliberately rejects an uninspected game binary and changed timer-save IL context. The verified Assembly-CSharp.dll SHA256 is `978A21A680F42C89EBCB3530F9A99EF074960BE6377DAF8E85893134A5E5CE23` (Steam build 24324368). A failed compatibility check requires inspecting the new save path and updating tests/evidence. Simply changing the accepted hash is not sufficient. Other mods' runtime patches still require separate compatibility testing.

To build and verify the distributable ZIP and source ZIP:

```powershell
./Package.ps1 -GameDir 'D:\Games\Sailwind' -BepInExCore 'D:\ModProfile\BepInEx\core'
```

Packages and SHA-256 checksums appear in `artifacts/release/`. Packaging uses an explicit file list and verifies every ZIP entry against its source file. It checks metadata, versions, required files and the 256x256 PNG icon. The package does not contain user configuration, logs or game/loader assemblies.

`assets/icon.svg` is the editable icon source. `tools/Render-Icon.ps1` renders its two-chevron geometry as `icon.png` using Windows System.Drawing. If you change the SVG geometry, update the renderer to match.

Before a version change, update the project version, `BepInPlugin` version, manifest and changelog. Keep the plugin ID `local.sailwind.fastforward` stable so existing configuration carries over.

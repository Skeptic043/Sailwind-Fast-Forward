# Sailwind Fast Forward

Spend less time waiting on long voyages. Fast-forward at **2x, 4x or 8x** while your ship, weather and survival needs keep running.

## Install

### Mod managers

Install [Sailwind Fast Forward](https://thunderstore.io/c/sailwind/p/Skeptic043/Sailwind_Fast_Forward/) through **r2modman** or **Thunderstore Mod Manager**, then launch the game modded through your manager. Dependencies are installed automatically.

### Manual installation

1. Install [BepInExPack](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) in your Sailwind game folder, following its installation instructions.
2. Download and extract the mod ZIP. Copy its `plugins/SailwindFastForward` folder into `BepInEx/plugins` in your Sailwind folder.
3. Launch Sailwind normally.

## Controls

Press **F7** to cycle **1x → 2x → 4x → 1x**. Press **F8** to turn fast-forward off and return directly to **1x** from any FF speed. A small indicator shows the active speed.

Hold **F9** for fast-forward at `HoldSpeed` (default **4x**), subject to your configured maximum and movement/inventory limits. Releasing it returns to **1x**, even if you were using cycle mode before holding it. It does not cycle speeds or return to an earlier toggled speed.

All three shortcuts are configurable and support modifiers. Reset takes priority over hold, and hold takes priority over cycling. Hotkeys work while holding sailing controls and are ignored while the game is unfocused. Reset does not override the game's pause, sleep speed or a speed changed externally. After a reset or cancellation while holding F9, release it and press again to start a new hold.

If your existing cycle shortcut already uses F8 (including with modifiers), the new reset shortcut starts unassigned to preserve it. Choose another reset key in the config.

Likewise, if your existing cycle or reset shortcut uses F9, the new hold shortcut starts disabled. An explicitly saved binding is always retained.

## Configuration

After the first launch, close the game and edit `BepInEx/config/local.sailwind.fastforward.cfg`. This is inside your mod manager profile, or your Sailwind folder for a manual installation.

Settings are grouped into **Controls**, **Display** and **Simulation**. Existing section names and setting names are retained so your custom values carry over.

| Section | Setting | Default | Options |
| --- | --- | --- | --- |
| Controls | `Hotkey` | `F7` | Cycle speed. Supports modifiers, such as `F7 + LeftControl`. Leave blank to disable. |
| Controls | `ResetHotkey` | `F8`* | Return directly to 1x. Supports the same modifier format. Leave blank to disable. |
| Controls | `HoldHotkey` | `F9`* | Fast-forward only while held. Supports modifiers. Leave blank to disable. |
| Display | `ShowIndicator` | `true` | Show or hide the speed indicator. |
| Simulation | `MaxSpeed` | `4` | Highest fast-forward speed: `2`, `4` or `8`. |
| Simulation | `HoldSpeed` | `4` | Requested hold speed: `2`, `4` or `8`, bounded by `MaxSpeed` and activity limits. |
| Simulation | `MovementInventoryMaxSpeed` | `2` | Speed limit while moving or viewing inventory/stats: `1`, `2`, `4` or `8`. |
| Simulation | `CancelOnAutosave` | `false` | Turn off fast-forward when an autosave starts. |

*Reset starts unassigned when the existing cycle shortcut uses F8. Hold starts unassigned when cycle or reset uses F9. Explicitly saved bindings are retained.

To disable a shortcut, remove everything after `=` on its config line. BepInEx may write an unassigned shortcut back as `None` when it saves the file.

All three shortcuts use the same modifier format. For example, `F7 + LeftShift` requires holding left Shift while pressing F7. Key names are case-sensitive. Use names such as `LeftShift`, `RightShift`, `LeftControl` or `LeftAlt`, rather than `shift` or `ctrl`. For hold-to-FF, keep both the main key and its modifiers held.

Set `MaxSpeed` to `8` for **1x → 2x → 4x → 8x → 1x**.

### Movement and inventory

By default, walking or opening inventory at 4x or 8x reduces speed to **2x**. Set `MovementInventoryMaxSpeed` to `1` to turn fast-forward off during those activities, or `8` to allow all speeds up to `MaxSpeed`. Stopping movement or closing inventory keeps the reduced speed. Press F7 to increase it again. While moving or viewing inventory, F7 cycles only through the allowed speeds. Looking around, steering, adjusting sails and being carried along by your boat do not trigger the movement limit unless you also press a movement control.

## During play

- Cycle mode continues while alt-tabbed. Hold mode cancels on focus loss. Normal island loading does not cancel either mode.
- Opening the pause menu cancels fast-forward. Inventory/stats uses your configured limit instead.
- Autosaves keep your current speed unless `CancelOnAutosave` is enabled.
- Loading a save and entering bed cancel fast-forward. Turn it on again when you're ready.
- Pause and sleep work as usual. Food, water and rest continue to drain during accelerated sailing.

Higher speeds require more performance from your system. Lower `MaxSpeed` if the game struggles.

Version 1.1.0 checks compatibility against the inspected Sailwind Steam build **24324368**. If a game update changes the inspected binary, FF disables itself and logs a compatibility error until the save path is reviewed again.

## Issues and links

[Report an issue](https://github.com/Skeptic043/Sailwind-Fast-Forward/issues) with your settings and `BepInEx/LogOutput.log`.

[Source code](https://github.com/Skeptic043/Sailwind-Fast-Forward) · [MIT License](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/LICENSE) · [Support on Ko-fi](https://ko-fi.com/skeptic043) · skeptic043

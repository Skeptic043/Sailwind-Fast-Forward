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

Press **F7** to cycle **1x → 2x → 4x → 1x**. A small indicator shows the active speed.

## Configuration

After the first launch, close the game and edit `BepInEx/config/local.sailwind.fastforward.cfg`. This is inside your mod manager profile, or your Sailwind folder for a manual installation.

| Setting | Default | Options |
| --- | --- | --- |
| `Hotkey` | `F7` | Key used to cycle speed. |
| `MaxSpeed` | `4` | Highest speed available: `2`, `4` or `8`. |
| `MovementInventoryMaxSpeed` | `2` | Speed limit while moving or viewing inventory/stats: `1`, `2`, `4` or `8`. |
| `CancelOnAutosave` | `false` | Set to `true` to turn off fast-forward when an autosave starts. |
| `ShowIndicator` | `true` | Show or hide the speed indicator. |

Set `MaxSpeed` to `8` for **1x → 2x → 4x → 8x → 1x**.

### Movement and inventory

By default, walking or opening inventory at 4x or 8x reduces speed to **2x**. Set `MovementInventoryMaxSpeed` to `1` to turn fast-forward off during those activities, or `8` to allow all speeds up to `MaxSpeed`. Stopping movement or closing inventory keeps the reduced speed. Press F7 to increase it again. While moving or viewing inventory, F7 cycles only through the allowed speeds. Looking around, steering, adjusting sails and being carried along by your boat do not trigger the movement limit unless you also press a movement control.

## During play

- Fast-forward continues while alt-tabbed and during normal island loading.
- Opening the pause menu cancels fast-forward. Inventory/stats uses your configured limit instead.
- Autosaves keep your current speed unless `CancelOnAutosave` is enabled.
- Loading a save and entering bed cancel fast-forward. Turn it on again when you're ready.
- Pause and sleep work as usual. Food, water and rest continue to drain during accelerated sailing.

Higher speeds require more performance from your system. Lower `MaxSpeed` if the game struggles.

## Issues and links

[Report an issue](https://github.com/Skeptic043/Sailwind-Fast-Forward/issues) with your settings and `BepInEx/LogOutput.log`.

[Source code](https://github.com/Skeptic043/Sailwind-Fast-Forward) · [MIT License](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/LICENSE) · [Support on Ko-fi](https://ko-fi.com/skeptic043) · skeptic043

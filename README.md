# Sailwind Fast Forward

Spend less time waiting on long voyages. Sail at **2x**, **4x**, or **8x** speed.

## Install

### Mod managers

Install [Sailwind Fast Forward](https://thunderstore.io/c/sailwind/p/Skeptic043/Sailwind_Fast_Forward/) through **r2modman** or **Thunderstore Mod Manager**, then launch the game modded through your manager. Dependencies are installed automatically.

### Manual installation

1. Install [BepInExPack](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) in your Sailwind game folder, following its installation instructions.
2. Download and extract the mod ZIP. Copy its `plugins/SailwindFastForward` folder into `BepInEx/plugins` in your Sailwind folder.
3. Launch Sailwind normally.

## Controls

Default controls:

- Press **F7** to cycle through speed levels, **1x → 2x → 4x → 1x**. A small indicator at the top right of the screen shows the active speed.
- Press **F8** to turn fast-forward off and return directly to **1x** from any FF speed.
- Hold **F9** for fast-forward at your configured `HoldSpeed` (default **4x**, adjustable in the config file). Walking, inventory and ordinary menus do not reduce the held speed, while releasing the button instantly returns you to **1x**.

All three shortcuts are configurable and support modifier keys. Speed reset takes priority over hold, and hold takes priority over cycling. Hotkeys work while holding sailing controls. Speed reset does not override the game's pause, sleep speed or a speed changed by other means.

## Configuration

After the first launch, close the game and edit `BepInEx/config/local.sailwind.fastforward.cfg`. This is inside your mod manager profile, or your Sailwind folder for a manual installation.

Settings are grouped into **Controls**, **Display** and **Simulation**. Existing section names and setting names are retained so your custom values carry over.

| Section | Setting | Default | Options |
| --- | --- | --- | --- |
| Controls | `Hotkey` | `F7` | Cycle speed. Supports modifiers, such as `F7 + LeftControl`. Leave blank to disable. |
| Controls | `ResetHotkey` | `F8` | Return directly to 1x. Supports the same modifier format. Leave blank to disable. |
| Controls | `HoldHotkey` | `F9` | Fast-forward only while held. Supports modifiers. Leave blank to disable. |
| Display | `ShowIndicator` | `true` | Show or hide the speed indicator. |
| Simulation | `MaxSpeed` | `4` | Highest speed in cycle mode: `2`, `4` or `8`. |
| Simulation | `HoldSpeed` | `4` | Speed while holding the shortcut: `2`, `4` or `8`. Independent of `MaxSpeed`. |
| Simulation | `MovementInventoryMaxSpeed` | `2` | Cycle-mode speed limit while moving or viewing inventory/stats: `1`, `2`, `4` or `8`. Does not limit hold mode. |
| Simulation | `CancelOnAutosave` | `false` | Turn off fast-forward when an autosave starts. |


To disable a shortcut, remove everything after `=` on its config line.

All three shortcuts use the same format: Key names ignore capitalization, so `o` and `O` both work for the `o` key. `LeftShift + Mouse4`, `left shift + mouse 4`, `Mouse4 + LeftShift` are equivalent. For hold-to-FF, keep the main key and all configured modifiers held. Shortcuts also work while holding sailing controls.

Use explicit modifier names such as `LeftShift`, `RightShift`, `LeftControl` or `LeftAlt`. Unsupported values stay in the file and produce a warning in the log. Only that shortcut is disabled until corrected. Close the game before editing the file so its running configuration cannot overwrite your edits.

Set `MaxSpeed` to `8` for **1x → 2x → 4x → 8x → 1x**.

### Movement and inventory

In cycle mode, walking or opening inventory at 4x or 8x reduces speed to **2x** by default. Set `MovementInventoryMaxSpeed` to `1` to turn cycle-mode fast-forward off during those activities, or `8` to allow all speeds up to `MaxSpeed`. Stopping movement or closing inventory keeps the reduced speed. While moving or viewing inventory, F7 cycles only through the allowed speeds. Hold mode ignores this activity limit. Looking around, steering, adjusting sails and being carried along by your boat do not trigger the movement limit unless you also press a movement control.

## During play

- Cycle mode continues while alt-tabbed. Hold mode ends when the game reports its shortcut released, including when focus loss causes that release. Normal island loading does not cancel either mode.
- Opening the pause menu cancels fast-forward. Inventory/stats limits cycle mode only.
- Autosaves keep your current speed unless `CancelOnAutosave` is enabled.
- Loading a save cancels both modes, and entering a bed cancels cycle mode. An active hold can continue through manual saves, bed entry, recovery and ordinary menus while FF still owns the speed.
- If the game or another mod changes the speed, FF releases control. It does not override native sleep speed or automatically restart after pause, loading or another cancellation. Reset, plugin errors and changes to hold settings also end the hold.
- Pause and sleep work as usual. Food, water and rest continue to drain during accelerated sailing.

Higher speeds require more performance from your system. Lower `MaxSpeed` or `HoldSpeed` if the game struggles in that mode.

## AI Use

AI was used to write all of the code in this project. The original concept, design direction, testing, debugging, and release decisions are my own. If you prefer not to use mods developed with AI assistance, I understand and respect that choice.

## Issues and links

[Report an issue](https://github.com/Skeptic043/Sailwind-Fast-Forward/issues) with your settings and `BepInEx/LogOutput.log`.

[Source code](https://github.com/Skeptic043/Sailwind-Fast-Forward) · [MIT License](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/LICENSE) · [Support on Ko-fi](https://ko-fi.com/skeptic043) · skeptic043

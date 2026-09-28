# Sailwind Fast Forward

Spend less time waiting on long voyages. Sail at 2x, 4x, or 8x speed, with food, water and rest progressing alongside game time.

## Installation

### Mod managers

Install [Sailwind Fast Forward](https://thunderstore.io/c/sailwind/p/Skeptic043/Sailwind_Fast_Forward/) through r2modman or Thunderstore Mod Manager, then launch the game modded through your manager. Dependencies are installed automatically.

### Manual installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) in your Sailwind game folder, following its installation instructions.
2. Download and extract the mod ZIP. Copy its `plugins/SailwindFastForward` folder into `BepInEx/plugins` in your Sailwind folder.
3. Launch Sailwind normally.

## Controls

| Default shortcut | Action |
| --- | --- |
| `F7` | Cycle through 1x → 2x → 4x → 1x. Set `MaxSpeed` to `8` to include 8x. |
| `F8` | Turn fast-forward off and return directly to 1x. |
| Hold `F9` | Fast-forward at `HoldSpeed`, 4x by default. Release to return to 1x. |

All three shortcuts are configurable and work while holding sailing controls. Reset takes priority over hold, and hold takes priority over cycling. The indicator at the top right shows your active fast-forward speed. You can adjust its size, change or remove its background, or hide the indicator entirely.

## Configuration

Settings can be changed in-game with [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager). For manual editing, `BepInEx/config/local.sailwind.fastforward.cfg` is generated after the first launch inside your mod manager profile or Sailwind folder.

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| Controls | `Hotkey` | `F7` | Cycle shortcut. |
| Controls | `ResetHotkey` | `F8` | Reset shortcut. |
| Controls | `HoldHotkey` | `F9` | Hold shortcut. |
| Display | `ShowIndicator` | `true` | Show or hide the speed indicator. |
| Display | `IndicatorScale` | `1` | Indicator size from `0.5` to `5` in `0.5` steps. `1` keeps the original size. |
| Display | `IndicatorBackground` | `Simple` | Plain box, Sailwind style scroll, or text only. |
| Simulation | `MaxSpeed` | `4` | Highest speed in cycle mode. |
| Simulation | `HoldSpeed` | `4` | Speed while holding the shortcut. Independent of `MaxSpeed`. |
| Simulation | `MovementInventoryMaxSpeed` | `2` | Cycle-mode speed limit while moving or viewing inventory/stats. Does not limit hold mode. |
| Simulation | `CancelOnAutosave` | `false` | Cancel both cycle and hold fast-forward when an autosave starts. |

### Shortcuts

Key names ignore capitalization and accept spaces. `LeftShift + Mouse4`, `left shift + mouse 4` and `Mouse4 + LeftShift` are equivalent. Use explicit modifier names such as `LeftShift`, `RightShift`, `LeftControl` or `LeftAlt`. Hold mode requires the main key and all configured modifiers to remain held.

## Compatibility

Tested on Sailwind patch 0.39 with BepInEx 5.4.23. No known incompatibilities.

## Known issues

- You can clip through some docks when landing on them while fast-forward is active. This has commonly been observed at Fort Aestrin and Dragon Cliffs. Turn fast-forward off before landing to avoid it. If you do clip through, you can usually jump back out.
- Higher speeds require more performance from your system. Use a lower `MaxSpeed` or `HoldSpeed` if the game struggles.

## AI Use

AI was used to write all of the code in this project. The original design direction, testing, debugging, and release decisions are my own. If you prefer not to use mods developed with AI assistance, I understand and respect that choice.

## Links and support

[Report an issue](https://github.com/Skeptic043/Sailwind-Fast-Forward/issues) with your settings and `BepInEx/LogOutput.log`.

[Source code](https://github.com/Skeptic043/Sailwind-Fast-Forward) · [Changelog](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/CHANGELOG.md) · [Build instructions](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/docs/BUILDING.md) · [MIT License](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/LICENSE) · [Support on Ko-fi](https://ko-fi.com/skeptic043) · skeptic043

# Sailwind Fast Forward

Fast-forward long voyages at **2x, 4x or 8x**. Your ship, weather and survival needs keep running, with a configurable speed limit when moving around your boat or checking inventory.

Press **F7** to cycle **1x → 2x → 4x → 1x** with the default settings. A small label shows the active speed.

## Installation

**BepInEx 5 is required.** If your r2modman profile already has BepInExPack, use that installation.

With Sailwind closed, extract the release ZIP and copy its `plugins/SailwindFastForward` folder into your profile's `BepInEx/plugins` folder. In r2modman, **Settings → Browse profile folder** opens the right location. Launch using **Start modded**.

To update an earlier test build, replace its `SailwindFastForward.dll`; keep only one copy of the plugin. Existing configuration carries over. To uninstall, remove the plugin folder with the game closed. The mod adds no save fields; gameplay progress made during fast-forward saves normally.

## Configuration

After the first launch, edit `BepInEx/config/local.sailwind.fastforward.cfg` with the game closed, then restart.

| Setting | Default | Effect |
| --- | --- | --- |
| `Hotkey` | `F7` | Cycle game speed. |
| `MaxSpeed` | `4` | Highest speed in the cycle: `2`, `4` or `8`. |
| `MovementInventoryMaxSpeed` | `2` | Speed limit while moving or viewing player inventory/stats: `1`, `2`, `4` or `8`. |
| `ShowIndicator` | `true` | Show the active fast-forward speed. |

Set `MaxSpeed` to `8` for **1x → 2x → 4x → 8x → 1x**.

The movement/inventory limit applies to the whole simulation. At the default `2`, walking or opening inventory at 8x drops speed to 2x. Set it to `1` to cancel fast-forward during those activities, or `8` to allow every supported speed. It never exceeds `MaxSpeed` or raises your current speed. Stopping movement or closing inventory leaves the reduced speed in place until you cycle again.

While moving or viewing inventory, F7 cycles only through the allowed speeds. With a 2x limit, that means **1x ↔ 2x**. Movement uses your walk/strafe/jump bindings and enabled gamepad movement input. Being carried along by the boat does not count; looking around, steering and adjusting sails alone do not count unless their controls are also bound to movement.

## Game behavior

- Fast-forward stays active when alt-tabbing and during normal island streaming. The game is allowed to run in the background while fast-forward is active.
- Settings and other menus cancel fast-forward. Player inventory/stats uses the configured limit instead.
- Saving (including autosave), loading, entering bed, sleep/recovery and full world transitions cancel fast-forward. Reactivate it afterward as needed.
- Native pause and sleep retain the game's own timing. The mod changes simulation speed without changing the physics timestep, survival rates or day-length settings.

Higher speeds ask the game to do more work per real second. Choose a maximum that runs well on your system. Ordinary 2x sailing and brief 4x/8x sailing were play-tested, including movement, sails, steering, cargo and inventory handling; heavy storms and demanding high-speed collisions were not exhaustively tested. Survival drain during acceleration was confirmed in logs. Desktop single-player is the tested target; VR and combinations with other simulation-speed mods are untested.

## Source and support

Visit the [source repository](https://github.com/Skeptic043/Sailwind-Fast-Forward), [build instructions](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/docs/BUILDING.md) and [validation notes](https://github.com/Skeptic043/Sailwind-Fast-Forward/blob/main/docs/RELEASE_REVIEW.md). When [reporting a problem](https://github.com/Skeptic043/Sailwind-Fast-Forward/issues), include the game/mod versions, what you were doing, your configuration and the relevant `BepInEx/LogOutput.log` excerpt.

Copyright 2026 skeptic043. Released under the [MIT License](LICENSE).

# Changelog

## 1.1.2

- Cycle and hold speeds are now independent. Keep a 1x/2x/4x cycle and use 8x while holding, or choose any other supported combination.
- Removed the game-build lockout. FF no longer disables itself just because a Sailwind update or beta changes the game assembly.
- Save-coroutine detection now tolerates compiler-generated renumbering.
- If the optional save hooks no longer match, FF stays available and logs a warning. All saves then cancel FF, including hold mode, instead of preventing the mod from starting.

## 1.1.1

- Holding FF now keeps the configured hold speed through movement, inventory, ordinary menus and manual saves. The movement/inventory limit still applies to cycle mode.
- Hold mode follows the shortcut state reported by the game. Pause, loading, reset and loss of speed ownership still cancel it. Autosaves retain the existing configurable behavior.
- Fixed lowercase and spaced shortcut names, including `o` and `left shift + mouse 4`. Modifiers can appear before or after the main key.
- Invalid shortcut text is retained with a clear log warning instead of being overwritten with an empty binding. Existing valid settings carry over. Re-enter any value already erased by an older version.

## 1.1.0

- Added **F8** to return directly to 1x from any fast-forward speed.
- Added **hold F9** for fast-forward, with a configurable hold speed of 2x, 4x or 8x. The default is 4x. Existing speed limits still apply, and releasing the key returns to 1x.
- All three shortcuts support modifiers. Configuration help now uses consistent wording and examples. Leave a shortcut value blank to disable it.
- Existing custom settings are preserved. A new shortcut starts unassigned if its default key is already used by an existing shortcut.
- Reset takes priority over hold and cycling. After cancellation, hold requires release and a fresh press before accelerating again.
- Autosaves retain the existing configurable behavior. Save-coroutine errors now cancel fast-forward without changing the game's save state or hiding the error.
- Added compatibility checks for the inspected Sailwind save path. An uninspected game update disables FF pending compatibility review.

## 1.0.1

- Added `CancelOnAutosave`, off by default. Fast-forward now continues through timer-triggered autosaves unless this setting is enabled.
- Updated the README with r2modman, Thunderstore Mod Manager and manual installation instructions, plus clearer controls and configuration guidance.

## 1.0.0

Initial release.

- Configurable hotkey cycles normal speed and 2x/4x/8x simulation.
- Small indicator shows the active speed.
- Configurable speed limit when moving or opening player inventory/stats.
- Fast-forward continues during alt-tab and routine island streaming.
- Settings, save/load and sleep handling return control safely to the game.
- No changes to save data, survival rates, day-length settings or physics timestep.

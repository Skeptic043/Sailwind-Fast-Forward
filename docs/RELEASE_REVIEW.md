# 1.0.0 validation notes

The release retains the gameplay implementation accepted in test build 0.3.0. Release preparation changes version metadata, documentation, licensing, the icon and packaging.

## Automated checks

The check executable covers speed cycling and ownership, activity limits, modeled menu ordering, hotkeys with unrelated held keys/modifiers, and background-setting ownership. These checks use the production helpers, but do not execute the Unity game loop or Harmony patches.

Packaging verifies an explicit allowlist, file hashes, metadata, version consistency and icon dimensions. A passing package check is not an actual r2modman import or an in-game run of the final versioned DLL.

The 1.0.0 release build passed with zero warnings/errors and all 155 checks passing. The release ZIP and source ZIP passed metadata, icon and per-entry hash validation.

## Player and log evidence

- Everyday 2x sailing: steering, sail adjustment, deck movement, moving cargo and drinking worked by player report.
- Brief 4x and 8x use worked. Earlier unwanted cancellations prompted the background-execution and island-streaming fixes.
- Opening settings canceled acceleration and resumed at 1x. Entering bed canceled acceleration and allowed native sleep.
- Routine island loading no longer canceled acceleration after the fix.
- Movement correctly reduced speed to 2x. Cycling while moving alternated between 1x and 2x under that limit. Player inventory handling was accepted.
- Needs snapshots during acceleration showed food, water and rest decreasing with elapsed game time. An 8x sample spanning about 0.053 game hours lost about 0.16 food, 0.21 water and 0.26 rest, consistent with the inspected baseline rates.
- Sail-a-dex 2.0.0 was present during testing. No direct simulation-speed conflict was found in the inspected code; this is not a guarantee for all mod combinations.

## Limits of verification

Continued background simulation after the alt-tab fix has helper-level checks and source review, but no explicit final player confirmation. Storms, heavy-cargo collisions, sustained 8x performance, recovery, full reload paths and controller/VR behavior were not comprehensively play-tested.

The test environment used Sailwind Steam build 24324368, Unity 2019.1.10.15730669 and BepInEx 5.4.23.5 (BepInExPack 5.4.2305). Game updates may change the methods patched by this plugin. Existing game errors were observed in logs and were not attributed to this mod without evidence.

The plugin leaves the game's fixed physics timestep, day-length settings, survival rates and save fields unchanged. It restores normal speed only while it still owns the selected value and preserves externally changed pause/sleep scales. Identical speed writes by another mod cannot be distinguished from its own writes.

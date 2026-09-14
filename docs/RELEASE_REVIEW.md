# 1.1.2 release validation

## Reported problem and change

Switching Sailwind to beta caused 1.1.1 to reject the game assembly before installing its hooks. Version 1.1.2 removes the assembly-hash allowlist. Game patches do not require approval or a new mod release merely because their binary identity changed.

Required pause, sleep and save/load boundaries remain mandatory. The timer-save transpiler still validates its semantic layout before rewriting the timer call. Save-coroutine discovery follows the factory's constructed iterator and its actual IEnumerator entry point, without depending on a generated numeric suffix.

Save continuation requires both a successfully installed timer hook with a recognized layout and the coroutine error hook. If either is unavailable, FF remains usable with a warning and all saves cancel it, including active hold mode. Unrecognized timer instructions pass through unchanged. Losing support during later transpilation resets pending save permissions and cancels owned FF. Configuration identities, default autosave policy and speed ownership remain unchanged.

After the initial beta play check, the user requested independent cycle and hold speeds. `MaxSpeed` now limits cycle mode only. `HoldSpeed` independently selects 2x, 4x or 8x. Existing setting identities and defaults are retained. Changing the cycle ceiling does not interrupt an active hold. Hold-speed or hold-binding changes still cancel it. The earlier player report predates this addition.

## Validation and limits

The orchestrator independently inspected the independent-hold implementation and tests, then built the integrated candidate against beta. All 1,197 checks passed with zero compiler warnings or errors. The current DLL SHA256 is `CBD36AA8463B2EC42BA29C081C60F9ACBCA95C5D675D9A57B47A10EBE69B344E`. Coverage includes all cycle-limit and hold-speed combinations, 4x cycling with 8x hold from idle and cycle mode, release followed by a 2x/4x/1x cycle, activity and focus handling, independent cycle-ceiling changes, hold-setting cancellation and real config save/reload. Existing save continuation and compatibility checks also pass. Live acceptance is player-reported below.

The final release ZIP contains eight verified files and the source ZIP contains 37. Metadata, versions, icon, exact inventory and every entry's SHA256 passed package validation. A fresh extraction of the current source ZIP built against beta with zero compiler warnings or errors and passed all 1,197 checks. Its test suite also passed all 1,197 checks against the retained stable references. The final packages include these completion notes, with all other source content unchanged from the tested extraction. Release ZIP, source ZIP and checksums are prepared locally for the user's publication.

The installed beta is Steam build 25217411, Assembly-CSharp.dll SHA256 `3DA76CAA73C76B5C549EAD6A5EBE94D69F675BE378477E7E2151E22D796CFC20`. The retained stable test reference matches build 24324368, SHA256 `978A21A680F42C89EBCB3530F9A99EF074960BE6377DAF8E85893134A5E5CE23`. Hashes identify test inputs only. Neither is a runtime allowlist.

The separate Sailwind research directory retains a new beta inspection map and the complete unchanged harness. Selected save call positions, iterator events and capture boundaries match the historical map. These are bounded static observations, not a save-integrity guarantee.

Before the independent-hold addition, the orchestrator's integrated beta build passed 1,172 checks with zero compiler warnings or errors. A distinct reviewer independently inspected that source and repeated the same checks, producing the same DLL SHA256 `1884A2A471BA05E1E0EBCA0CB8649F8729528ECDEA3ED52465B444049D38631F`. No blocking review findings remained for that candidate.

New regression checks cover changed assembly identities and renumbered iterators, unavailable or ambiguous factories, a discarded iterator, failed optional-hook installation, every hook-availability combination for held and cycled saves, and loss of support during active continuation. Existing tests continue to verify the recognized timer rewrite and preserve original IL for altered branches, fields, calls and arguments. Production startup wiring was inspected separately because the helper test executable does not run Plugin.Awake.

The earlier compatibility-fix release ZIP contains eight verified files and its source ZIP contains 37. Packaging checks exact inventory, per-entry hashes, metadata and version consistency. A fresh extraction of that source ZIP built against beta and passed all 1,172 checks. The same extracted test suite then passed all 1,172 checks against the retained stable references. Those package checks predate the independent-hold addition and current README edits. Byte-identical builds across different checkout paths are not claimed.

Tests use .NET 10 with actual game and loader references. They do not execute the Unity/Mono player lifecycle or install detours in a live game. No agent-performed game installation changes or publication have been performed.

## Player-reported live check

On 2026-09-14, the user reported that the game booted and FF worked as expected after the 1.1.2 fix. This supports beta startup and ordinary FF use. It is a player report, not an independent inspection of the installed DLL or logs, and does not establish a separate save/reload or degraded-hook test.

The user subsequently tested an 8x hold alongside a 4x cycle and reported that both worked perfectly. This is player-reported acceptance of the independent-speed addition. It does not constitute an independent installed-DLL hash check or a broader save-integrity claim.

The user approved packaging after the final two README edits were reviewed. The default-controls label and shortcut-example spacing are correct. The settings table was corrected to describe the independent cycle and hold speeds, with matching performance guidance. Other public wording is retained.

# 1.1.1 release validation (historical)

## Reported problems and changes

The player reported that movement stepped down hold-to-FF and that shortcuts such as `o` and `left shift + mouse 4` failed or disappeared. The actual BepInEx 5.4.23.5 converter reproduced both shortcut failures in isolated configuration files. Its case-sensitive parser returns an empty shortcut after an error, and the typed config entry then saves that empty value over the original text.

All three shortcuts now bind as text and use one local parser. Case-insensitive Unity key names and spaced names are accepted. A conventional modifier chord works in either order. Unsupported text remains in the file, disables only that shortcut and produces a warning identifying the setting. Blank values remain blank and disabled. Existing section/key identities and unrelated entries are preserved. Values already erased by an earlier version cannot be recovered automatically. Configuration editors may now present these settings as text fields rather than typed keybind controls.

An intentional hold uses HoldSpeed up to MaxSpeed and bypasses ordinary movement, inventory, bed/recovery, shipyard and menu restrictions. An already active owned hold can continue through a manual save. Cycle mode keeps its previous restrictions. Hold follows the game's reported chord state across focus changes, while new activation still requires focus. No native or external speed is overwritten after ownership is lost.

The existing autosave choices remain in effect. CancelOnAutosave=true cancels even during a hold. False permits the existing timer-save continuation. The explicit save state distinguishes a cancelled timer prefix from a permitted manual hold save. Reset, pause, loading, world transition, invalidating hold settings, plugin shutdown and errors still cancel. No pending resume or automatic reacquisition was added. A shared exception-only finalizer now covers both SaveGame entry failures and the save coroutine, cancelling FF while retaining the original exception and leaving game save state untouched.

## Validation and limits

The orchestrator's integrated build passed 1,087 checks with zero warnings or errors. These include actual-loader reproductions of both reported shortcut failures, bind/save/reload retention for all three shortcuts, invalid-input warnings and repair, held modifiers alongside movement, hold overrides at 2x/4x/8x, physical release, hard cancellation, manual saves, timer policy and save errors. The existing actual-Harmony normalization regression and semantic timer-save IL guard still pass. The accepted game assembly hash and guard were not relaxed.

The orchestrator independently inspected the implementation diff and test outputs. A distinct reviewer inspected the final source, configuration and documentation, repeated all 1,087 checks with zero warnings or errors, and produced the same DLL hash. No actionable review findings remain. The public README carries the same AI-use disclaimer as SLA, with the project name changed. The loader parsing observation and retrieval commands were also captured in the separate private Unity KB.

The release and source ZIPs passed exact inventory and per-entry hash checks. A fresh extraction of the source ZIP built and passed all 1,087 checks with separately supplied references and cached dependencies. This verifies source-package completeness, not byte-identical builds across checkout paths.

Tests run against separately supplied game/loader references under .NET 10. They do not execute the Unity/Mono player lifecycle, install detours in a live game, certify save integrity or prove every configuration editor's UI behavior. The prior 1.1.0 play observations below remain historical. The user has deferred manufactured edge-case play sessions.

## Live acceptance

On 2026-09-14, the user reported the following 1.1.1 live checks passed:

- FF started without errors.
- `o` and `left shift + mouse 4` worked and the bindings survived restart.
- Holding while walking and viewing inventory kept the speed steady. Releasing returned to 1x.
- Reset and pause cancelled FF without restarting it while the key remained held.

The user clarified that the normal-save check was not performed because their normal save action requires quitting the game. They consider this untested scenario non-blocking. Manual-save continuation has automated coverage, not live acceptance.

These are player-reported results, not a new independent log inspection. Together with the completed automated checks and separate review, they complete the agreed release acceptance. No additional stress testing is required for this release decision. The packaged DLL remains SHA256 `B159C161A034B2C57EECB2E402C7F381BEE54DF8D2D604934E431141A048EA19`.

Release files are prepared locally. Recording acceptance does not perform or authorize a commit, push or publication.

# 1.1.0 release validation (historical)

## Release decision and live observations

The user confirmed that hold-to-fast-forward works well in the game and that cargo remains intact after loading saves. The inspected latest-session logs also showed the corrected plugin reaching Ready, and the installed DLL hash matched the corrected startup build. These observations support ordinary live use without claiming a complete test matrix or save-integrity proof.

The user approved preparing 1.1.0 for release with the existing automated coverage and ordinary live-play results. Remaining unusual cancellation, error and mod-interaction scenarios are deferred until a report or live occurrence warrants investigation. They are untested limits, not mandatory release blockers. No artificial save failures or additional edge-case play sessions are required for this release decision.

The release ZIP, source ZIP and checksums are prepared locally. The user will publish the mod first. GitHub publication is deferred until the user confirms that step is complete.

## Startup correction after player testing

The player reported that the previous 531-check candidate failed during initialization with `Unexpected SaveLoadManager.Update shape at IL_0006`. The stack trace identifies this mod's AutosavePatch.Rewrite, followed by FF disabling itself. That build did not pass live startup acceptance.

The inspected HarmonyX 2.9.0.0 library expands short branches before invoking transpilers. Our earlier tests reconstructed raw game IL and missed that transformation. A new regression through the actual library's reader, preparation, normalization and transpiler invocation reproduced the exact failure before the fix. The guard now accepts only equivalent short/long branch encodings while retaining condition, destination, member and save-origin checks. Tests reject changed targets and conditions, including ordered versus unordered comparisons.

The corrected candidate passed 543 checks with zero build warnings/errors. The orchestrator and a distinct reviewer independently repeated all 543 checks. Blank shortcut values were tested against real BepInEx config binding and save/reload. They disable the shortcut, although the loader writes them back as `None`. Public config descriptions now explain blank values and omit the awkward default-collision sentences. Local packaging validates eight release entries and 30 source entries.

These checks transform IL in memory without installing a detour or running the Unity player. Later logs confirmed corrected startup, and the user's subsequent play report confirmed hold behavior. The coroutine error handler and unusual cancellation combinations have automated coverage but no dedicated live acceptance. The earlier validation record below explains the original candidate and does not override the reported startup failure or the later live observations.

## Earlier candidate and autosave investigation

The configurable reset shortcut defaults to F8 and returns directly to 1x through the existing cancellation path. HoldHotkey defaults to F9 and requests HoldSpeed (default 4x) within current speed limits. Releasing hold returns to 1x. Reset, focus loss, invalidating configuration changes and gameplay cancellation require release/new press before hold can acquire again. Reset wins simultaneous inputs, then hold, then cycle. New default bindings are disabled if they collide with an existing shortcut. Explicitly saved bindings and existing custom values are preserved.

The relocated repository is `E:\Projects\Unity\Sailwind\FastForward`. Its Git history and all original files were preserved by a SHA256-verified relocation with a retained temporary backup. The Sailwind parent holds game research and workspace instructions. Future mods can have independent sibling repositories.

The installed game investigation found one WaitForEndOfFrame yield before SaveContainer construction. Capture, BinaryFormatter.Serialize, stream close and busy=false then occur without a further coroutine yield. Thus the suspected multi-frame capture skew is unsupported for this input. CancelOnAutosave=false continues already active FF. True cancels it. No speed suspension, saved-speed resume or automatic reacquisition was added. A small explicit autosave state grants one save-prefix exemption and only its associated busy-period continuation. Cancellation clears it.

The inspected input is Assembly-CSharp.dll SHA256 `978A21A680F42C89EBCB3530F9A99EF074960BE6377DAF8E85893134A5E5CE23`, Steam build 24324368. Primary locators: SaveLoadManager.Update, SaveGame(bool), and <DoSaveGame>d__27.MoveNext (yield return IL_0043, container IL_004b, Serialize IL_0702, stream close IL_0709, busy=false IL_0739). The workspace game-research directory retains the complete read-only inspection harness and selected factual map. Independent reproduction matched that map. These are source observations, not a save-integrity guarantee.

Unlike a normal completion, an escaping coroutine exception can bypass busy=false. The candidate's exception-only finalizer cancels FF and pending hold intent on that path while preserving the original exception and leaving vanilla busy/save files alone. The inspected game binary and timer-call context are guarded so uninspected changes require reinspection instead of silently relabeling an explicit save as an autosave. These guards do not certify arbitrary other Harmony patches.

The expanded candidate build passed with zero warnings/errors and 531 checks. Coverage includes 2x/4x/8x hold sessions and multi-frame autosaves, reset/release/policy changes during saves, rejected and nested saves, no reacquisition after cancellation or completion, external speed ownership, late coroutine errors and cleanup errors, custom config persistence, and altered timer-call layouts. The checks exercise production helpers with modeled Unity input/time state. They do not execute the live Unity lifecycle.

The orchestrator independently ran packaging and reproduced all 531 passing checks. The eight-file release ZIP and 29-file source ZIP passed metadata, version, icon, exact inventory and per-entry SHA256 validation. A fresh extraction of the source ZIP built, passed the same 531 checks and produced validated packages using separately supplied game/loader references and cached NuGet dependencies. This establishes source-package completeness. Byte-identical binaries across checkout paths are not claimed.

A distinct reviewer independently inspected the production integration, evidence, tests and package inventory and ran all 531 checks. The review's alt-tab documentation correction was applied: cycle mode continues in the background, while hold mode cancels on focus loss.

Config fixtures run under .NET 10 using the local BepInEx 5.4.23.5 reference assembly and isolated scratch paths. They do not establish config ordering, Harmony finalizer execution or input/lifecycle behavior under Unity's Mono runtime. The game installation was not changed. Historical acceptance below applies to the versions named there.

Deferred scenarios for future issue-driven investigation, not prerequisites for 1.1.0:

- At rest, use hold at 2x/4x/8x, then release and confirm 1x. Test rebind/restart and modifier release. Walking/inventory must still respect the configured cap.
- Hold through a timer autosave with CancelOnAutosave=false, then release during the busy period if observable. Repeat in cycle mode at 2x/4x/8x. After reset during a save, remain at 1x after completion even if hold stays pressed.
- With CancelOnAutosave=true, start a timer autosave while accelerated and confirm cancellation with no later reacquisition. Release/press is required to use hold again.
- While holding, trigger reset, alt-tab, pause/settings, bed/recovery and load/world transition. Cancelled FF must stay off until a fresh intentional activation. Preserve native pause/sleep speed. Check lowering configured limits and any installed mod that changes timeScale.
- After a completed autosave, reload the disposable save and check position, cargo, needs and game time. Inspect logs for FF/save errors. A controlled coroutine-error/finalizer test is still a separate runtime gap. Do not provoke disk failure or corrupt a real save to test it.

Publication remains pending. Static/helper tests and the reported successful loads do not establish save-file durability or compatibility with every other mod.

# 1.0.1 validation notes

The README now covers r2modman, Thunderstore Mod Manager and manual installation. `CancelOnAutosave` defaults to `false`, preserving the selected speed during timer-triggered autosaves. `true` cancels it. Pause, bed, load and explicit save boundaries retain cancellation.

Sailwind's `SaveGame(bool)` argument selects compression, not the save type. A Harmony transpiler wraps only the timer autosave call in `SaveLoadManager.Update`. The two explicit-save calls remain unchanged. The save's busy period is exempt only when an autosave began while fast-forward was active and cancellation was disabled. Completion, cancellation and error paths clear that permission.

The build passed with zero warnings/errors and all 195 checks passing. The checks cover both autosave choices at 2x/4x/8x, the across-frame busy period, rejected saves, manual interruption and external pause/sleep scales. Cecil reads the installed game IL to verify that only the third (timer) save call is replaced. The rewrite retains labels and exception boundaries and rejects unexpected save-call counts. These checks do not run Unity or establish live Harmony execution.

The 1.0.1 play-test log confirmed the default `CancelOnAutosave = false` path: a compressed save completed while fast-forward was at 2x, and the next speed change was the pause menu restoring 1x. The installed DLL matched the candidate and no Fast Forward plugin errors were logged. The player also confirmed that `CancelOnAutosave = true` disables fast-forward on autosave. Both autosave settings have now passed live acceptance.

# 1.0.0 validation notes

The release retains the gameplay implementation accepted in test build 0.3.0. Release preparation changes version metadata, documentation, licensing, the icon and packaging.

## Automated checks

The check executable covers speed cycling and ownership, activity limits, modeled menu ordering, hotkeys with unrelated held keys/modifiers, and background-setting ownership. These checks use the production helpers, but do not execute the Unity game loop or Harmony patches.

Packaging verifies an explicit allowlist, file hashes, metadata, version consistency and icon dimensions. A passing package check is not an actual r2modman import or an in-game run of the final versioned DLL.

The 1.0.0 release build passed with zero warnings/errors and all 155 checks passing. The release ZIP and source ZIP passed metadata, icon and per-entry hash validation. A fresh extraction of the source ZIP also built, passed all 155 checks and produced verified packages. Decompiled release gameplay matched the tested 0.3.0 DLL after excluding version metadata.

## Player and log evidence

- Everyday 2x sailing: steering, sail adjustment, deck movement, moving cargo and drinking worked by player report.
- Brief 4x and 8x use worked. Earlier unwanted cancellations prompted the background-execution and island-streaming fixes.
- Opening settings canceled acceleration and resumed at 1x. Entering bed canceled acceleration and allowed native sleep.
- Routine island loading no longer canceled acceleration after the fix.
- Movement correctly reduced speed to 2x. Cycling while moving alternated between 1x and 2x under that limit. Player inventory handling was accepted.
- Needs snapshots during acceleration showed food, water and rest decreasing with elapsed game time. An 8x sample spanning about 0.053 game hours lost about 0.16 food, 0.21 water and 0.26 rest, consistent with the inspected baseline rates.
- Sail-a-dex 2.0.0 was present during testing. No direct simulation-speed conflict was found in the inspected code. This is not a guarantee for all mod combinations.

## Limits of verification

Continued background simulation after the alt-tab fix has helper-level checks and source review, but no explicit final player confirmation. Storms, heavy-cargo collisions, sustained 8x performance, recovery, full reload paths and controller/VR behavior were not comprehensively play-tested.

The test environment used Sailwind Steam build 24324368, Unity 2019.1.10.15730669 and BepInEx 5.4.23.5 (BepInExPack 5.4.2305). Game updates may change the methods patched by this plugin. Existing game errors were observed in logs and were not attributed to this mod without evidence.

The plugin leaves the game's fixed physics timestep, day-length settings, survival rates and save fields unchanged. It restores normal speed only while it still owns the selected value and preserves externally changed pause/sleep scales. Identical speed writes by another mod cannot be distinguished from its own writes.

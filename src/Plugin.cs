using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SailwindFastForward
{
    [BepInPlugin(Id, "Sailwind Fast Forward", "1.1.0")]
    [BepInProcess("Sailwind.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal const string Id = "local.sailwind.fastforward";
        private readonly SpeedOwnership speed = new SpeedOwnership();
        private readonly HoldInput holdInput = new HoldInput();
        private readonly AutosaveState autosave = new AutosaveState();
        private readonly BackgroundExecution background = new BackgroundExecution(
            () => Application.runInBackground, value => Application.runInBackground = value);
        private PluginSettings settings;
        private Action resetHotkey;
        private Action<string> cancelInput;
        private Action saveError;
        private Harmony harmony;
        private FieldInfo saveBusy;
        private bool ready;
        private GUIStyle indicatorStyle;
        private static Plugin instance;

        private void Awake()
        {
            instance = this;
            resetHotkey = () => Cancel("reset hotkey");
            cancelInput = Cancel;
            saveError = () => Cancel("save error");
            settings = new PluginSettings(Config);
            try
            {
                GameCompatibility.RequireSupported(typeof(SaveLoadManager).Assembly.Location);
                saveBusy = AccessTools.Field(typeof(SaveLoadManager), "busy");
                if (saveBusy == null || saveBusy.FieldType != typeof(bool))
                    throw new MissingFieldException("SaveLoadManager.busy");
                harmony = new Harmony(Id);
                // Pause captures the old scale synchronously; Update alone is too late.
                PatchBoundary(typeof(StartMenu), "GameToSettings", Type.EmptyTypes);
                // Catch save/load calls even when their work begins and ends between frames.
                PatchBoundary(typeof(SaveLoadManager), "SaveGame", new[] { typeof(bool) });
                PatchBoundary(typeof(SaveLoadManager), "LoadGame", new[] { typeof(int) });
                // Also covers pass-out/recovery, which calls FallAsleep before its fade.
                PatchBoundary(typeof(Sleep), "FallAsleep", Type.EmptyTypes);
                var saveUpdate = AccessTools.DeclaredMethod(typeof(SaveLoadManager), "Update", Type.EmptyTypes);
                if (saveUpdate == null) throw new MissingMethodException("SaveLoadManager.Update");
                harmony.Patch(saveUpdate, transpiler: new HarmonyMethod(typeof(Plugin), nameof(MarkAutosave)));
                var iterator = typeof(SaveLoadManager).GetNestedType("<DoSaveGame>d__27", BindingFlags.NonPublic);
                var moveNext = iterator == null ? null : AccessTools.DeclaredMethod(iterator, "MoveNext", Type.EmptyTypes);
                if (moveNext == null || moveNext.ReturnType != typeof(bool))
                    throw new MissingMethodException("SaveLoadManager.<DoSaveGame>d__27.MoveNext");
                harmony.Patch(moveNext, finalizer: new HarmonyMethod(typeof(Plugin), nameof(AfterSaveCoroutine)));
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                SceneManager.sceneLoaded += OnSceneLoaded;
                ready = true;
                Logger.LogInfo($"Ready. {settings.Hotkey.Value} to cycle; {settings.ResetHotkey.Value} to return to 1x; hold {settings.HoldHotkey.Value} for {settings.HoldSpeed.Value}x within speed limits.");
            }
            catch (Exception error)
            {
                Logger.LogError($"Initialization failed; fast-forward disabled: {error}");
                harmony?.UnpatchSelf();
                enabled = false;
            }
        }

        private void PatchBoundary(Type type, string name, Type[] arguments)
        {
            var method = AccessTools.DeclaredMethod(type, name, arguments);
            if (method == null)
                throw new MissingMethodException(type.FullName, name);
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(Plugin), nameof(BeforeBoundary)));
        }

        private static void BeforeBoundary(MethodBase __originalMethod)
        {
            if (instance == null) return;
            if (__originalMethod.DeclaringType == typeof(SaveLoadManager) &&
                __originalMethod.Name == "SaveGame" && instance.autosave.TryEnterSave()) return;
            instance.Cancel($"{__originalMethod.DeclaringType.Name}.{__originalMethod.Name}");
        }

        private static IEnumerable<CodeInstruction> MarkAutosave(IEnumerable<CodeInstruction> instructions) =>
            AutosavePatch.Rewrite(instructions,
                AccessTools.DeclaredMethod(typeof(SaveLoadManager), "SaveGame", new[] { typeof(bool) }),
                AccessTools.DeclaredMethod(typeof(Plugin), nameof(SaveAutosave)));

        private static Exception AfterSaveCoroutine(Exception __exception) =>
            SaveErrorBoundary.Handle(__exception, instance?.saveError);

        private static void SaveAutosave(SaveLoadManager manager, bool compressed)
        {
            var plugin = instance;
            if (plugin == null || !plugin.ready)
            {
                manager.SaveGame(compressed);
                return;
            }
            bool alreadyBusy = (bool)plugin.saveBusy.GetValue(manager);
            plugin.autosave.Begin(!plugin.settings.CancelOnAutosave.Value && plugin.speed.Active && !alreadyBusy);
            try { manager.SaveGame(compressed); }
            catch (Exception error)
            {
                SaveErrorBoundary.Handle(error, plugin.saveError);
                throw;
            }
            finally { plugin.autosave.End((bool)plugin.saveBusy.GetValue(manager)); }
        }

        private string BlockReason()
        {
            if (!GameState.playing) return "outside gameplay";
            if (GameState.currentlyLoading) return "save loading";
            if (GameState.justStarted || GameState.changingStartRegion) return "world transition";
            // loadingScenes and loadingBoatLocalItems also describe routine island/boat streaming.
            if (GameState.sleeping || GameState.eyesFullyClosed || Sleep.timeskipSleep ||
                GameState.justWokeUp || GameState.inBed) return "sleep/bed";
            if (GameState.recovering) return "recovery";
            if (GameState.wasInSettingsMenu) return "settings menu";
            if (GameState.currentShipyard) return "shipyard";
            if (EconomyUI.instance && EconomyUI.instance.uiActive) return "economy menu";
            if (GameState.inCursorMenu && !InventoryOpen()) return "cursor menu";
            if (!SaveLoadManager.instance) return "save unavailable";
            if (autosave.BlocksBusySave((bool)saveBusy.GetValue(SaveLoadManager.instance), settings.CancelOnAutosave.Value)) return "save busy";
            return null;
        }

        private static bool InventoryOpen() => PlayerNeedsUI.instance && PlayerNeedsUI.instance.IsActive();

        private static bool MovementRequested()
        {
            var controller = Refs.ovrController;
            if (!Application.isFocused || !controller || !controller.isActiveAndEnabled || !controller.EnableLinearMovement)
                return false;
            bool halted = false;
            controller.GetHaltUpdateMovement(ref halted);
            if (halted) return false;
            // Use the game's rebound locomotion controls, never world/boat velocity.
            return GameInput.GetKey(InputName.MoveUp) || GameInput.GetKey(InputName.MoveDown) ||
                GameInput.GetKey(InputName.MoveLeft) || GameInput.GetKey(InputName.MoveRight) ||
                GameInput.GetKey(InputName.Jump) ||
                Mathf.Abs(GameInput.GetPrimaryHorizontal()) > 0.1f || Mathf.Abs(GameInput.GetPrimaryVertical()) > 0.1f;
        }

        private void Update()
        {
            if (!ready) return;
            try
            {
                string reason = BlockReason();
                bool inventory = reason == null && InventoryOpen();
                bool moving = reason == null && !inventory && MovementRequested();
                int effectiveMaximum = (inventory || moving)
                    ? Math.Min(settings.MaxSpeed.Value, settings.MovementInventoryMaxSpeed.Value) : settings.MaxSpeed.Value;
                float previous = Time.timeScale;
                var action = holdInput.Dispatch(speed, previous, settings.MaxSpeed.Value, settings.MovementInventoryMaxSpeed.Value,
                    effectiveMaximum, Application.isFocused, reason, settings.Hotkey.Value, settings.ResetHotkey.Value,
                    settings.HoldHotkey.Value, settings.HoldSpeed.Value, Input.GetKeyDown, Input.GetKey, cancelInput, resetHotkey);
                if (action == SpeedInputAction.Limit)
                    ApplySelectedSpeed(previous, inventory ? "inventory speed limit" : "movement speed limit");
                else if (action == SpeedInputAction.Cycle)
                    ApplySelectedSpeed(previous, "hotkey");
                else if (action == SpeedInputAction.Hold)
                    ApplySelectedSpeed(previous, "hold hotkey");
                else if (action == SpeedInputAction.Unavailable)
                    Logger.LogInfo($"Fast-forward unavailable at native/external scale {Time.timeScale}.");
            }
            catch (Exception error)
            {
                Cancel("runtime error");
                Logger.LogError($"Fast-forward disabled: {error}");
                enabled = false;
            }
        }

        private void ApplySelectedSpeed(float previous, string reason)
        {
            Time.timeScale = speed.SelectedSpeed;
            if (speed.Active) background.Enable();
            else background.Restore();
            Logger.LogInfo($"{previous}x -> {Time.timeScale}x ({reason})");
            LogNeeds();
        }

        private void Cancel(string reason)
        {
            holdInput.Invalidate();
            autosave.Reset();
            if (!speed.Active) return;
            float previous = Time.timeScale;
            if (speed.Release(previous)) Time.timeScale = 1f;
            background.Restore();
            Logger.LogInfo($"Fast-forward off ({reason}); scale {previous} -> {Time.timeScale}.");
            LogNeeds();
        }

        private void LogNeeds()
        {
            if (!GameState.playing || !Sun.sun || !PlayerNeeds.instance) return;
            Logger.LogDebug($"Needs: realSeconds={Time.realtimeSinceStartup:F1}, day={GameState.day}, hour={Sun.sun.globalTime:F3}, " +
                $"food={PlayerNeeds.food:F2}, water={PlayerNeeds.water:F2}, rest={PlayerNeeds.sleep:F2}, " +
                $"sunRate={Sun.sun.timescale:F5}, godMode={PlayerNeeds.instance.godMode}.");
        }

        private void OnGUI()
        {
            if (!ready || !speed.Active || Time.timeScale != speed.SelectedSpeed || !settings.ShowIndicator.Value) return;
            if (indicatorStyle == null)
                indicatorStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width - 76, 16, 60, 28), $"{speed.SelectedSpeed}\u00d7", indicatorStyle);
        }

        private void OnActiveSceneChanged(Scene previous, Scene next) => Cancel("active scene changed");
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Cancel("world scene loaded");
        }
        private void OnApplicationQuit() => Cancel("quit");
        private void OnDisable() => Cancel("plugin disabled");

        private void OnDestroy()
        {
            ready = false;
            Cancel("plugin destroyed");
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            harmony?.UnpatchSelf();
            if (instance == this) instance = null;
        }
    }
}

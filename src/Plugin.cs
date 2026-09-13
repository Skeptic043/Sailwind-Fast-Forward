using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SailwindFastForward
{
    [BepInPlugin(Id, "Sailwind Fast Forward", "1.0.0")]
    [BepInProcess("Sailwind.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal const string Id = "local.sailwind.fastforward";
        private readonly SpeedOwnership speed = new SpeedOwnership();
        private readonly BackgroundExecution background = new BackgroundExecution(
            () => Application.runInBackground, value => Application.runInBackground = value);
        private ConfigEntry<KeyboardShortcut> hotkey;
        private ConfigEntry<bool> showIndicator;
        private ConfigEntry<int> maxSpeed;
        private ConfigEntry<int> movementInventoryMaxSpeed;
        private Harmony harmony;
        private FieldInfo saveBusy;
        private bool ready;
        private GUIStyle indicatorStyle;
        private static Plugin instance;

        private void Awake()
        {
            instance = this;
            hotkey = Config.Bind("Controls", "Hotkey", new KeyboardShortcut(KeyCode.F7),
                "Cycle simulation speed.");
            maxSpeed = Config.Bind("Simulation", "MaxSpeed", 4,
                new ConfigDescription("Maximum fast-forward speed.",
                    new AcceptableValueList<int>(2, 4, 8)));
            movementInventoryMaxSpeed = Config.Bind("Simulation", "MovementInventoryMaxSpeed", 2,
                new ConfigDescription("Speed limit while moving or viewing inventory. 1 disables fast-forward; 8 allows all speeds.",
                    new AcceptableValueList<int>(1, 2, 4, 8)));
            showIndicator = Config.Bind("Display", "ShowIndicator", true,
                "Show the current fast-forward speed.");
            try
            {
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
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                SceneManager.sceneLoaded += OnSceneLoaded;
                ready = true;
                Logger.LogInfo($"Ready. {hotkey.Value} to cycle game speed.");
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
            if (instance != null)
                instance.Cancel($"{__originalMethod.DeclaringType.Name}.{__originalMethod.Name}");
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
            if (!SaveLoadManager.instance || (bool)saveBusy.GetValue(SaveLoadManager.instance)) return "save unavailable/busy";
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
                if (reason != null)
                {
                    Cancel(reason);
                    return;
                }
                if (speed.Active && Time.timeScale != speed.SelectedSpeed)
                {
                    Cancel("timescale changed externally");
                    return;
                }
                if (speed.SelectedSpeed > maxSpeed.Value)
                {
                    Cancel("configured maximum lowered");
                    return;
                }
                bool inventory = InventoryOpen();
                bool moving = !inventory && MovementRequested();
                int effectiveMaximum = (inventory || moving)
                    ? Math.Min(maxSpeed.Value, movementInventoryMaxSpeed.Value) : maxSpeed.Value;
                float previous = Time.timeScale;
                if (speed.TryLimit(previous, effectiveMaximum))
                {
                    ApplySelectedSpeed(previous, inventory ? "inventory speed limit" : "movement speed limit");
                    return;
                }
                // Background simulation continues, but keys in another app must not cycle speed.
                if (!Application.isFocused) return;
                var shortcut = hotkey.Value;
                if (!HotkeyInput.IsDown(shortcut.MainKey, shortcut.Modifiers, Input.GetKeyDown, Input.GetKey)) return;
                if (effectiveMaximum == 1) return;
                if (speed.TryCycle(previous, effectiveMaximum))
                {
                    ApplySelectedSpeed(previous, "hotkey");
                }
                else
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
            if (!ready || !speed.Active || Time.timeScale != speed.SelectedSpeed || !showIndicator.Value) return;
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

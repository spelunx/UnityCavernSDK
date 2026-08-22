using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// True fullscreen Game View in the Unity Editor (Windows + macOS).
    /// No menus, no toolbar, no status bar, no tabs, no OS taskbar/dock.
    ///
    /// USAGE:
    ///   - Press F11 (configurable) during Play Mode to toggle fullscreen on/off
    ///   - Press Escape (configurable) to exit fullscreen without stopping play
    ///   - Menu: Tools → Rowlan → Fullscreen → Fullscreen On Play — toggles auto-fullscreen on play
    ///   - Press F12 (configurable) to force-exit fullscreen and reset the editor layout
    ///   - Preferences: Edit → Preferences → Rowlan/Fullscreen — configure keybinds and behavior
    ///
    /// REQUIREMENTS:
    ///   - Unity 6.000.40+ (for hiding GameView toolbar via showToolbar property)
    ///
    /// State persistence and orphan recovery after domain reload are handled by
    /// FullscreenStateGuard. This class focuses on the popup lifecycle, platform
    /// calls, keybind injection, and editor callbacks.
    /// </summary>
    public static class FullscreenGameView
    {
        #region Private State

        private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

        private static readonly PropertyInfo ShowToolbarProperty =
            GameViewType?.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ShowStatsField =
            GameViewType?.GetField("m_Stats", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ShowGizmosField =
            GameViewType?.GetField("m_Gizmos", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo VSyncEnabledProperty =
            GameViewType?.GetProperty("vSyncEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo TargetDisplayField =
            GameViewType?.GetField("m_TargetDisplay", BindingFlags.Instance | BindingFlags.NonPublic);

        private const int DisplayInactive = 7;

        private static EditorWindow fullscreenInstance;
        private static bool sceneViewWasOpen;
        private static int originalVSyncCount;
        private static bool originalCursorVisible;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the editor is currently in fullscreen mode.
        /// Checks the in-memory reference only. After domain reload this will
        /// be false even if an orphaned popup still exists — use
        /// FullscreenStateGuard.WasFullscreen to detect that case.
        /// </summary>
        public static bool IsFullscreen => IsAlive(fullscreenInstance);

        #endregion

        #region Menu Items

        /// <summary>
        /// Toggles the auto-fullscreen preference. When checked, entering Play Mode
        /// will automatically activate fullscreen. This menu item performs no immediate action.
        /// </summary>
        [MenuItem("Tools/Rowlan/Fullscreen/Fullscreen On Play", priority = 2)]
        private static void ToggleAutoFullscreen()
        {
            FullscreenSettings.AutoFullscreen = !FullscreenSettings.AutoFullscreen;
        }

        /// <summary>
        /// Validates the menu item checkmark state to reflect the current preference.
        /// </summary>
        [MenuItem("Tools/Rowlan/Fullscreen/Fullscreen On Play", true)]
        private static bool ToggleAutoFullscreenValidate()
        {
            Menu.SetChecked("Tools/Rowlan/Fullscreen/Fullscreen On Play", FullscreenSettings.AutoFullscreen);
            return true;
        }

        /// <summary>
        /// Exits fullscreen and resets the Unity editor layout to default.
        /// </summary>
        [MenuItem("Tools/Rowlan/Fullscreen/Fullscreen Reset", priority = 3)]
        private static void ResetUnityLayout()
        {
            RunForceCleanup();

            EditorApplication.delayCall += () =>
            {
                EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggles fullscreen mode on or off. Called by the runtime key listener via delegate.
        /// </summary>
        public static void Toggle()
        {
            if (GameViewType == null)
            {
                Debug.LogError("[Fullscreen] GameView type not found.");
                return;
            }

            if (ShowToolbarProperty == null)
            {
                Debug.LogWarning("[Fullscreen] GameView.showToolbar not found. " +
                                 "Play toolbar may remain visible. Requires Unity 6.000.40+.");
            }

            if (IsFullscreen)
                ExitFullscreen();
            else
                EnterFullscreen();
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// Creates a borderless popup GameView covering the entire screen
        /// and invokes platform-specific logic to cover the OS taskbar/dock.
        /// </summary>
        private static async void EnterFullscreen()
        {
            // Safety: clean up any orphaned fullscreen state before creating a new one.
            if (FullscreenStateGuard.WasFullscreen)
            {
                if (FullscreenSettings.DebugLogging)
                    Debug.Log("[Fullscreen] Cleaning up orphaned fullscreen state before re-entering.");
                RunForceCleanup();
            }

            // Capture overlay states from the original GameView before redirecting it
            EditorWindow originalGameView = GetMainGameView();
            bool showStats = GetFieldValue<bool>(originalGameView, ShowStatsField);
            bool showGizmos = GetFieldValue<bool>(originalGameView, ShowGizmosField);

            // Save the original target display so we can restore it on exit
            int origDisplay = GetGameViewTargetDisplay(originalGameView);

            // Capture the ContainerWindow position BEFORE the async gap.
            Rect containerRect = GetContainerWindowRect(originalGameView);

            // Fix resolution when entering fullscreen from a non-fullscreen Game View:
            // Unity's Game View can latch onto a stale resolution from its previous
            // target display configuration. Briefly switching to an intermediate unused
            // display and waiting one frame forces Unity to recalculate the render
            // target resolution before the final redirect.
            SetGameViewTargetDisplay(DisplayInactive - 1);
            await Awaitable.NextFrameAsync();

            // Redirect original GameView to unused display to avoid double-rendering
            SetGameViewTargetDisplay(DisplayInactive);

            // Create a fresh GameView instance
            fullscreenInstance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);

            // Hide the play/pause toolbar inside the GameView
            ShowToolbarProperty?.SetValue(fullscreenInstance, false);

            // Copy overlay states from the original GameView
            SetFieldValue(fullscreenInstance, ShowStatsField, showStats);
            SetFieldValue(fullscreenInstance, ShowGizmosField, showGizmos);

            // Place the popup at the ContainerWindow's position so it spawns on the
            // correct monitor. The platform layer (MonitorFromWindow on Win32) will
            // then detect the actual monitor and resize to cover it fully.
            fullscreenInstance.ShowPopup();
            fullscreenInstance.position = containerRect;
            fullscreenInstance.Focus();

            // Pause the Scene View to save rendering cost while in fullscreen
            PauseSceneView();

            // Apply VSync preference (save original to restore later)
            ApplyVSync();

            // Hide cursor if configured
            ApplyCursorVisibility();

            // Persist state so FullscreenStateGuard can recover after domain reload
#if UNITY_6000_5_OR_NEWER
            FullscreenStateGuard.Persist(fullscreenInstance.GetEntityId(), origDisplay, originalVSyncCount);
#else
            FullscreenStateGuard.Persist(fullscreenInstance.GetInstanceID(), origDisplay, originalVSyncCount);
#endif

            // Platform-specific: detect correct monitor via OS APIs and cover it fully
            FullscreenPlatform.OnEnterFullscreen(fullscreenInstance);

            if (FullscreenSettings.DebugLogging)
                Debug.Log($"[Fullscreen] Entered (containerPos={containerRect}). " +
                          $"Press {FullscreenSettings.ToggleKey} or {FullscreenSettings.ExitKey} to exit.");
        }

        /// <summary>
        /// Closes the fullscreen popup, restores platform window state,
        /// and redirects the original GameView back to its original display.
        /// </summary>
        private static void ExitFullscreen()
        {
            FullscreenPlatform.OnExitFullscreen();

            if (IsAlive(fullscreenInstance))
            {
                try { fullscreenInstance.Close(); } catch { }
            }
            fullscreenInstance = null;

            // Read display to restore from persisted state (survives domain reload)
            int displayToRestore = FullscreenStateGuard.OriginalDisplay;

            // Clear persisted state now that we're exiting cleanly
            FullscreenStateGuard.Clear();

            // Defer display restoration to next editor frame, as editor windows
            // may still be mid-destruction during ExitingPlayMode
            EditorApplication.delayCall += () =>
            {
                try { SetGameViewTargetDisplay(displayToRestore); } catch { }
            };

            // Restore the Scene View if it was open before entering fullscreen
            try { ResumeSceneView(); } catch { }

            // Restore original VSync setting
            RestoreVSync();

            // Restore original cursor visibility
            RestoreCursorVisibility();

            if (FullscreenSettings.DebugLogging)
                Debug.Log("[Fullscreen] Exited fullscreen.");
        }

        /// <summary>
        /// Delegates to FullscreenStateGuard.ForceCleanup, passing the in-memory
        /// instance and a callback for display restoration. Clears the local
        /// static reference afterward.
        /// </summary>
        private static void RunForceCleanup()
        {
            FullscreenStateGuard.ForceCleanup(fullscreenInstance, SetGameViewTargetDisplay);
            fullscreenInstance = null;
        }

#endregion

        #region Helpers

        /// <summary>
        /// Gets the position of the OS-level ContainerWindow that hosts the given EditorWindow.
        /// This is the actual window rect in virtual screen coordinates, unlike
        /// EditorWindow.position which for docked windows returns view-relative coordinates.
        ///
        /// Accesses: editorWindow → m_Parent (View) → window (ContainerWindow) → position (Rect)
        /// Falls back to a zero-origin rect using Screen.currentResolution if reflection fails.
        /// </summary>
        private static Rect GetContainerWindowRect(EditorWindow editorWindow)
        {
            if (editorWindow == null)
                return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);

            try
            {
                var parentField = typeof(EditorWindow).GetField("m_Parent",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (parentField == null) return editorWindow.position;

                var parent = parentField.GetValue(editorWindow);
                if (parent == null) return editorWindow.position;

                var windowProp = parent.GetType().GetProperty("window",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (windowProp == null) return editorWindow.position;

                var containerWindow = windowProp.GetValue(parent);
                if (containerWindow == null) return editorWindow.position;

                var positionProp = containerWindow.GetType().GetProperty("position",
                    BindingFlags.Instance | BindingFlags.Public);
                if (positionProp == null) return editorWindow.position;

                return (Rect)positionProp.GetValue(containerWindow);
            }
            catch
            {
                return editorWindow.position;
            }
        }

        /// <summary>
        /// Checks whether a UnityEngine.Object reference is both non-null in C#
        /// and has not been destroyed by Unity.
        /// </summary>
        private static bool IsAlive(UnityEngine.Object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            try { return obj != null; }
            catch { return false; }
        }

        /// <summary>
        /// Finds and returns the main GameView editor window instance.
        /// Skips the fullscreen popup if one exists.
        /// </summary>
        private static EditorWindow GetMainGameView()
        {
            if (GameViewType == null) return null;

            UnityEngine.Object[] gameViews = Resources.FindObjectsOfTypeAll(GameViewType);
            foreach (var gv in gameViews)
            {
                if (!IsAlive(gv)) continue;

                // Skip our own fullscreen popup
#if UNITY_6000_5_OR_NEWER
                if (IsAlive(fullscreenInstance) && gv.GetEntityId() == fullscreenInstance.GetEntityId())
                    continue;
#else
                if (IsAlive(fullscreenInstance) && gv.GetInstanceID() == fullscreenInstance.GetInstanceID())
                    continue;
#endif

                return gv as EditorWindow;
            }
            return null;
        }

        /// <summary>
        /// Reads the current target display index from a GameView instance.
        /// </summary>
        private static int GetGameViewTargetDisplay(EditorWindow gameView)
        {
            if (!IsAlive(gameView) || TargetDisplayField == null) return 0;
            try { return (int)TargetDisplayField.GetValue(gameView); }
            catch { return 0; }
        }

        /// <summary>
        /// Redirects the main GameView to render from a specific display index.
        /// </summary>
        private static void SetGameViewTargetDisplay(int displayIndex)
        {
            EditorWindow gameView = GetMainGameView();
            if (!IsAlive(gameView)) return;

            try
            {
                gameView.GetType().InvokeMember(
                    "SetTargetDisplay",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                    null,
                    gameView,
                    new object[] { displayIndex }
                );
            }
            catch { }
        }

        /// <summary>
        /// Safely reads a private field value from an EditorWindow via reflection.
        /// </summary>
        private static T GetFieldValue<T>(EditorWindow window, FieldInfo field)
        {
            if (window == null || field == null) return default;
            return (T)field.GetValue(window);
        }

        /// <summary>
        /// Safely writes a private field value on an EditorWindow via reflection.
        /// </summary>
        private static void SetFieldValue(EditorWindow window, FieldInfo field, object value)
        {
            if (window == null || field == null) return;
            field.SetValue(window, value);
        }

        /// <summary>
        /// Disables Scene View rendering to save GPU/CPU cost while in fullscreen.
        /// </summary>
        private static void PauseSceneView()
        {
            sceneViewWasOpen = SceneView.sceneViews.Count > 0;

            foreach (SceneView sv in SceneView.sceneViews)
            {
                sv.sceneViewState.alwaysRefresh = false;
                sv.autoRepaintOnSceneChange = false;
            }
        }

        /// <summary>
        /// Restores Scene View rendering after exiting fullscreen.
        /// </summary>
        private static void ResumeSceneView()
        {
            if (!sceneViewWasOpen) return;

            foreach (SceneView sv in SceneView.sceneViews)
            {
                sv.autoRepaintOnSceneChange = true;
                sv.Repaint();
            }
        }

        /// <summary>
        /// Saves the current VSync setting and applies the fullscreen preference.
        /// </summary>
        private static void ApplyVSync()
        {
            originalVSyncCount = QualitySettings.vSyncCount;

            bool vsync = FullscreenSettings.VSync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;

            if (IsAlive(fullscreenInstance))
                VSyncEnabledProperty?.SetValue(fullscreenInstance, vsync);
        }

        /// <summary>
        /// Restores the VSync setting that was active before entering fullscreen.
        /// </summary>
        private static void RestoreVSync()
        {
            QualitySettings.vSyncCount = originalVSyncCount;
        }

        /// <summary>
        /// Saves the current cursor visibility and hides it if configured.
        /// </summary>
        private static void ApplyCursorVisibility()
        {
            originalCursorVisible = Cursor.visible;

            if (FullscreenSettings.HideCursor)
                Cursor.visible = false;
        }

        /// <summary>
        /// Restores the cursor visibility that was active before entering fullscreen.
        /// </summary>
        private static void RestoreCursorVisibility()
        {
            Cursor.visible = originalCursorVisible;
        }

#endregion

        #region Callbacks

        /// <summary>
        /// Registers callbacks and delegates orphan recovery to FullscreenStateGuard.
        ///
        /// [InitializeOnLoadMethod] runs after every domain reload. The guard checks
        /// if SessionState says "fullscreen was active" while the in-memory reference
        /// is gone, and schedules ForceCleanup if so.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            FullscreenStateGuard.RecoverIfOrphaned(
                IsAlive(fullscreenInstance),
                RunForceCleanup);

            // After a domain reload during Play Mode, Unity does NOT re-fire
            // EnteredPlayMode — we land here already playing. The key listener
            // GameObject may have survived (DontDestroyOnLoad + HideAndDontSave)
            // but its delegates were wiped because they pointed at static methods
            // whose containing class was reloaded. Re-spawn and re-inject so
            // hotkeys keep working.
            if (EditorApplication.isPlaying && FullscreenSettings.Enabled)
            {
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlaying) return;
                    var listener = SpawnKeyListener();
                    InjectKeybinds(listener);
                };
            }
        }

        /// <summary>
        /// Handles Play Mode transitions: injects keybinds, wires delegates,
        /// spawns the key listener on enter, auto-enters fullscreen if enabled,
        /// and runs ForceCleanup on stop.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (!FullscreenSettings.Enabled) return;

                var listener = SpawnKeyListener();
                InjectKeybinds(listener);

                if (FullscreenSettings.AutoFullscreen)
                    EditorApplication.delayCall += () => EnterFullscreen();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Always attempt cleanup when returning to edit mode.
                // After domain reload the static is null but the popup and
                // platform state may still be alive.
                if (IsFullscreen || FullscreenStateGuard.WasFullscreen)
                {
                    RunForceCleanup();
                }

                // Remove the key listener so it cannot survive into the next play
                // session and stack with a freshly spawned one (Reload Domain off).
                DestroyKeyListeners();
            }
        }

        /// <summary>
        /// Copies the current keybind preferences into the shared runtime FullscreenKeybinds,
        /// wires up the delegate callbacks, and re-resolves the cached KeyControls.
        /// </summary>
        private static void InjectKeybinds(FullscreenKeyListener listener)
        {
            FullscreenKeybinds.ToggleKey = FullscreenSettings.ToggleKey;
            FullscreenKeybinds.ExitKey = FullscreenSettings.ExitKey;
            FullscreenKeybinds.ResetKey = FullscreenSettings.ResetKey;

            listener.OnTogglePressed = Toggle;
            listener.IsFullscreen = () => IsFullscreen;
            listener.OnResetPressed = ResetUnityLayout;

            listener.enabled = listener.ResolveAndValidateKeybinds();
        }

        /// <summary>
        /// Creates a hidden, persistent GameObject with the FullscreenKeyListener attached.
        /// </summary>
        private static FullscreenKeyListener SpawnKeyListener()
        {
            // FindFirstObjectByType / FindAnyObjectByType cannot see this object:
            // both skip anything with HideFlags.DontSave, which HideAndDontSave
            // includes. Only Resources.FindObjectsOfTypeAll returns hidden /
            // DontSave objects, so it is the one reliable way to detect a listener
            // that survived a previous play session (which happens when
            // "Reload Domain" is disabled) and avoid stacking duplicates that each
            // fire Toggle() and cancel each other out.
            var existing = Resources.FindObjectsOfTypeAll<FullscreenKeyListener>();
            if (existing.Length > 0)
            {
                // Destroy any extras so the toggle key fires exactly once per press.
                for (int i = 1; i < existing.Length; i++)
                {
                    if (existing[i] != null)
                        UnityEngine.Object.DestroyImmediate(existing[i].gameObject);
                }
                return existing[0];
            }

            var go = new GameObject("[FullscreenKeyListener]");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent<FullscreenKeyListener>();
        }

        /// <summary>
        /// Destroys every FullscreenKeyListener instance, including hidden ones.
        /// Uses Resources.FindObjectsOfTypeAll because the listeners are flagged
        /// HideAndDontSave and are invisible to the Find...ByType methods.
        /// </summary>
        private static void DestroyKeyListeners()
        {
            foreach (var listener in Resources.FindObjectsOfTypeAll<FullscreenKeyListener>())
            {
                if (listener != null)
                    UnityEngine.Object.DestroyImmediate(listener.gameObject);
            }
        }

#endregion
    }
}

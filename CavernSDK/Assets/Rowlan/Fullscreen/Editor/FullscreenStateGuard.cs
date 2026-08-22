using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Owns all domain-reload-safe state persistence and orphan recovery for
    /// the fullscreen feature. Persists critical values to SessionState (which
    /// survives domain reload but not editor restart) and provides ForceCleanup
    /// as a single idempotent method that tears down everything — in-memory
    /// references, orphaned popup windows, platform state, display routing,
    /// VSync, cursor, and Scene View rendering.
    ///
    /// FullscreenGameView calls into this class at three points:
    ///   - Persist()      when entering fullscreen
    ///   - Clear()        when exiting fullscreen normally
    ///   - ForceCleanup() from the Reset menu, EnteredEditMode, and pre-enter safety
    ///
    /// [InitializeOnLoadMethod] runs after every domain reload and triggers
    /// ForceCleanup if orphaned state is detected.
    /// </summary>
    public static class FullscreenStateGuard
    {
        #region SessionState Keys

        private const string KeyIsFullscreen = "Rowlan.Fullscreen.IsFullscreen";
        private const string KeyInstanceId = "Rowlan.Fullscreen.InstanceId";
        private const string KeyOriginalDisplay = "Rowlan.Fullscreen.OriginalDisplay";
        private const string KeyOriginalVSync = "Rowlan.Fullscreen.OriginalVSync";

        #endregion

        #region Reflected Types (cached once)

        private static readonly Type GameViewType =
            Type.GetType("UnityEditor.GameView,UnityEditor");

        private static readonly PropertyInfo ShowToolbarProperty =
            GameViewType?.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether SessionState believes fullscreen was active. True even after
        /// domain reload wipes in-memory references — this is the flag that
        /// triggers orphan recovery.
        /// </summary>
        public static bool WasFullscreen =>
            SessionState.GetBool(KeyIsFullscreen, false);

        /// <summary>
        /// The original display index saved when fullscreen was entered.
        /// Falls back to 0 (Display 1) if not persisted.
        /// </summary>
        public static int OriginalDisplay =>
            SessionState.GetInt(KeyOriginalDisplay, 0);

        #endregion

        #region Persist / Clear

        /// <summary>
        /// Persists fullscreen state to SessionState so it survives domain reload.
        /// Called by FullscreenGameView.EnterFullscreen after the popup is created.
        /// </summary>
        /// <param name="instanceId">GetInstanceID() of the fullscreen popup.</param>
        /// <param name="originalDisplay">The GameView target display before redirect.</param>
        /// <param name="originalVSyncCount">QualitySettings.vSyncCount before override.</param>
#if UNITY_6000_5_OR_NEWER
        public static void Persist(EntityId instanceId, int originalDisplay, int originalVSyncCount)
        {
            SessionState.SetBool(KeyIsFullscreen, true);
            SessionState.SetEntityId(KeyInstanceId, instanceId);
            SessionState.SetInt(KeyOriginalDisplay, originalDisplay);
            SessionState.SetInt(KeyOriginalVSync, originalVSyncCount);
        }
#else
        public static void Persist(int instanceId, int originalDisplay, int originalVSyncCount)
        {
            SessionState.SetBool(KeyIsFullscreen, true);
            SessionState.SetInt(KeyInstanceId, instanceId);
            SessionState.SetInt(KeyOriginalDisplay, originalDisplay);
            SessionState.SetInt(KeyOriginalVSync, originalVSyncCount);
        }
#endif

        /// <summary>
        /// Clears persisted fullscreen state from SessionState.
        /// Called by FullscreenGameView.ExitFullscreen on a clean exit.
        /// </summary>
        public static void Clear()
        {
            SessionState.SetBool(KeyIsFullscreen, false);

#if UNITY_6000_5_OR_NEWER
            SessionState.SetEntityId(KeyInstanceId, EntityId.None);
#else
            SessionState.SetInt(KeyInstanceId, 0);
#endif
        }

        #endregion

        #region ForceCleanup

        /// <summary>
        /// Unconditionally tears down all fullscreen state — both in-memory and
        /// persisted. Finds and closes orphaned popup GameViews by instance ID
        /// or by scanning for any GameView with a hidden toolbar (our unique
        /// fingerprint). Restores platform state, display routing, cursor, and
        /// VSync regardless of whether the in-memory reference is still valid.
        ///
        /// Safe to call multiple times — all operations are idempotent.
        ///
        /// <param name="liveInstance">
        /// The in-memory fullscreen popup reference from FullscreenGameView,
        /// or null if already lost (e.g. after domain reload).
        /// </param>
        /// <param name="setGameViewTargetDisplay">
        /// Callback into FullscreenGameView to restore the original display.
        /// Avoids exposing internal helpers publicly.
        /// </param>
        /// </summary>
        public static void ForceCleanup(
            EditorWindow liveInstance,
            Action<int> setGameViewTargetDisplay)
        {
            // 1. Restore platform state (topmost flag, Dock/menubar) unconditionally.
            //    On Windows this is harmless if popupHwnd is already zero.
            //    On Mac this resets presentation options to default.
            FullscreenPlatform.OnExitFullscreen();

            // 2. Close the in-memory instance if we still have it
            if (IsAlive(liveInstance))
            {
                try { liveInstance.Close(); } catch { }
            }

#if UNITY_6000_5_OR_NEWER
            // 3. Try to find and close the orphaned popup by persisted instance ID
            EntityId persistedId = SessionState.GetEntityId(KeyInstanceId, EntityId.None);
            if (persistedId != EntityId.None && GameViewType != null)
            {
                foreach (var obj in Resources.FindObjectsOfTypeAll(GameViewType))
                {
                    if (IsAlive(obj) && obj.GetEntityId() == persistedId)
                    {
                        try { ((EditorWindow)obj).Close(); } catch { }
                        if (FullscreenSettings.DebugLogging)
                            Debug.Log($"[Fullscreen] Closed orphaned popup by instance ID {persistedId}.");
                        break;
                    }
                }
            }
#else
            // 3. Try to find and close the orphaned popup by persisted instance ID
            int persistedId = SessionState.GetInt(KeyInstanceId, 0);
            if (persistedId != 0 && GameViewType != null)
            {
                foreach (var obj in Resources.FindObjectsOfTypeAll(GameViewType))
                {
                    if (IsAlive(obj) && obj.GetInstanceID() == persistedId)
                    {
                        try { ((EditorWindow)obj).Close(); } catch { }
                        if (FullscreenSettings.DebugLogging)
                            Debug.Log($"[Fullscreen] Closed orphaned popup by instance ID {persistedId}.");
                        break;
                    }
                }
            }
#endif
            // 4. Scan for any remaining GameViews with hidden toolbars (our fingerprint).
            //    Normal GameViews created by Unity always have showToolbar = true.
            //    This catches edge cases where the instance ID changed or wasn't persisted.
            if (GameViewType != null && ShowToolbarProperty != null)
            {
                foreach (var obj in Resources.FindObjectsOfTypeAll(GameViewType))
                {
                    if (!IsAlive(obj)) continue;
                    var win = (EditorWindow)obj;

                    try
                    {
                        bool toolbar = (bool)ShowToolbarProperty.GetValue(win);
                        if (!toolbar)
                        {
#if UNITY_6000_5_OR_NEWER
                            if (FullscreenSettings.DebugLogging)
                                Debug.Log($"[Fullscreen] Closing orphaned toolbar-hidden GameView (id={win.GetEntityId()}).");
#else
                            if (FullscreenSettings.DebugLogging)
                                Debug.Log($"[Fullscreen] Closing orphaned toolbar-hidden GameView (id={win.GetInstanceID()}).");
#endif
                            try { win.Close(); } catch { }
                        }
                    }
                    catch { }
                }
            }

            // 5. Restore the original display index
            int displayToRestore = SessionState.GetInt(KeyOriginalDisplay, 0);
            EditorApplication.delayCall += () =>
            {
                try { setGameViewTargetDisplay?.Invoke(displayToRestore); } catch { }
            };

            // 6. Restore VSync from persisted value if available
            int savedVSync = SessionState.GetInt(KeyOriginalVSync, -1);
            if (savedVSync >= 0)
                QualitySettings.vSyncCount = savedVSync;

            // 7. Restore cursor
            Cursor.visible = true;

            // 8. Resume Scene Views
            try { ResumeAllSceneViews(); } catch { }

            // 9. Clear all persisted state
            Clear();

            if (FullscreenSettings.DebugLogging)
                Debug.Log("[Fullscreen] ForceCleanup complete.");
        }

#endregion

        #region Domain Reload Recovery

        /// <summary>
        /// Called from FullscreenGameView.RegisterCallbacks after every domain
        /// reload. If SessionState says we were in fullscreen but the caller
        /// reports the in-memory reference is gone, schedules ForceCleanup.
        /// </summary>
        /// <param name="liveInstanceIsAlive">
        /// Whether FullscreenGameView still has a valid in-memory reference.
        /// </param>
        /// <param name="cleanupAction">
        /// The parameterless cleanup action to schedule (typically a lambda
        /// that calls ForceCleanup with the right arguments).
        /// </param>
        public static void RecoverIfOrphaned(bool liveInstanceIsAlive, Action cleanupAction)
        {
            if (WasFullscreen && !liveInstanceIsAlive)
            {
                if (FullscreenSettings.DebugLogging)
                    Debug.Log("[Fullscreen] Detected orphaned fullscreen state after domain reload. Cleaning up.");

                EditorApplication.delayCall += () => cleanupAction?.Invoke();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Checks whether a UnityEngine.Object is non-null and not destroyed.
        /// </summary>
        private static bool IsAlive(UnityEngine.Object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            try { return obj != null; }
            catch { return false; }
        }

        /// <summary>
        /// Re-enables autoRepaintOnSceneChange on all open Scene Views.
        /// </summary>
        private static void ResumeAllSceneViews()
        {
            foreach (SceneView sv in SceneView.sceneViews)
            {
                sv.autoRepaintOnSceneChange = true;
                sv.Repaint();
            }
        }

        #endregion
    }
}

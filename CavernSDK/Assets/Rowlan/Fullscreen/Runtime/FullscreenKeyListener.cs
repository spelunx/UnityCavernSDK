#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Invisible MonoBehaviour that catches keybind input when the Game View has focus
    /// during Play Mode. Spawned automatically by FullscreenGameView when Play Mode starts.
    ///
    /// Reads keybinds from FullscreenKeybinds (populated by the editor at spawn time).
    /// Communicates back to the editor via instance delegates to avoid a compile-time
    /// dependency on the Editor assembly. Instance (rather than static) delegates prevent
    /// stale subscriptions from stacking when domain reload is disabled.
    /// </summary>
    public class FullscreenKeyListener : MonoBehaviour
    {
        #region Editor Callbacks

        /// <summary>
        /// Delegate invoked when the toggle or exit key is pressed.
        /// Assigned by FullscreenGameView (editor side) before this component is used.
        /// Instance (not static) to prevent stale subscriptions stacking when
        /// domain reload is disabled.
        /// </summary>
        public Action OnTogglePressed;

        /// <summary>
        /// Delegate invoked to check whether fullscreen is currently active.
        /// Assigned by FullscreenGameView (editor side) before this component is used.
        /// Instance (not static) to prevent stale subscriptions stacking when
        /// domain reload is disabled.
        /// </summary>
        public Func<bool> IsFullscreen;

        /// <summary>
        /// Delegate invoked when the reset key is pressed.
        /// Assigned by FullscreenGameView (editor side) before this component is used.
        /// Forces fullscreen exit and restores the default editor layout.
        /// </summary>
        public Action OnResetPressed;

        #endregion

        #region Cached Keybinds

        private KeyControl toggleControl;
        private KeyControl exitControl;
        private KeyControl resetControl;

        #endregion

        #region Initialization

        /// <summary>
        /// Resolves each keybind to a KeyControl once at spawn time and caches
        /// the result. Keys set to None are left as null and silently skipped
        /// during Update. Disables the component if validation fails.
        /// </summary>
        private void Awake()
        {
            if (Keyboard.current == null)
            {
                Debug.LogWarning("[Fullscreen] No keyboard device found. Key listener disabled.");
                enabled = false;
                return;
            }

            if (!ResolveAndValidateKeybinds())
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Resolves all configured keys to cached KeyControls and checks for
        /// problematic configurations. Returns false if the component should
        /// be disabled (invalid key or all keys unbound). Logs warnings for
        /// partial configurations that still allow the component to run.
        ///
        /// Called from Awake on first creation, and again from the editor
        /// (via InjectKeybinds) on each play session to pick up preference
        /// changes — especially important when domain reload is disabled
        /// and the listener survives between sessions.
        /// </summary>
        public bool ResolveAndValidateKeybinds()
        {
            Key toggleKey = FullscreenKeybinds.ToggleKey;
            Key exitKey = FullscreenKeybinds.ExitKey;
            Key resetKey = FullscreenKeybinds.ResetKey;

            // Resolve each key to a KeyControl. None → null (unbound), valid key → control.
            if (!TryResolve(toggleKey, out toggleControl) ||
                !TryResolve(exitKey, out exitControl) ||
                !TryResolve(resetKey, out resetControl))
            {
                Debug.LogWarning($"[Fullscreen] Invalid keybinds (toggle='{toggleKey}', exit='{exitKey}', reset='{resetKey}'). Key listener disabled.");
                return false;
            }

            // All keys unbound — nothing to listen for
            if (toggleControl == null && exitControl == null && resetControl == null)
            {
                Debug.LogWarning("[Fullscreen] All keybinds are set to None. Key listener disabled. " +
                                 "Configure keybinds in Edit → Preferences → Rowlan/Fullscreen.");
                return false;
            }

            // Toggle and exit both unbound — user can't control fullscreen via keyboard
            if (toggleControl == null && exitControl == null)
            {
                Debug.LogWarning("[Fullscreen] Toggle and Exit keybinds are both set to None. " +
                                 "Fullscreen can only be controlled via the menu (Tools → Rowlan → Fullscreen).");
            }

            return true;
        }

        /// <summary>
        /// Resolves a Key to a KeyControl. Returns true if the key is either
        /// None (control set to null) or a valid physical key. Returns false
        /// only for keys that fail to resolve, meaning the component should
        /// be disabled.
        /// </summary>
        private static bool TryResolve(Key key, out KeyControl control)
        {
            control = null;
            if (key == Key.None) return true;
            control = Keyboard.current[key];
            return control != null;
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Checks cached KeyControls for presses each frame and forwards
        /// them to the editor via the appropriate delegate.
        /// Null controls (unbound keys) are skipped with no overhead.
        /// </summary>
        private void Update()
        {
            if (resetControl != null && resetControl.wasPressedThisFrame)
            {
                OnResetPressed?.Invoke();
            }
            else if (toggleControl != null && toggleControl.wasPressedThisFrame)
            {
                OnTogglePressed?.Invoke();
            }
            else if (exitControl != null && exitControl.wasPressedThisFrame)
            {
                if (IsFullscreen != null && IsFullscreen())
                    OnTogglePressed?.Invoke();
            }
        }

        #endregion
    }
}
#endif


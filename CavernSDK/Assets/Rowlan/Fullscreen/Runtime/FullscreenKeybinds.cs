#if UNITY_EDITOR
using UnityEngine.InputSystem;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Shared keybind state that is written by the editor (FullscreenSettings)
    /// and read by the runtime (FullscreenKeyListener).
    ///
    /// Lives in the Runtime assembly so both sides can reference it without
    /// introducing a circular dependency. Values are injected by the editor
    /// when Play Mode starts and cached for the duration of the session.
    /// </summary>
    public static class FullscreenKeybinds
    {
        /// <summary>
        /// The key used to toggle fullscreen on and off during Play Mode.
        /// </summary>
        public static Key ToggleKey { get; set; } = Key.F11;

        /// <summary>
        /// The key used to exit fullscreen without stopping Play Mode.
        /// </summary>
        public static Key ExitKey { get; set; } = Key.Escape;

        /// <summary>
        /// The key used to force-exit fullscreen and reset the editor layout to default.
        /// Acts as a safety fallback when the editor becomes unresponsive in fullscreen.
        /// </summary>
        public static Key ResetKey { get; set; } = Key.None;
    }
}
#endif
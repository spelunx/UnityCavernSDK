using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Routes fullscreen platform calls to the correct OS-specific implementation.
    /// Falls back to a no-op with a warning on unsupported platforms.
    /// </summary>
    public static class FullscreenPlatform
    {
        /// <summary>
        /// Invokes platform-specific logic to force the popup window over the OS taskbar or dock.
        /// </summary>
        /// <param name="popup">The fullscreen popup EditorWindow.</param>
        /// <param name="pixelW">Screen width in physical pixels.</param>
        /// <param name="pixelH">Screen height in physical pixels.</param>
        public static void OnEnterFullscreen(EditorWindow popup)
        {
#if UNITY_EDITOR_WIN
            FullscreenPlatformWindows.OnEnterFullscreen(popup);
#elif UNITY_EDITOR_OSX
        FullscreenPlatformMac.OnEnterFullscreen(popup);
#else
        Debug.LogWarning("[Fullscreen] No platform-specific taskbar override for this OS. " +
                         "Popup is shown but may not cover the OS taskbar/panel.");
#endif
        }

        /// <summary>
        /// Invokes platform-specific logic to restore the OS taskbar or dock visibility.
        /// </summary>
        public static void OnExitFullscreen()
        {
#if UNITY_EDITOR_WIN
            FullscreenPlatformWindows.OnExitFullscreen();
#elif UNITY_EDITOR_OSX
        FullscreenPlatformMac.OnExitFullscreen();
#endif
        }
    }
}
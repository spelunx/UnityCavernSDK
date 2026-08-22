#if UNITY_EDITOR_WIN

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Windows-specific fullscreen logic.
    /// Uses Win32 MonitorFromWindow to detect which monitor the popup is on,
    /// then strips window borders and forces it topmost covering the full monitor.
    ///
    /// The window handle (HWND) is persisted in SessionState so that cleanup
    /// can remove the topmost flag even after a domain reload wipes static fields.
    /// </summary>
    public static class FullscreenPlatformWindows
    {
        #region Win32 Imports

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        #endregion

        #region Win32 Constants

        private const int GWL_STYLE = -16;
        private const int WS_BORDER = 0x00800000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        #endregion

        #region Private State

        private static IntPtr popupHwnd = IntPtr.Zero;

        private const string SessionKeyHwnd = "Rowlan.Fullscreen.Win.Hwnd";

        #endregion

        #region Public API

        /// <summary>
        /// Strips remaining border styles from the popup window, uses MonitorFromWindow
        /// to detect which monitor the popup is on, and forces it topmost covering the
        /// full monitor rect (including the taskbar area).
        /// Persists the HWND to SessionState for domain-reload recovery.
        /// </summary>
        /// <param name="popup">The fullscreen popup EditorWindow.</param>
        public static void OnEnterFullscreen(EditorWindow popup)
        {
            EditorApplication.delayCall += () =>
            {
                if (popup == null) return;
                popup.Focus();

                EditorApplication.delayCall += () =>
                {
                    if (popup == null) return;

                    popupHwnd = GetActiveWindow();

                    if (popupHwnd == IntPtr.Zero)
                    {
                        Debug.LogWarning("[Fullscreen/Win] Could not acquire window handle.");
                        return;
                    }

                    // Persist the HWND so we can clean up after domain reload
                    SessionState.SetInt(SessionKeyHwnd, (int)(long)popupHwnd);

                    // Strip any remaining border styles
                    int style = GetWindowLong(popupHwnd, GWL_STYLE);
                    style &= ~WS_BORDER;
                    style &= ~WS_CAPTION;
                    style &= ~WS_THICKFRAME;
                    SetWindowLong(popupHwnd, GWL_STYLE, style);

                    // Ask the OS which monitor this window is on and get its full rect
                    int targetX = 0, targetY = 0;
                    int targetW = Screen.currentResolution.width;
                    int targetH = Screen.currentResolution.height;

                    IntPtr hMonitor = MonitorFromWindow(popupHwnd, MONITOR_DEFAULTTONEAREST);
                    if (hMonitor != IntPtr.Zero)
                    {
                        MONITORINFO mi = new MONITORINFO();
                        mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

                        if (GetMonitorInfo(hMonitor, ref mi))
                        {
                            targetX = mi.rcMonitor.left;
                            targetY = mi.rcMonitor.top;
                            targetW = mi.rcMonitor.right - mi.rcMonitor.left;
                            targetH = mi.rcMonitor.bottom - mi.rcMonitor.top;

                            if (FullscreenSettings.DebugLogging)
                                Debug.Log($"[Fullscreen/Win] MonitorFromWindow: " +
                                          $"origin=({targetX},{targetY}), size={targetW}x{targetH}");
                        }
                    }

                    // Force topmost and cover the entire monitor including the taskbar
                    SetWindowPos(popupHwnd, HWND_TOPMOST,
                        targetX, targetY, targetW, targetH,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                };
            };
        }

        /// <summary>
        /// Removes the topmost flag from the popup window so it no longer covers the taskbar.
        /// Checks both the in-memory handle and the persisted SessionState handle to ensure
        /// cleanup works even after a domain reload wiped the static field.
        /// Safe to call multiple times — IsWindow validates the handle before use.
        /// </summary>
        public static void OnExitFullscreen()
        {
            // Try the in-memory handle first
            IntPtr hwndToRestore = popupHwnd;

            // If the in-memory handle is gone (domain reload), recover from SessionState
            if (hwndToRestore == IntPtr.Zero)
            {
                int persisted = SessionState.GetInt(SessionKeyHwnd, 0);
                if (persisted != 0)
                    hwndToRestore = new IntPtr(persisted);
            }

            if (hwndToRestore != IntPtr.Zero)
            {
                // Validate the handle is still a real window before calling SetWindowPos.
                // After domain reload the window may have been destroyed by Unity,
                // in which case the handle is stale and we should skip the call.
                if (IsWindow(hwndToRestore))
                {
                    SetWindowPos(hwndToRestore, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                }

                popupHwnd = IntPtr.Zero;
                SessionState.SetInt(SessionKeyHwnd, 0);
            }
        }

        #endregion
    }
}
#endif

#if UNITY_EDITOR_OSX

using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// macOS-specific fullscreen logic.
    /// Uses the Objective-C runtime to detect which NSScreen the popup is on,
    /// resize it to fill that screen, raise it above the Dock and menu bar,
    /// and hide them via NSApplication presentation options.
    ///
    /// On exit, presentation options are always reset to default even if the
    /// NSWindow pointer was lost to domain reload — hiding the Dock/menubar is
    /// an application-level setting, not per-window.
    /// </summary>
    public static class FullscreenPlatformMac
    {
        #region Objective-C Runtime Imports

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        private static extern IntPtr objc_getClass(string className);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_void_long(IntPtr receiver, IntPtr selector, long arg);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern long objc_msgSend_long(IntPtr receiver, IntPtr selector);

        // NSRect is a CGRect: { CGPoint origin, CGSize size } — 4 doubles on 64-bit
        [StructLayout(LayoutKind.Sequential)]
        private struct NSRect
        {
            public double x, y, width, height;
        }

        // objc_msgSend returning NSRect (CGRect). On x86_64 macOS, structs > 16 bytes
        // are returned via objc_msgSend_stret, but CGRect is exactly 32 bytes so we
        // need stret on x86_64. On ARM64, all structs are returned in registers via
        // regular objc_msgSend. We provide both and call the correct one at runtime.
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern NSRect objc_msgSend_NSRect(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
        private static extern void objc_msgSend_stret_NSRect(out NSRect result, IntPtr receiver, IntPtr selector);

        // setFrame:display: — void, takes NSRect + BOOL
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_void_NSRect_bool(IntPtr receiver, IntPtr selector,
            NSRect frame, bool display);

        #endregion

        #region Constants

        private const long NSScreenSaverWindowLevel = 1000;

        // NSApplicationPresentationOptions flags
        private const long PresentationHideDock = 1 << 1;
        private const long PresentationHideMenuBar = 1 << 3;
        private const long PresentationDefault = 0;

        private const long CollectionBehaviorStationary = 1 << 4;

        #endregion

        #region Private State

        private static IntPtr nsWindow = IntPtr.Zero;
        private static long originalWindowLevel;

        #endregion

        #region Helpers

        /// <summary>
        /// Reads an NSRect from an Objective-C message send, handling the x86_64 vs ARM64
        /// ABI difference (stret vs register return).
        /// </summary>
        private static NSRect GetNSRect(IntPtr receiver, IntPtr selector)
        {
#if UNITY_EDITOR_OSX
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                return objc_msgSend_NSRect(receiver, selector);
            }
            else
            {
                objc_msgSend_stret_NSRect(out NSRect result, receiver, selector);
                return result;
            }
#else
            return objc_msgSend_NSRect(receiver, selector);
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Acquires the key NSWindow, detects which NSScreen it is on,
        /// resizes it to fill that screen, raises it above the Dock and menu bar,
        /// and sets presentation options to hide them entirely.
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

                    try
                    {
                        IntPtr nsApp = objc_getClass("NSApplication");
                        IntPtr sharedApp = objc_msgSend_IntPtr(nsApp, sel_registerName("sharedApplication"));
                        nsWindow = objc_msgSend_IntPtr(sharedApp, sel_registerName("keyWindow"));

                        if (nsWindow == IntPtr.Zero)
                        {
                            Debug.LogWarning("[Fullscreen/Mac] Could not acquire NSWindow.");
                            return;
                        }

                        // Save original level for restoration
                        originalWindowLevel = objc_msgSend_long(nsWindow, sel_registerName("level"));

                        // Get the NSScreen this window is on and read its full frame
                        IntPtr nsScreen = objc_msgSend_IntPtr(nsWindow, sel_registerName("screen"));
                        if (nsScreen != IntPtr.Zero)
                        {
                            NSRect screenFrame = GetNSRect(nsScreen, sel_registerName("frame"));

                            // Resize window to fill the entire screen
                            objc_msgSend_void_NSRect_bool(nsWindow, sel_registerName("setFrame:display:"),
                                screenFrame, true);

                            if (FullscreenSettings.DebugLogging)
                                Debug.Log($"[Fullscreen/Mac] Screen frame: " +
                                          $"origin=({screenFrame.x},{screenFrame.y}), " +
                                          $"size={screenFrame.width}x{screenFrame.height}");
                        }

                        // Raise window above Dock and menu bar
                        objc_msgSend_void_long(nsWindow, sel_registerName("setLevel:"),
                            NSScreenSaverWindowLevel);

                        // Keep window stationary during Exposé and Spaces
                        objc_msgSend_void_long(nsWindow, sel_registerName("setCollectionBehavior:"),
                            CollectionBehaviorStationary);

                        // Hide Dock and menu bar
                        objc_msgSend_void_long(sharedApp, sel_registerName("setPresentationOptions:"),
                            PresentationHideDock | PresentationHideMenuBar);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Fullscreen/Mac] Platform call failed: {e.Message}");
                    }
                };
            };
        }

        /// <summary>
        /// Restores the original NSWindow level and re-shows the Dock and menu bar
        /// by resetting NSApplication presentation options to default.
        ///
        /// The presentation options reset (Dock + menu bar) is always performed
        /// even if the nsWindow pointer was lost to domain reload, because hiding
        /// the Dock/menubar is an application-level setting on NSApplication, not
        /// tied to a specific window handle.
        /// </summary>
        public static void OnExitFullscreen()
        {
            try
            {
                // Restore window level if we still have the pointer
                if (nsWindow != IntPtr.Zero)
                {
                    objc_msgSend_void_long(nsWindow, sel_registerName("setLevel:"), originalWindowLevel);
                    nsWindow = IntPtr.Zero;
                }

                // Always restore Dock and menu bar — this is safe to call
                // unconditionally because it's an application-level setting.
                // After domain reload, nsWindow is gone but the Dock/menubar
                // are still hidden. This ensures they come back.
                IntPtr nsApp = objc_getClass("NSApplication");
                IntPtr sharedApp = objc_msgSend_IntPtr(nsApp, sel_registerName("sharedApplication"));
                objc_msgSend_void_long(sharedApp, sel_registerName("setPresentationOptions:"),
                    PresentationDefault);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Fullscreen/Mac] Restore failed: {e.Message}");
            }
        }

        #endregion
    }
}
#endif

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Persistent editor preferences for the Fullscreen Game View feature.
    /// Stores keybind assignments and the auto-fullscreen toggle via EditorPrefs.
    /// Provides a SettingsProvider for the Unity Preferences window under "Rowlan/Fullscreen".
    /// </summary>
    public static class FullscreenSettings
    {
        #region Pref Keys

        private const string Prefix = "Rowlan.Fullscreen.";
        private const string EnabledPref = Prefix + "Enabled";
        private const string ToggleKeyPref = Prefix + "ToggleKey";
        private const string ExitKeyPref = Prefix + "ExitKey";
        private const string ResetKeyPref = Prefix + "ResetKey";
        private const string AutoFullscreenPref = Prefix + "AutoFullscreen";
        private const string VSyncPref = Prefix + "VSync";
        private const string HideCursorPref = Prefix + "HideCursor";
        private const string DebugLoggingPref = Prefix + "DebugLogging";

        #endregion

        #region Defaults

        private const bool DefaultEnabled = true;
        private const Key DefaultToggleKey = Key.F11;
        private const Key DefaultExitKey = Key.Escape;
        private const Key DefaultResetKey = Key.None;
        private const bool DefaultAutoFullscreen = false;
        private const bool DefaultVSync = true;
        private const bool DefaultHideCursor = true;
        private const bool DefaultDebugLogging = true;

        #endregion

        #region Properties

        /// <summary>
        /// Master switch for the entire Fullscreen feature. When disabled,
        /// keybinds are not registered, auto-fullscreen is skipped, and
        /// the key listener is not spawned during Play Mode.
        /// </summary>
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPref, DefaultEnabled);
            set => EditorPrefs.SetBool(EnabledPref, value);
        }

        /// <summary>
        /// The key used to toggle fullscreen on and off during Play Mode.
        /// </summary>
        public static Key ToggleKey
        {
            get => (Key)EditorPrefs.GetInt(ToggleKeyPref, (int)DefaultToggleKey);
            set => EditorPrefs.SetInt(ToggleKeyPref, (int)value);
        }

        /// <summary>
        /// The key used to exit fullscreen without stopping Play Mode.
        /// </summary>
        public static Key ExitKey
        {
            get => (Key)EditorPrefs.GetInt(ExitKeyPref, (int)DefaultExitKey);
            set => EditorPrefs.SetInt(ExitKeyPref, (int)value);
        }

        /// <summary>
        /// The key used to force-exit fullscreen and reset the editor layout to default.
        /// Acts as a safety fallback when the editor becomes unresponsive in fullscreen.
        /// </summary>
        public static Key ResetKey
        {
            get => (Key)EditorPrefs.GetInt(ResetKeyPref, (int)DefaultResetKey);
            set => EditorPrefs.SetInt(ResetKeyPref, (int)value);
        }

        /// <summary>
        /// Whether fullscreen should activate automatically when entering Play Mode.
        /// </summary>
        public static bool AutoFullscreen
        {
            get => EditorPrefs.GetBool(AutoFullscreenPref, DefaultAutoFullscreen);
            set => EditorPrefs.SetBool(AutoFullscreenPref, value);
        }

        /// <summary>
        /// Whether VSync should be enabled during fullscreen Play Mode.
        /// When disabled, the frame rate is uncapped (useful for performance testing).
        /// The original VSync setting is restored when exiting fullscreen.
        /// </summary>
        public static bool VSync
        {
            get => EditorPrefs.GetBool(VSyncPref, DefaultVSync);
            set => EditorPrefs.SetBool(VSyncPref, value);
        }

        /// <summary>
        /// Whether the mouse cursor should be hidden during fullscreen Play Mode.
        /// The original cursor visibility is restored when exiting fullscreen.
        /// </summary>
        public static bool HideCursor
        {
            get => EditorPrefs.GetBool(HideCursorPref, DefaultHideCursor);
            set => EditorPrefs.SetBool(HideCursorPref, value);
        }

        /// <summary>
        /// Whether debug log messages should be printed to the console when
        /// entering and exiting fullscreen. Disable to reduce console noise.
        /// </summary>
        public static bool DebugLogging
        {
            get => EditorPrefs.GetBool(DebugLoggingPref, DefaultDebugLogging);
            set => EditorPrefs.SetBool(DebugLoggingPref, value);
        }

        #endregion

        #region Settings Provider

        /// <summary>
        /// Registers the preferences panel under Edit → Preferences → Rowlan/Fullscreen.
        /// </summary>
        /// <returns>A SettingsProvider instance for the Unity Preferences window.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Rowlan/Fullscreen", SettingsScope.User)
            {
                label = "Fullscreen",
                keywords = new HashSet<string> { "fullscreen", "game view", "play mode" },
                guiHandler = OnGUI
            };
        }

        /// <summary>
        /// Draws the preferences GUI with keybind fields, auto-fullscreen toggle,
        /// and a reset button.
        /// </summary>
        /// <param name="searchContext">The current search filter from the Preferences window.</param>
        private static void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 200;
            EditorGUILayout.Space(10);

            Enabled = EditorGUILayout.Toggle(
                new GUIContent("Enabled", "Master switch for the Fullscreen feature. When disabled, all fullscreen functionality is turned off."),
                Enabled);

            EditorGUILayout.Space(10);

            // Grey out all settings when the feature is disabled
            EditorGUI.BeginDisabledGroup(!Enabled);

            EditorGUILayout.LabelField("Keybinds", EditorStyles.boldLabel);

            ToggleKey = (Key)EditorGUILayout.EnumPopup("Toggle Fullscreen", ToggleKey);
            ExitKey = (Key)EditorGUILayout.EnumPopup("Exit Fullscreen", ExitKey);
            ResetKey = (Key)EditorGUILayout.EnumPopup(
                new GUIContent("Reset Layout", "Force-exit fullscreen and restore the default editor layout. Safety fallback if the editor becomes stuck."),
                ResetKey);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);

            AutoFullscreen = EditorGUILayout.Toggle("Fullscreen On Play", AutoFullscreen);
            VSync = EditorGUILayout.Toggle(
                new GUIContent("VSync", "Enable VSync during fullscreen. Disable to uncap frame rate for performance testing."),
                VSync);
            HideCursor = EditorGUILayout.Toggle(
                new GUIContent("Hide Cursor", "Hide the mouse cursor during fullscreen. Useful for gamepad-only or presentation scenarios."),
                HideCursor);
            DebugLogging = EditorGUILayout.Toggle(
                new GUIContent("Debug Logging", "Print log messages to the console when entering and exiting fullscreen."),
                DebugLogging);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(20);

            if (UnityEngine.GUILayout.Button("Reset to Defaults", UnityEngine.GUILayout.Width(150)))
            {
                ResetToDefaults();
            }
        }

        /// <summary>
        /// Resets all preferences to their default values.
        /// </summary>
        private static void ResetToDefaults()
        {
            Enabled = DefaultEnabled;
            ToggleKey = DefaultToggleKey;
            ExitKey = DefaultExitKey;
            ResetKey = DefaultResetKey;
            AutoFullscreen = DefaultAutoFullscreen;
            VSync = DefaultVSync;
            HideCursor = DefaultHideCursor;
            DebugLogging = DefaultDebugLogging;
        }

        #endregion
    }
}
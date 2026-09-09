using UnityEditor;

namespace Spelunx.Fullscreen
{
    [InitializeOnLoad]
    public class AutoActivateFullscreenPreview
    {
        public const string IsFullscreenPreviewEnabledKey = "Fullscreen On Play";

        static AutoActivateFullscreenPreview()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            int whichDisplay = EditorPrefs.GetInt(IsFullscreenPreviewEnabledKey, -1);
            // if (EditorPrefs.GetBool(IsFullscreenPreviewEnabledKey, false))
            if (whichDisplay != -1)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    FullscreenGameView.SetFullscreen(true, whichDisplay);
                }
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    FullscreenGameView.SetFullscreen(false);
                }
            }
        }
    }
}
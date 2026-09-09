using UnityEditor;


namespace Spelunx.Fullscreen
{
    public class ToggleFullscreenGameView : Editor
    {
        public const string MenuPathPrefix = "CAVERN";
        // public const string IsEnabledBoolPath = AutoActivateFullscreenPreview.IsFullscreenPreviewEnabledKey;
        public const string IsEnabledIntPath = AutoActivateFullscreenPreview.IsFullscreenPreviewEnabledKey;
        private const string FullMenuPath = MenuPathPrefix + "/" + IsEnabledIntPath;
        private const string MainDisplayPath = FullMenuPath + "/Display 1";
        private const string SecondaryDisplayPath = FullMenuPath + "/Display 2";
        private const string OffPath = FullMenuPath + "/Disabled";


        // [MenuItem(FullMenuPath)]
        // public static void ToggleIsEnabled()
        // {
        //     EditorPrefs.SetBool(IsEnabledBoolPath, !EditorPrefs.GetBool(IsEnabledBoolPath, false));
        //     Menu.SetChecked(FullMenuPath, EditorPrefs.GetBool(IsEnabledBoolPath, false));
        // }

        // [MenuItem(FullMenuPath, true)]
        // public static bool ToggleIsEnabledValidate()
        // {
        //     Menu.SetChecked(FullMenuPath, EditorPrefs.GetBool(IsEnabledBoolPath, false));
        //     return true;
        // }

        [MenuItem(MainDisplayPath)]
        public static void SetMainDisplay()
        {
            EditorPrefs.SetInt(IsEnabledIntPath, 0);
            Menu.SetChecked(MainDisplayPath, EditorPrefs.GetInt(IsEnabledIntPath, -1) == 0);
        }

        [MenuItem(SecondaryDisplayPath)]
        public static void SetSecondaryDisplay()
        {
            EditorPrefs.SetInt(IsEnabledIntPath, 1);
            Menu.SetChecked(SecondaryDisplayPath, EditorPrefs.GetInt(IsEnabledIntPath, -1) == 1);
        }

        [MenuItem(OffPath)]
        public static void SetDisabled()
        {
            EditorPrefs.SetInt(IsEnabledIntPath, -1);
            Menu.SetChecked(OffPath, EditorPrefs.GetInt(IsEnabledIntPath, -1) == -1);
        }

        [MenuItem(MainDisplayPath, true)]
        public static bool SetMainDisplayValidate()
        {
            Menu.SetChecked(MainDisplayPath, EditorPrefs.GetInt(IsEnabledIntPath, -1) == 0);
            return true;
        }


        [MenuItem(SecondaryDisplayPath, true)]
        public static bool SetSecondaryDisplayValidate()
        {
            Menu.SetChecked(SecondaryDisplayPath, EditorPrefs.GetInt(IsEnabledIntPath, -1) == 1);
            return true;
        }

        [MenuItem(OffPath, true)]
        public static bool SetDisabledValidate()
        {
            Menu.SetChecked(OffPath, EditorPrefs.GetInt(IsEnabledIntPath, -1) == -1);
            return true;
        }
    }
}
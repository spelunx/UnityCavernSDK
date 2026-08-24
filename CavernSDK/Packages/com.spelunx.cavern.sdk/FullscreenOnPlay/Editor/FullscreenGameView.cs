#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spelunx.Fullscreen
{
    // Forked from https://github.com/JorGra/JG-UnityEditor-GameViewFullscreen
    /// <summary>
    /// When enabled or activated, tries to run the game view full screen by rendering the game view to a new window that is the size of the monitor, and hiding the toolbar.
    /// Does not use the same API as a native fullscreen build, so performance will not be the same as "exclusive" fullscreen
    /// </summary>
    public static class FullscreenGameView
    {
        static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        private static readonly Type HostViewType = Type.GetType("UnityEditor.HostView,UnityEditor");
        private static readonly Type ContainerWindowType = Type.GetType("UnityEditor.ContainerWindow,UnityEditor");
        private static readonly PropertyInfo ShowToolbarProperty = GameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);
        // hack to prevent double-rendering while in fullscreen
        private readonly static int DISPLAY_0 = 1; // target gameview display
        private readonly static int DISPLAY_7 = 7; // display gameview doesn't render to
        static EditorWindow instance;


        private static PropertyInfo FindProperty(Type type, string propertyName)
        {
            return type?.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static MethodInfo FindMethod(Type type, string methodName, params Type[] args)
        {
            return args.Length == 0
                ? type?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                : type?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, args, null);
        }
        private static EditorWindow GetMainGameView()
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
            {
                Debug.LogError("Unable to find the UnityEditor.GameView type.");
                return null;
            }
            return EditorWindow.GetWindow(gameViewType);
        }

        private static void SetGameViewTargetDisplay(int displayIndex)
        {
            EditorWindow gameView = GetMainGameView();
            if (gameView == null)
                return;

            FindMethod(gameView.GetType(), "SetTargetDisplay", typeof(int))?.Invoke(gameView, new object[] { displayIndex });
        }

        public static void EnterFullscreen(bool secondaryMonitor = true)
        {
            if (GameViewType == null)
            {
                Debug.LogError("GameView type not found.");
                return;
            }

            if (ShowToolbarProperty == null)
            {
                Debug.LogWarning("GameView.showToolbar property not found.");
            }

            if (instance != null)
            {
                instance.Close();
                instance = null;
                SetGameViewTargetDisplay(DISPLAY_0);
            }
            else
            {
                SetGameViewTargetDisplay(DISPLAY_7);
                instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);
                var containerWindow = ScriptableObject.CreateInstance(ContainerWindowType);
                var hostView = ScriptableObject.CreateInstance(HostViewType);
                FindMethod(instance.GetType(), "SetTargetDisplay", typeof(int))?.Invoke(instance, new object[] { DISPLAY_0 });

                ShowToolbarProperty?.SetValue(instance, false);
                List<DisplayInfo> screens = new();
                Screen.GetDisplayLayout(screens);
                // foreach (var thing in screens)
                // {
                //     Debug.Log($"Display width: {thing.width}\theight: {thing.height}\t dpi: {thing.physicalDpi}");
                // }
                Vector2 position;
                Vector2 resolution;
                if (secondaryMonitor)
                {
                    position = new(-screens[1].width, 0);
                    resolution = new(screens[1].width, screens[1].height);// / EditorGUIUtility.pixelsPerPoint;
                }
                else
                {
                    position = Vector2.zero;
                    resolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) / EditorGUIUtility.pixelsPerPoint;
                }

                FindProperty(HostViewType, "actualView")?.SetValue(hostView, instance);

                var fullscreenRect = new Rect(position, resolution);
                FindProperty(ContainerWindowType, "position")?.SetValue(containerWindow, fullscreenRect);
                FindProperty(ContainerWindowType, "rootView")?.SetValue(containerWindow, hostView);

                var showMethod = ContainerWindowType?.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "Show" && m.GetParameters().Length is 3 or 4 or 5);

                showMethod?.Invoke(containerWindow, new object[] { 3, false, true, true, 0 }.Take(showMethod.GetParameters().Length).ToArray());

                FindProperty(ContainerWindowType, "m_ShowMode")?.SetValue(containerWindow, 1);
                FindProperty(ContainerWindowType, "m_DontSaveToLayout")?.SetValue(containerWindow, true);

                FindMethod(ContainerWindowType, "SetMinMaxSizes", typeof(Vector2), typeof(Vector2))?.Invoke(containerWindow, new object[] { fullscreenRect.size, fullscreenRect.size });
            }
        }

        public static void ExitFullscreen()
        {
            if (instance != null)
            {
                // To prevent error with closing hostview while game is running, wait for the next editor frame to actually close the instance
                EditorApplication.delayCall += CloseFullscreenAfterDelay;
            }
        }


        public static void SetFullscreen(bool fullscreen)
        {
            if (instance == null && fullscreen)
            {
                EnterFullscreen();
            }
            else if (instance != null && !fullscreen)
            {
                ExitFullscreen();
            }
        }

        private static void CloseFullscreenAfterDelay()
        {
            instance.Close();
            SetGameViewTargetDisplay(DISPLAY_0);
            EditorApplication.delayCall -= CloseFullscreenAfterDelay;
        }

    }
}
#endif
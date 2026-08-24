using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Spelunx
{
    [AddComponentMenu("Cavern/Features/Cavern Debug Keys")]
    [DisallowMultipleComponent]
    public class CavernDebugKeysFeature : CavernFeature, ICavernDebugKeysFeature
    {
        public override string Name => "Cavern Debug Keys Feature";

        [Header("Input Actions")]
        [SerializeField, Tooltip("Quits the game or play mode")]
        private InputAction quit = new("Quit", InputActionType.Value, "<Keyboard>/Escape");
        [SerializeField, Tooltip("Opens the help debug window")]
        private InputAction help = new("Help", InputActionType.Value, "<Keyboard>/h");
        [SerializeField, Tooltip("Swaps the eyes on the stereoscopic glasses")]
        private InputAction swapEyes = new("Swap Eyes", InputActionType.Value, "<Keyboard>/e");
        [SerializeField, Tooltip("Toggles rendering between stereo and mono")]
        private InputAction stereoMonoToggle = new("Stereo/Mono Toggle", InputActionType.Value, "<Keyboard>/t");
        [SerializeField, Tooltip("Toggles stereo convergence rendering")]
        private InputAction convergenceToggle = new("Convergence Toggle", InputActionType.Value, "<Keyboard>/c");
        [SerializeField, Tooltip("Toggles muting all sounds")]
        private InputAction muteToggle = new("Mute Toggle", InputActionType.Value, "<Keyboard>/m");
        [SerializeField, Tooltip("Hides the mouse when it doesn't move for a few seconds")]
        private InputAction mouseMove = new("Mouse Move", InputActionType.Value, "<Mouse>/delta");
        [SerializeField, Tooltip("Increase the interpupillary distance")]
        private InputAction increaseIPD = new("Increase IPD", InputActionType.Value, "<Keyboard>/rightArrow");
        [SerializeField, Tooltip("Decreases the interpupillary distance")]
        private InputAction decreaseIPD = new("Decrease IPD", InputActionType.Value, "<Keyboard>/leftArrow");
        [SerializeField, Tooltip("Increase the camera height")]
        private InputAction increaseCameraHeight = new("Increase Camera Height", InputActionType.Value, "<Keyboard>/upArrow");
        [SerializeField, Tooltip("Decreases the camera height")]
        private InputAction decreaseCameraHeight = new("Decrease Camera Height", InputActionType.Value, "<Keyboard>/downArrow");

        [Header("Settings")]
        [SerializeField, Range(0, 0.01f), Tooltip("Amount to adjust interpupillary distance for stereo rendering")]
        private float IPD_CHANGE = 0.001f;
        [SerializeField, Range(0, 0.5f), Tooltip("Amount to adjust camera height")]
        private float CAMERA_HEIGHT_CHANGE = 0.0254f; // one inch
        [SerializeField, Tooltip("The Head object that gets repositioned")]
        private Transform headTrackingCamera;

        [SerializeField, Tooltip("Skin for the GUI")]
        private GUISkin guiSkin;

        // used to render the help debug window
        private List<string> helpKeys = new();
        private List<string> helpDescriptions = new();
        private UnityAction extraGUICalls;

        private enum HelpDisplay
        {
            Off = 0,
            PC = 1,
            CAVERN = 2
        }
        private HelpDisplay showHelp = HelpDisplay.Off;

        // running average of framerates
        private readonly int[] framerates = new int[100];
        private int framerateIndex = 0;

        public List<(string Key, string Description)> KeyDescriptions()
        {
            return new(){
                (quit.GetBindingDisplayString(), "Quit the game or exit play mode"),
                (help.GetBindingDisplayString(), "Open this help window"),
                (swapEyes.GetBindingDisplayString(), "Swap the eyes on the stereoscopic glasses"),
                (stereoMonoToggle.GetBindingDisplayString(), "Toggle rendering between stereo and mono"),
                (convergenceToggle.GetBindingDisplayString(), "Toggle extra stereo convergence"),
                (muteToggle.GetBindingDisplayString(), "Mute all sounds"),
                (increaseIPD.GetBindingDisplayString(), "Increase IPD"),
                (decreaseIPD.GetBindingDisplayString(), "Decrease IPD"),
                (increaseCameraHeight.GetBindingDisplayString(), "Increase Camera Height"),
                (decreaseCameraHeight.GetBindingDisplayString(), "Decrease Camera Height")
            };
        }

        public void DoExtraGUI()
        {
            framerates[framerateIndex] = (int)(1 / Time.unscaledDeltaTime);
            framerateIndex = (framerateIndex + 1) % framerates.Length;
            int currentFramerate = (int)framerates.Average();
            GUILayout.Label($"Framerate: {currentFramerate} fps");
            float ipd = cavernSetup.IPD * 1000;
            GUILayout.Label($"Head height: {headTrackingCamera.localPosition.y} meters\t\tIPD: {ipd:F1} mm");
            GUILayout.Label($"Convergence: {(cavernSetup.Convergence ? "On" : "Off")}");
        }

        // enable the input actions on play mode start
        public void OnEnable()
        {
            quit.Enable();
            help.Enable();
            swapEyes.Enable();
            stereoMonoToggle.Enable();
            convergenceToggle.Enable();
            muteToggle.Enable();
            mouseMove.Enable();
            increaseIPD.Enable();
            decreaseIPD.Enable();
            increaseCameraHeight.Enable();
            decreaseCameraHeight.Enable();
        }


        // disable the input actions on play mode stop
        public void OnDisable()
        {
            quit.Disable();
            help.Disable();
            swapEyes.Disable();
            stereoMonoToggle.Disable();
            convergenceToggle.Disable();
            muteToggle.Disable();
            mouseMove.Disable();
            increaseIPD.Disable();
            decreaseIPD.Disable();
            increaseCameraHeight.Disable();
            decreaseCameraHeight.Disable();
        }

        // bind the proper callbacks to each action.performed
        // using the saved key managers
        // This must happen in play mode, not in edit mode, or it won't work.
        public void Awake()
        {
            quit.performed += QuitAction;
            help.performed += HelpAction;
            swapEyes.performed += SwapEyesAction;
            stereoMonoToggle.performed += MonoStereoAction;
            convergenceToggle.performed += ConvergenceAction;
            muteToggle.performed += MuteToggleAction;
            mouseMove.performed += OnMouseMove;
            increaseIPD.performed += IncreaseIPDAction;
            decreaseIPD.performed += DecreaseIPDAction;
            increaseCameraHeight.performed += IncreaseCameraHeightAction;
            decreaseCameraHeight.performed += DecreaseCameraHeightAction;
        }

        public void Start()
        {
            // Start the coroutine for hiding the mouse if it doesn't move
            hideMouseCoroutine = HideMouse();
            cavernSetup.StartCoroutine(hideMouseCoroutine);
            // find all help descriptions and add them to list
            foreach (ICavernDebugKeysFeature manager in GetComponents<ICavernDebugKeysFeature>())
            {
                foreach ((string Key, string Description) d in manager.KeyDescriptions())
                {
                    helpKeys.Add(d.Key);
                    helpDescriptions.Add(d.Description);
                }
                extraGUICalls += manager.DoExtraGUI;
            }
        }

        public void QuitAction(InputAction.CallbackContext ctx)
        {
#if UNITY_EDITOR
            // UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        void SwapEyesAction(InputAction.CallbackContext ctx)
        {
            cavernSetup.SwapEyes = !cavernSetup.SwapEyes;
        }

        void MonoStereoAction(InputAction.CallbackContext ctx)
        {
            switch (cavernSetup.GetStereoscopicMode())
            {
                case CavernSetup.StereoscopicMode.Mono:
                    cavernSetup.SetStereoscopicMode(CavernSetup.StereoscopicMode.Stereo);
                    break;
                case CavernSetup.StereoscopicMode.Stereo:
                    cavernSetup.SetStereoscopicMode(CavernSetup.StereoscopicMode.Mono);
                    break;
            }

        }

        private void ConvergenceAction(InputAction.CallbackContext context)
        {
            cavernSetup.Convergence = !cavernSetup.Convergence;
        }

        //  void HeadtrackingToggleAction(InputAction.CallbackContext ctx){

        // }

        void MuteToggleAction(InputAction.CallbackContext ctx)
        {
            AudioListener l = cavernSetup.GetEar().GetComponent<AudioListener>();
            l.enabled = !l.enabled;
        }

        void HelpAction(InputAction.CallbackContext ctx)
        {
            // cycle through the different help display types
            switch (showHelp)
            {
                case HelpDisplay.Off:
                    showHelp = HelpDisplay.PC;
                    break;
                case HelpDisplay.PC:
                    showHelp = HelpDisplay.Off;
                    break;
                case HelpDisplay.CAVERN:
                    showHelp = HelpDisplay.Off;
                    break;
            }
            // showHelp = (HelpDisplay)(((int)showHelp + 1) % typeof(HelpDisplay).GetEnumValues().Length);
        }

        void IncreaseIPDAction(InputAction.CallbackContext ctx)
        {
            cavernSetup.IPD += IPD_CHANGE;
        }

        void DecreaseIPDAction(InputAction.CallbackContext ctx)
        {
            cavernSetup.IPD -= IPD_CHANGE;
        }

        void IncreaseCameraHeightAction(InputAction.CallbackContext ctx)
        {
            headTrackingCamera.localPosition = new(headTrackingCamera.localPosition.x, headTrackingCamera.localPosition.y + CAMERA_HEIGHT_CHANGE, headTrackingCamera.localPosition.z);
        }

        void DecreaseCameraHeightAction(InputAction.CallbackContext ctx)
        {
            headTrackingCamera.localPosition = new(headTrackingCamera.localPosition.x, headTrackingCamera.localPosition.y - CAMERA_HEIGHT_CHANGE, headTrackingCamera.localPosition.z);
        }


        #region Cursor Hiding

        // We hide the cursor when it's not moving.
        // We use coroutines instead of an update loop because most of the time the mouse isn't going to be moving
        // And this saves on compute cost in that case (although it's slightly worse if the mouse is moving often)
        IEnumerator hideMouseCoroutine = null;
        IEnumerator HideMouse()
        {
            yield return new WaitForSeconds(3); // after three seconds, hide the mouse
            Cursor.visible = false;
        }

        void OnMouseMove(InputAction.CallbackContext context)
        {
            cavernSetup.StopCoroutine(hideMouseCoroutine);

            Cursor.visible = true;
            hideMouseCoroutine = HideMouse();
            cavernSetup.StartCoroutine(hideMouseCoroutine);
        }
        #endregion

        #region Debug GUI
        void OnGUI()
        {
            switch (showHelp)
            {
                case HelpDisplay.Off:
                    return;
                case HelpDisplay.PC:
                    PCGui();
                    break;
                case HelpDisplay.CAVERN:

                    break;
            }

        }

        void PCGui()
        {
            GUI.skin = guiSkin;
            GUILayout.BeginArea(new Rect(40, 40, 1000, 1000), GUI.skin.box);
            // GUILayout.Box("Debug Info");
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            foreach (string key in helpKeys)
            {
                GUILayout.Label(key);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            foreach (string description in helpDescriptions)
            {
                GUILayout.Label(description);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            extraGUICalls.Invoke();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void CAVERNGui()
        {

        }

        #endregion
    }
}

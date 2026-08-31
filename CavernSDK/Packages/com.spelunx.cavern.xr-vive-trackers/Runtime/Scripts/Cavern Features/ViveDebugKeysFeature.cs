using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Spelunx.XR.Vive
{
    /// <summary>
    /// Helpful debug information (both keyboard shortcuts and GUI) for Vive Trackers.
    /// This also adds the ability to calibrate the rotation of all active Vive Trackers.
    /// </summary>
    [DisallowMultipleComponent]
    public class ViveDebugKeysFeature : MonoBehaviour, ICavernDebugKeysFeature
    {
        [Header("Input Actions")]
        [SerializeField, Tooltip("Calibrate the rotations of all vive trackers. Hold them upright in the center of the CAVERN and pointed towards the center of the screen.")]
        private InputAction calibrate = new("Calibrate", InputActionType.Value, "<Keyboard>/v");

        // display the number of vive trackers in the debug GUI
        private int numViveTrackers = 0;
        private int numIndexControllers = 0;
        private readonly List<string> trackerRoles = new();
        private readonly List<string> controllerRoles = new();
        public List<(string Key, string Description)> KeyDescriptions()
        {
            return new(){
                (calibrate.GetBindingDisplayString(), "Calibrate the rotation of all vive trackers."),
            };
        }

        // Render information about the currently bound Vive Trackers in the Debug UI
        public void DoExtraGUI()
        {
            if (numViveTrackers > 0)
            {
                GUILayout.Label($"Vive Trackers: {numViveTrackers}");
                GUILayout.Label($"Tracker roles: {string.Join(", ", trackerRoles)}");
            }
            if (numIndexControllers > 0)
            {
                GUILayout.Label($"Index Controllers: {numIndexControllers}");
                GUILayout.Label($"Controller roles: {string.Join(", ", controllerRoles)}");
            }
        }

        // enable the input actions on play mode start
        void OnEnable()
        {
            calibrate.Enable();
        }


        // disable the input actions on play mode stop
        void OnDisable()
        {
            calibrate.Disable();
        }

#if UNITY_6000_5_OR_NEWER

        // bind the proper callbacks to each action.performed
        // using the saved key managers
        // This must happen in play mode, not in edit mode, or it won't work.
        void Awake()
        {
            calibrate.performed += CalibrateAction;

            // add the vive tracker info to the GUI
            foreach (ViveTracker tracker in FindObjectsByType<ViveTracker>())
            {
                numViveTrackers++;
                ViveTracker.TrackerRole binding = tracker.binding;
                trackerRoles.Add(binding.GetReadableName());
            }
            foreach (IndexController controller in FindObjectsByType<IndexController>())
            {
                numIndexControllers++;
                IndexController.Role role = controller.role;
                controllerRoles.Add(role.GetReadableName());
            }
        }

        void CalibrateAction(InputAction.CallbackContext ctx)
        {
            foreach (ViveTracker tracker in FindObjectsByType<ViveTracker>())
            {
                tracker.Calibrate();
            }
        }
#elif UNITY_6000_3
        void Awake()
        {
            calibrate.performed += CalibrateAction;

            // add the vive tracker info to the GUI
            foreach (ViveTracker tracker in FindObjectsByType<ViveTracker>(FindObjectsSortMode.None))
            {
                numViveTrackers++;
                ViveTracker.TrackerRole binding = tracker.binding;
                trackerRoles.Add(binding.GetReadableName());
            }
            foreach (IndexController controller in FindObjectsByType<IndexController>(FindObjectsSortMode.None))
            {
                numIndexControllers++;
                IndexController.Role role = controller.role;
                controllerRoles.Add(role.GetReadableName());
            }
        }

        void CalibrateAction(InputAction.CallbackContext ctx)
        {
            foreach (ViveTracker tracker in FindObjectsByType<ViveTracker>(FindObjectsSortMode.None))
            {
                tracker.Calibrate();
            }
        }

#endif
    }
}

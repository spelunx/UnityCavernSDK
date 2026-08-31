using UnityEngine;
using UnityEngine.InputSystem;

namespace Spelunx.XR.Vive
{
    [AddComponentMenu("Cavern/Tracking/IndexController")]
    [DisallowMultipleComponent]
    public class IndexController : OpenXRTrackedDevice
    {
        /// <summary>
        /// The OpenXR Role for this controller
        /// </summary>
        public enum Role
        {
            None,
            LeftHand,
            RightHand,
            AnyHand
        }
        [Header("Tracker Role")]

        [Tooltip("Specify an OpenXR binding for this controller. Assign a controller to this same binding in SteamVR.")]
        public Role role = Role.None;

        #region Input Actions
        public InputAction PoseAction { get; private set; }
        public InputAction TrackingState { get; private set; }
        public InputAction SystemPressed { get; private set; }
        public InputAction SystemTouched { get; private set; }
        public InputAction AButtonPressed { get; private set; }
        public InputAction AButtonTouched { get; private set; }
        public InputAction BButtonPressed { get; private set; }
        public InputAction BButtonTouched { get; private set; }
        public InputAction GripPressed { get; private set; }
        public InputAction GripSqueeze { get; private set; }
        public InputAction GripSqueezeForce { get; private set; }
        public InputAction TriggerValue { get; private set; }
        public InputAction TriggerPressed { get; private set; }
        public InputAction TriggerTouched { get; private set; }
        public InputAction Thumbstick { get; private set; }
        public InputAction ThumbstickPressed { get; private set; }
        public InputAction ThumbstickTouched { get; private set; }
        public InputAction Trackpad { get; private set; }
        public InputAction TrackpadForce { get; private set; }
        public InputAction TrackpadTouched { get; private set; }
        public InputAction PointerPose { get; private set; }
        public InputAction Haptics { get; private set; }
        public InputAction IsTracked { get; private set; }
        #endregion Input Actions

        void Awake()
        {
            actionMap = new InputActionMap($"Index Controller {role}");
            if (role == Role.None) return; // No actions for a None tracker role
            string deviceName = role.GetRolePath();
            PoseAction = actionMap.AddAction("Pose", InputActionType.Value, $"<ValveIndexController>{deviceName}/devicePose", expectedControlLayout: "Pose");
            TrackingState = actionMap.AddAction("TrackingState", InputActionType.Value, $"<ValveIndexController>{deviceName}/trackingState", expectedControlLayout: "Integer");
            SystemPressed = actionMap.AddAction("SystemPress", InputActionType.Button, $"<ValveIndexController>{deviceName}/system", expectedControlLayout: "Button");
            SystemTouched = actionMap.AddAction("SystemTouch", InputActionType.Button, $"<ValveIndexController>{deviceName}/systemTouched", expectedControlLayout: "Button");
            AButtonPressed = actionMap.AddAction("APress", InputActionType.Button, $"<ValveIndexController>{deviceName}/primaryButton", expectedControlLayout: "Button");
            AButtonTouched = actionMap.AddAction("ATouch", InputActionType.Button, $"<ValveIndexController>{deviceName}/primaryTouched", expectedControlLayout: "Button");
            BButtonPressed = actionMap.AddAction("BPress", InputActionType.Button, $"<ValveIndexController>{deviceName}/secondaryButton", expectedControlLayout: "Button");
            BButtonTouched = actionMap.AddAction("BTouch", InputActionType.Button, $"<ValveIndexController>{deviceName}/secondaryTouched", expectedControlLayout: "Button");
            GripPressed = actionMap.AddAction("GripPress", InputActionType.Button, $"<ValveIndexController>{deviceName}/gripPressed", expectedControlLayout: "Button");
            GripSqueeze = actionMap.AddAction("GripSqueeze", InputActionType.Value, $"<ValveIndexController>{deviceName}/grip", expectedControlLayout: "Axis");
            GripSqueezeForce = actionMap.AddAction("GripSqueezeForce", InputActionType.Value, $"<ValveIndexController>{deviceName}/gripForce", expectedControlLayout: "Axis");
            TriggerValue = actionMap.AddAction("Trigger", InputActionType.Value, $"<ValveIndexController>{deviceName}/trigger", expectedControlLayout: "Axis");
            TriggerPressed = actionMap.AddAction("TriggerPress", InputActionType.Button, $"<ValveIndexController>{deviceName}/triggerPressed", expectedControlLayout: "Button");
            TriggerTouched = actionMap.AddAction("TriggerTouch", InputActionType.Button, $"<ValveIndexController>{deviceName}/triggerTouched", expectedControlLayout: "Button");
            Thumbstick = actionMap.AddAction("Thumbstick", InputActionType.Value, $"<ValveIndexController>{deviceName}/thumbstick", expectedControlLayout: "Stick");
            ThumbstickPressed = actionMap.AddAction("ThumbstickPressed", InputActionType.Button, $"<ValveIndexController>{deviceName}/thumbstickClicked", expectedControlLayout: "Button");
            ThumbstickTouched = actionMap.AddAction("ThumbstickTouched", InputActionType.Button, $"<ValveIndexController>{deviceName}/thumbstickTouched", expectedControlLayout: "Button");
            Trackpad = actionMap.AddAction("Trackpad", InputActionType.Value, $"<ValveIndexController>{deviceName}/trackpad", expectedControlLayout: "Stick");
            TrackpadForce = actionMap.AddAction("TrackpadForce", InputActionType.Value, $"<ValveIndexController>{deviceName}/trackpadForce", expectedControlLayout: "Axis");
            TrackpadTouched = actionMap.AddAction("TrackpadTouched", InputActionType.Button, $"<ValveIndexController>{deviceName}/trackpadTouched", expectedControlLayout: "Button");
            PointerPose = actionMap.AddAction("PointerPose", InputActionType.Value, $"<ValveIndexController>{deviceName}/pointer", expectedControlLayout: "Pose");
            Haptics = actionMap.AddAction("Haptics", InputActionType.Value, $"<ValveIndexController>{deviceName}/haptic", expectedControlLayout: "Haptic");
            IsTracked = actionMap.AddAction("IsTracked", InputActionType.Button, $"<ValveIndexController>{deviceName}/isTracked", expectedControlLayout: "Button");

            PoseAction.performed += OnPose;
            PoseAction.canceled += OnPoseCanceled;
            TrackingState.performed += OnTrackingStatePerformed;
            TrackingState.canceled += OnTrackingStateCanceled;
        }


#if UNITY_EDITOR
        // A gizmo, which can be enabled or disabled through the gizmos menu
        // This shows the position, size, and rotation of the index controller.
        private void OnDrawGizmos()
        {
            if (role == Role.LeftHand)
            {
                ViveDebugMeshes.indexControllerLeftMaterial.SetPass(0);
                Graphics.DrawMeshNow(ViveDebugMeshes.indexControllerLeftMesh, transform.position, transform.rotation);
            }
            else if (role == Role.RightHand)
            {
                ViveDebugMeshes.indexControllerRightMaterial.SetPass(0);
                Graphics.DrawMeshNow(ViveDebugMeshes.indexControllerRightMesh, transform.position, transform.rotation);
            }
        }
#endif
    }


    public static class ControllerRoleExtensions
    {
        public static string GetReadableName(this IndexController.Role role)
        {
            return role switch
            {
                IndexController.Role.None => "None",
                IndexController.Role.LeftHand => "Left Hand",
                IndexController.Role.RightHand => "Right Hand",
                IndexController.Role.AnyHand => "Hand Hand",
                _ => "(undefined)",
            };
        }

        public static string GetRolePath(this IndexController.Role role)
        {
            return role switch
            {
                IndexController.Role.LeftHand => "{LeftHand}",
                IndexController.Role.RightHand => "{RightHand}",
                IndexController.Role.AnyHand => "",
                _ => "(undefined)",
            };
        }
    }
}
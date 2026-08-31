using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Input;

namespace Spelunx.XR.Vive
{
    // Some of this code is taken from the UnityEngine.InputSystem.XR.TrackedPoseDriver script

    [AddComponentMenu("Cavern/Tracking/ViveTracker")]
    [DisallowMultipleComponent]
    public class ViveTracker : OpenXRTrackedDevice
    {
        /// <summary>
        /// The OpenXR Role for this tracker
        /// </summary>
        public enum TrackerRole
        {
            [InspectorName("Disabled")] // note: This isn't actually disabled. It's just another binding
            None,
            // [InspectorName("Any")] // note: Reacts to any change in any tracker
            // Any,
            [InspectorName("Handheld Object")]
            Handheld_Object,
            [InspectorName("Left Foot")]
            LeftFoot,
            [InspectorName("Right Foot")]
            RightFoot,
            [InspectorName("Left Shoulder")]
            LeftShoulder,
            [InspectorName("Right Shoulder")]
            RightShoulder,
            [InspectorName("Left Elbow")]
            LeftElbow,
            [InspectorName("Right Elbow")]
            RightElbow,
            [InspectorName("Left Knee")]
            LeftKnee,
            [InspectorName("Right Knee")]
            RightKnee,
            [InspectorName("Left Wrist")]
            LeftWrist,
            [InspectorName("Right Wrist")]
            RightWrist,
            [InspectorName("Left Ankle")]
            LeftAnkle,
            [InspectorName("Right Ankle")]
            RightAnkle,
            [InspectorName("Waist")]
            Waist,
            [InspectorName("Chest")]
            Chest,
            [InspectorName("Camera")]
            Camera,
            [InspectorName("Keyboard")]
            Keyboard,
        }

        [Header("Tracker Role")]
        [Tooltip("Specify an OpenXR binding for this tracker. Assign a tracker to this same binding in SteamVR.")]
        public TrackerRole binding = TrackerRole.None;

        private InputAction _poseAction;
        public InputAction PoseAction => _poseAction;
        private InputAction _positionAction;
        public InputAction PositionAction => _positionAction;
        private InputAction _rotationAction;
        public InputAction RotationAction => _rotationAction;
        private InputAction _triggerPress;
        public InputAction TriggerPress => _triggerPress;
        private InputAction _squeezePress;
        public InputAction SqueezePress => _squeezePress;
        private InputAction _menuPress;
        public InputAction MenuPress => _menuPress;
        private InputAction _systemPress;
        public InputAction SystemPress => _systemPress;
        private InputAction _trackpadPress;
        public InputAction TrackpadPress => _trackpadPress;
        private InputAction _isTrackedAction;
        public InputAction IsTracked => _isTrackedAction;
        private InputAction _trackingStateAction;
        // test
        private InputAction _powerPress;
        public InputAction PowerPress => _powerPress;
        private InputAction _thumbPress;
        public InputAction ThumbPress => _thumbPress;
        private InputAction _haptics;
        // end test
        public InputAction TrackingState => _trackingStateAction;

        public bool IsCurrentlyTracking => (_isTrackedAction?.ReadValue<float>() ?? 0) >= 0.5f;

        void Awake()
        {
            actionMap = new InputActionMap($"Vive Tracker {binding}");
            // if (binding == TrackerRole.None) return; // No actions for a None tracker role
            string deviceName = binding.GetRolePath();
            _positionAction = actionMap.AddAction("Position", InputActionType.Value, $"<XRViveTracker>{deviceName}/devicePosition", expectedControlLayout: "Vector3");
            _rotationAction = actionMap.AddAction("Rotation", InputActionType.Value, $"<XRViveTracker>{deviceName}/deviceRotation", expectedControlLayout: "Quaternion");
            _triggerPress = actionMap.AddAction("TriggerPress", InputActionType.Button, $"<XRViveTracker>{deviceName}/triggerButton", expectedControlLayout: "Digital");
            _squeezePress = actionMap.AddAction("SqueezePress", InputActionType.Button, $"<XRViveTracker>{deviceName}/squeezeButton", expectedControlLayout: "Digital");
            _systemPress = actionMap.AddAction("SystemPress", InputActionType.Button, $"<XRViveTracker>{deviceName}/systemButton", expectedControlLayout: "Digital");
            _menuPress = actionMap.AddAction("MenuPress", InputActionType.Button, $"<XRViveTracker>{deviceName}/menuButton", expectedControlLayout: "Digital");
            _trackpadPress = actionMap.AddAction("TrackpadPress", InputActionType.Button, $"<XRViveTracker>{deviceName}/trackpadButton", expectedControlLayout: "Digital");
            _haptics = actionMap.AddAction("Haptics", InputActionType.Value, $"<XRViveTracker>{deviceName}/haptics", expectedControlLayout: "Haptic");
            _trackingStateAction = actionMap.AddAction("TrackingState", InputActionType.Value, $"<XRViveTracker>{deviceName}/trackingState", expectedControlLayout: "Integer");
            _poseAction = actionMap.AddAction("Pose", InputActionType.Value, $"<XRViveTracker>{deviceName}/devicePose", expectedControlLayout: "Pose");
            _isTrackedAction = actionMap.AddAction("IsTracked", InputActionType.Button, $"<XRViveTracker>{deviceName}/isTracked", expectedControlLayout: "Digital");
            //test
            // _powerPress = actionMap.AddAction("PowerPress", InputActionType.Button, $"<XRViveTracker>{deviceName}/powerButton", expectedControlLayout: "Digital");
            _thumbPress = actionMap.AddAction("ThumbPress", InputActionType.Button, $"<XRViveTracker>{deviceName}/thumbButton", expectedControlLayout: "Digital");

            // if(binding == TrackerRole.Any)
            // {
            //     _positionAction.performed += OnPosition;
            //     _positionAction.canceled += OnPositionCanceled;
            //     _rotationAction.performed += OnRotation;
            //     _rotationAction.canceled += OnRotationCanceled;
            //     // TODO: why doesn't rotation track?
            // } else
            // {
            _poseAction.performed += OnPose;
            _poseAction.canceled += OnPoseCanceled;
            // }

            _trackingStateAction.performed += OnTrackingStatePerformed;
            _trackingStateAction.canceled += OnTrackingStateCanceled;
        }

        public void Calibrate()
        {
            // TODO: implement this
        }

        /// <summary>
        /// Make the Vive Tracker vibrate
        /// </summary>
        /// <param name="amplitude">Vibration intensity, in range [0,1]</param>
        /// <param name="duration">Duration in seconds, > 0</param>
        public void TriggerHaptics(float amplitude, float duration)
        {
            if (_haptics.activeControl != null)
            {
                InputDevice targetDevice = _haptics.activeControl.device;
                OpenXRInput.SendHapticImpulse(_haptics, amplitude, duration, targetDevice);
            }
            else
            {
                OpenXRInput.SendHapticImpulse(_haptics, amplitude, duration);
            }
        }

        /// <summary>
        /// Stop any active vibration for this Vive Tracker
        /// </summary>
        public void StopHaptics()
        {
            OpenXRInput.StopHaptics(_haptics, _haptics.activeControl?.device);
        }

#if UNITY_EDITOR
        // A gizmo, which can be enabled or disabled through the gizmos menu
        // This shows the position, size, and rotation of the vive tracker.
        private void OnDrawGizmos()
        {
            ViveDebugMeshes.trackerMaterial.SetPass(0);
            Graphics.DrawMeshNow(ViveDebugMeshes.trackerMesh, transform.position, transform.rotation);
        }
#endif
    }



    public static class TrackerRoleExtensions
    {
        public static string GetReadableName(this ViveTracker.TrackerRole role)
        {
            return role switch
            {
                ViveTracker.TrackerRole.None => "Disabled",
                // ViveTracker.TrackerRole.Any => "Any",
                ViveTracker.TrackerRole.Handheld_Object => "Handheld Object",
                ViveTracker.TrackerRole.LeftFoot => "Left Foot",
                ViveTracker.TrackerRole.RightFoot => "Right Foot",
                ViveTracker.TrackerRole.LeftShoulder => "Left Shoulder",
                ViveTracker.TrackerRole.RightShoulder => "Right Shoulder",
                ViveTracker.TrackerRole.LeftElbow => "Left Elbow",
                ViveTracker.TrackerRole.RightElbow => "Right Elbow",
                ViveTracker.TrackerRole.LeftKnee => "Left Knee",
                ViveTracker.TrackerRole.RightKnee => "Right Knee",
                ViveTracker.TrackerRole.LeftWrist => "Left Wrist",
                ViveTracker.TrackerRole.RightWrist => "Right Wrist",
                ViveTracker.TrackerRole.LeftAnkle => "Left Ankle",
                ViveTracker.TrackerRole.RightAnkle => "Right Ankle",
                ViveTracker.TrackerRole.Waist => "Waist",
                ViveTracker.TrackerRole.Chest => "Chest",
                ViveTracker.TrackerRole.Camera => "Camera",
                ViveTracker.TrackerRole.Keyboard => "Keyboard",
                _ => "(undefined)",
            };
        }
        public static string GetRolePath(this ViveTracker.TrackerRole role)
        {
            return role switch
            {
                // ViveTracker.TrackerRole.Any => "",
                ViveTracker.TrackerRole.None => "(none)",
                ViveTracker.TrackerRole.Handheld_Object => "{Handheld Object}",
                ViveTracker.TrackerRole.LeftFoot => "{Left Foot}",
                ViveTracker.TrackerRole.RightFoot => "{Right Foot}",
                ViveTracker.TrackerRole.LeftShoulder => "{Left Shoulder}",
                ViveTracker.TrackerRole.RightShoulder => "{Right Shoulder}",
                ViveTracker.TrackerRole.LeftElbow => "{Left Elbow}",
                ViveTracker.TrackerRole.RightElbow => "{Right Elbow}",
                ViveTracker.TrackerRole.LeftKnee => "{Left Knee}",
                ViveTracker.TrackerRole.RightKnee => "{Right Knee}",
                ViveTracker.TrackerRole.LeftWrist => "{Left Wrist}",
                ViveTracker.TrackerRole.RightWrist => "{Right Wrist}",
                ViveTracker.TrackerRole.LeftAnkle => "{Left Ankle}",
                ViveTracker.TrackerRole.RightAnkle => "{Right Ankle}",
                ViveTracker.TrackerRole.Waist => "{Waist}",
                ViveTracker.TrackerRole.Chest => "{Chest}",
                ViveTracker.TrackerRole.Camera => "{Camera}",
                ViveTracker.TrackerRole.Keyboard => "{Keyboard}",
                _ => "(undefined)",
            };
        }
    }
}
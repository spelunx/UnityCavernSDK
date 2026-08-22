using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;

namespace Spelunx.XR.Vive
{
    // Some of this code is taken from the UnityEngine.InputSystem.XR.TrackedPoseDriver script

    public abstract class OpenXRTrackedDevice : MonoBehaviour
    {
        [Header("Tracking Settings")]
        [SerializeField, Tooltip("Which Transform properties to update.")]
        private TrackingEnums.TrackingType trackingType = TrackingEnums.TrackingType.RotationAndPosition;
        [SerializeField, Tooltip("Updates the Transform properties after these phases of Input System event processing.")]
        private TrackingEnums.UpdateType updateType = TrackingEnums.UpdateType.UpdateAndBeforeRender;
        [SerializeField, Tooltip("What should happen if the device loses tracking")]
        private TrackingEnums.TrackingFailureMode failureMode = TrackingEnums.TrackingFailureMode.FreezePositionAndRotation;
        [SerializeField, Tooltip("How to map the real world coordinates to unity coordinates")]
        private TrackingEnums.OriginMode originMode = TrackingEnums.OriginMode.Origin;

        // private Pose m_currentPose;
        private Vector3 m_currentPosition;
        private Quaternion m_currentRotation;
        private UnityEngine.XR.InputTrackingState m_CurrentTrackingState;

        private TrackingSpaceOrigin origin;

        protected InputActionMap actionMap;

        void OnEnable()
        {
            InputSystem.onAfterUpdate += UpdateCallback;
            InputSystem.onDeviceChange += OnDeviceChanged;
            TrackingSpaceOrigin.OriginEnabled += SetOrigin;
            TrackingSpaceOrigin.OriginDisabled += RemoveOrigin;
            actionMap?.Enable();
        }

        void OnDisable()
        {
            InputSystem.onAfterUpdate -= UpdateCallback;
            InputSystem.onDeviceChange -= OnDeviceChanged;
            TrackingSpaceOrigin.OriginEnabled -= SetOrigin;
            TrackingSpaceOrigin.OriginDisabled -= RemoveOrigin;
            actionMap?.Disable();
        }

        void Start()
        {
            var foundOrigin = TrackingSpaceOrigin.Instance;
            if (foundOrigin != null && foundOrigin.isActiveAndEnabled)
            {
                origin = foundOrigin;
            }
        }

        void SetOrigin(TrackingSpaceOrigin origin)
        {
            this.origin = origin;
        }

        void RemoveOrigin(TrackingSpaceOrigin origin)
        {
            this.origin = null;
        }

        /// <summary>
        /// This should be attached to the "performed" event of a "Pose" input action.
        /// It updates the position and rotation of this object.
        /// </summary>
        /// <param name="context"></param>
        protected void OnPose(InputAction.CallbackContext context)
        {
            var poseState = context.ReadValue<PoseState>();
            m_CurrentTrackingState = poseState.trackingState;
            m_currentPosition = poseState.position;
            m_currentRotation = poseState.rotation;
            // m_currentPose = new Pose(poseState.position, poseState.rotation);
        }

        /// <summary>
        /// This should be attached to the "canceled" event of a "Pose" input action.
        /// It updates the position and rotation of this object.
        /// </summary>
        /// <param name="context"></param>
        protected void OnPoseCanceled(InputAction.CallbackContext context)
        {
            // m_currentPose = Pose.identity;
            m_currentPosition = Vector3.zero;
            m_currentRotation = Quaternion.identity;
        }

        /// <summary>
        /// This should be attached to the "performed" event of a "Position" input action.
        /// It updates the position of this object.
        /// </summary>
        /// <param name="context"></param>
        protected void OnPosition(InputAction.CallbackContext context)
        {
            var position = context.ReadValue<Vector3>();
            m_currentPosition = position;
        }

        /// <summary>
        /// This should be attached to the "canceled" event of a "Position" input action.
        /// It updates the position of this object.
        /// </summary>
        /// <param name="context"></param>
        protected void OnPositionCanceled(InputAction.CallbackContext context)
        {
            m_currentPosition = Vector3.zero;
        }

        /// <summary>
        /// This should be attached to the "performed" event of a "Rotation" input action.
        /// It updates the rotation of this object.
        /// </summary>
        /// <param name="context"></param>
        protected void OnRotation(InputAction.CallbackContext context)
        {
            var rotation = context.ReadValue<Quaternion>();
            m_currentRotation = rotation;
        }

        /// <summary>
        /// This should be attached to the "canceled" event of a "Rotation" input action.
        /// It updates the rotation of this object.
        /// </summary>
        /// <param name="context"></param>
        protected void OnRotationCanceled(InputAction.CallbackContext context)
        {
            m_currentRotation = Quaternion.identity;
        }

        /// <summary>
        /// This should be attached to the "performed" event of a "trackingState" input action.
        /// </summary>
        /// <param name="context"></param>
        protected void OnTrackingStatePerformed(InputAction.CallbackContext context)
        {
            m_CurrentTrackingState = (UnityEngine.XR.InputTrackingState)context.ReadValue<int>();
        }

        /// <summary>
        /// This should be attached to the "canceled" event of a "trackingState" input action.
        /// </summary>
        /// <param name="context"></param>
        protected void OnTrackingStateCanceled(InputAction.CallbackContext context)
        {
            m_CurrentTrackingState = UnityEngine.XR.InputTrackingState.None;
        }

        /// <summary>
        /// The callback method called after the Input System has completed an update and processed all pending events.
        /// </summary>
        /// <seealso cref="InputSystem.onAfterUpdate"/>
        protected void UpdateCallback()
        {
            if (updateType == TrackingEnums.UpdateType.UpdateAndBeforeRender
            || (InputState.currentUpdateType == InputUpdateType.BeforeRender && updateType == TrackingEnums.UpdateType.BeforeRender)
            || (InputState.currentUpdateType != InputUpdateType.BeforeRender && updateType == TrackingEnums.UpdateType.Update))
            {
                PerformUpdate();
            }
        }

        void OnDeviceChanged(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
        {
            // m_CurrentTrackingState = TrackingEnums.TrackingStates.None;
        }

        /// <summary>
        /// Updates <see cref="Transform"/> properties with the current input pose values that have been read,
        /// constrained by tracking type and tracking state.<br/>
        /// Affected by the origin.
        /// </summary>
        private void PerformUpdate()
        {
            // Pose offsetPose = Pose.identity;
            transform.GetPositionAndRotation(out Vector3 newPosition, out Quaternion newRotation);
            if (m_CurrentTrackingState == UnityEngine.XR.InputTrackingState.None)
            {
                switch (failureMode)
                {
                    case TrackingEnums.TrackingFailureMode.FreezePositionAndRotation:
                        // offsetPose = transform.GetWorldPose();
                        break;
                    case TrackingEnums.TrackingFailureMode.FreezePosition:
                        // offsetPose = new Pose(transform.position, Quaternion.identity);
                        newRotation = Quaternion.identity;
                        break;
                    case TrackingEnums.TrackingFailureMode.SnapToDefault:
                        // offsetPose = Pose.identity;
                        newPosition = Vector3.zero;
                        newRotation = Quaternion.identity;
                        break;
                }
            }
            else
            {
                switch (originMode)
                {
                    case TrackingEnums.OriginMode.Origin:
                        {
                            // // Realign the rotation of the vive tracker. It's hard to know visually when they're pointing forwards,
                            // // So this offsets their rotation
                            // if (doCalibration)
                            // {
                            //     // calibrate
                            //     rotationAlignment = Quaternion.Inverse(origin.rotation * rigidTransform.rot);
                            //     doCalibration = false;
                            // }
                            // transform.rotation = origin.rotation * rigidTransform.rot * rotationAlignment;
                            // if (origin == null)
                            // {
                            // offsetPose = m_currentPose;
                            // }
                            if (origin != null)
                            {
                                // var offsetPose = origin.transform.TransformPose(m_currentPose);
                                // m_currentPose.GetTransformedBy( origin.transform);
                                // position = lhs.TransformPoint(Pose.position),
                                // rotation = lhs.rotation * Pose.rotation
                                // newPosition = offsetPose.position;
                                // newRotation = offsetPose.rotation;
                                newPosition = origin.transform.TransformPoint(m_currentPosition);
                                newRotation = origin.transform.rotation * m_currentRotation;
                            }
                            break;
                        }
                    case TrackingEnums.OriginMode.Local:
                        {
                            // var offsetPose = transform.parent.TransformPose(m_currentPose);
                            // newPosition = offsetPose.position;
                            // newRotation = offsetPose.rotation;
                            newPosition = transform.parent.TransformPoint(m_currentPosition);
                            newRotation = transform.parent.rotation * m_currentRotation;
                            break;
                        }
                    case TrackingEnums.OriginMode.World:
                        {
                            // newPosition = m_currentPose.position;
                            // newRotation = m_currentPose.rotation;
                            newPosition = m_currentPosition;
                            newRotation = m_currentRotation;
                            break;
                        }
                }
            }


            switch (trackingType)
            {
                case TrackingEnums.TrackingType.RotationAndPosition:
                    transform.SetPositionAndRotation(newPosition, newRotation);
                    break;
                case TrackingEnums.TrackingType.RotationOnly:
                    transform.rotation = newRotation;
                    break;
                case TrackingEnums.TrackingType.PositionOnly:
                    transform.position = newPosition;
                    break;
                case TrackingEnums.TrackingType.None:
                    break;
            }
        }

    }
}
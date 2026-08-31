using UnityEngine;

namespace Spelunx.XR.Vive
{
    public static class TrackingEnums
    {
        /// <summary>
        /// Options for which <see cref="Transform"/> properties to update.
        /// </summary>
        /// <seealso cref="trackingType"/>
        public enum TrackingType
        {
            /// <summary>
            /// Update both rotation and position.
            /// </summary>
            RotationAndPosition,

            /// <summary>
            /// Update rotation only.
            /// </summary>
            RotationOnly,

            /// <summary>
            /// Update position only.
            /// </summary>
            PositionOnly,

            /// <summary>
            /// Don't update any position or rotation. Maybe useful if you only want control input.
            /// </summary>
            None
        }

        /// <summary>
        /// Options for which phases of the player loop will update <see cref="Transform"/> properties.
        /// </summary>
        /// <seealso cref="updateType"/>
        /// <seealso cref="InputSystem.onAfterUpdate"/>
        public enum UpdateType
        {
            /// <summary>
            /// Update after the Input System has completed an update and right before rendering.
            /// This is the recommended and default option to minimize lag for XR tracked devices.
            /// </summary>
            /// <seealso cref="InputUpdateType.BeforeRender"/>
            UpdateAndBeforeRender,

            /// <summary>
            /// Update after the Input System has completed an update except right before rendering.
            /// </summary>
            /// <remarks>
            /// This may be dynamic update, fixed update, or a manual update depending on the Update Mode
            /// project setting for Input System.
            /// </remarks>
            Update,

            /// <summary>
            /// Update after the Input System has completed an update right before rendering.
            /// </summary>
            /// <remarks>
            /// Note that this update mode may not trigger if there are no XR devices added which use before render timing.
            /// </remarks>
            /// <seealso cref="InputUpdateType.BeforeRender"/>
            /// <seealso cref="InputDevice.updateBeforeRender"/>
            BeforeRender,
        }

        /// <summary>
        /// What should happen if a device loses tracking
        /// </summary>
        public enum TrackingFailureMode
        {
            /// <summary>
            /// Keep the device in the same position and rotation
            /// </summary>
            FreezePositionAndRotation,

            /// <summary>
            /// Keep the device in the same place, but reset the rotation
            /// </summary>
            FreezePosition,

            /// <summary>
            /// Reset the position and rotation of the device
            /// </summary>
            SnapToDefault
        }

        /// <summary>
        /// How the position and rotation updates should be applied
        /// </summary>
        public enum OriginMode
        {
            /// <summary>
            /// Apply position and rotation relative to an tracking origin transform
            /// </summary>
            Origin,
            /// <summary>
            /// Apply position and rotation locally, relative to the parent transform
            /// </summary>
            Local,
            /// <summary>
            /// Apply position and rotation centered around world (0,0,0)
            /// </summary>
            World
        }
    }
}

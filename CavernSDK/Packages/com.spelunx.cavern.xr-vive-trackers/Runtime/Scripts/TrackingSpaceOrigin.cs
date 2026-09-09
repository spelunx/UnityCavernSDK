// Copyright HTC Corporation All Rights Reserved.

using System;
using UnityEngine;

namespace Spelunx.XR.Vive
{
    /// <summary>
    /// This component acts as a world space refernce point of the tracking space origin.
    /// Add this component to the root of your VR camera rig in order to let other features to find the tracking space origin and apply positional and rotational offset when needed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cavern/Tracking/TrackingSpaceOrigin")]
    public sealed class TrackingSpaceOrigin : MonoBehaviour
    {
        public static TrackingSpaceOrigin Instance { get { return m_Instance; } }
        private static TrackingSpaceOrigin m_Instance = null;
        public static event Action<TrackingSpaceOrigin> OriginEnabled;
        public static event Action<TrackingSpaceOrigin> OriginDisabled;

        private void Awake()
        {
            if (m_Instance != null)
            {
                Destroy(m_Instance);
            }

            m_Instance = this;
        }

        void OnEnable()
        {
            OriginEnabled?.Invoke(this);
        }

        void OnDisable()
        {
            OriginDisabled?.Invoke(this);
        }
    }
}

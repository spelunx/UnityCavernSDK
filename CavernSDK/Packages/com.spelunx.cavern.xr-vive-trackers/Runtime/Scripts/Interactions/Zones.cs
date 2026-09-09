using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Spelunx.XR.Vive
{
    [AddComponentMenu("Cavern/Tracking/Interactions/Zones")]
    public class Zones : MonoBehaviour
    {
        [Tooltip("The number of zones within the CAVERN angle. There is one extra zone where there is no screen.")]
        public int numZones = 3;

        [Tooltip("The radius in the middle of the CAVERN that acts as a deadzone, to avoid rapid angle changes.")]
        public float innerDeadZone = 1;

        [Tooltip("The CAVERN setup, needed to get angle and radius information.")]
        public CavernSetup cavern;

        [Tooltip("Each object you want to track through zones should have an entry in this array.")]
        public ZonedTracker[] zonedTrackers;

        [Serializable]
        public struct ZonedTracker
        {
            [Tooltip("The object to track in zones. Usually a Vive Tracker.")]
            public Transform transform;
            [Tooltip("The current zone the object is in. -1 if out of bounds, or between 0 and numZones.")]
            public int zone;
            [Tooltip("The distance the object is from the edge of the deadzone. Ranges from [0,1], where 1 is the edge of the CAVERN.")]
            public float distance;
        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 0; i < zonedTrackers.Length; i++)
            {
                Vector3 tracker_pos = zonedTrackers[i].transform.position;
                tracker_pos.y = cavern.transform.position.y;

                float angle = Vector3.SignedAngle(tracker_pos - transform.position, Vector3.forward, Vector3.down);
                angle += cavern.CavernAngle / 2; // realign zero to left side of cavern
                if (angle < 0 || angle >= cavern.CavernAngle)
                {
                    zonedTrackers[i].zone = -1; // angles that are outside of the swoop of the cavern are in the deadzone
                    zonedTrackers[i].distance = 0;
                    return;
                }
                float distance = Vector3.Distance(tracker_pos, transform.position);
                if (distance < innerDeadZone)
                {
                    zonedTrackers[i].zone = -1; // distances that are close to the center are in the deadzone
                    zonedTrackers[i].distance = 0;
                }
                else
                {
                    zonedTrackers[i].zone = (int)Mathf.Floor(angle / (cavern.CavernAngle / numZones));
                    zonedTrackers[i].distance = Mathf.InverseLerp(innerDeadZone, cavern.CavernRadius, distance);
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.DrawWireDisc(transform.position, Vector3.up, innerDeadZone);
            float cavernAngle = cavern.CavernAngle * Mathf.PI / 180;
            float angle = -cavernAngle / 2;
            float deltaAngle = cavernAngle / numZones;
            for (int i = 0; i < numZones + 1; i++)
            {
                Vector3 angleLine = new(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                Gizmos.DrawLine(innerDeadZone * angleLine + transform.position, cavern.CavernRadius * angleLine + transform.position);
                angle += deltaAngle;
            }
        }
#endif
    }
}

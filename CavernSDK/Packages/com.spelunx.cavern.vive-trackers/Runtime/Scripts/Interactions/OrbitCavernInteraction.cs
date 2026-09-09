using UnityEngine;

namespace Spelunx.Vive
{
    /*
     * This script assumes the CAVERN screen is a mirror, and the object holding this script will be mirrored.
     * A deadzone radius value can be assigned to prevent area near the center reflecting endlessly and spinning around.
     */
    public class OrbitCavernInteraction : CavernInteraction
    {
        public enum OrbitMode
        {
            Default = 0,
            MirrorDistance,
            FixedDistance
        }

        [SerializeField] private float deadZoneRadius = 0.5f;
        [SerializeField] private bool lookAtCentre = true;
        [SerializeField] private OrbitMode orbitMode = OrbitMode.Default;

        public void SetDeadZoneRadius(float deadZoneRadius) { this.deadZoneRadius = deadZoneRadius; }
        public float GetDeadZoneRadius() { return deadZoneRadius; }

        public bool GetLookAtCentre() { return lookAtCentre; }
        public void SetLookAtCentre(bool lookAtCentre) { this.lookAtCentre = lookAtCentre; }

        public OrbitMode GetOrbitMode() { return orbitMode; }
        public void SetOrbitMode(OrbitMode orbitMode) { this.orbitMode = orbitMode; }

        private void Update()
        {
            if (target == null || cavernSetup == null) return;

            // Set position.
            Vector3 targetPosition = new Vector3(target.position.x, 0.0f, target.position.z);
            Vector3 cavernPosition = new Vector3(cavernSetup.transform.position.x, 0.0f, cavernSetup.transform.position.z);
            Vector3 cavernToTarget = targetPosition - cavernPosition;
            if (cavernToTarget.magnitude < deadZoneRadius) return;
            float screenRadius = cavernSetup.CavernRadius;

            switch (orbitMode)
            {
                case OrbitMode.MirrorDistance:
                    transform.position = cavernPosition +
                                 new Vector3(0.0f, target.position.y, 0.0f) +
                                 cavernToTarget.normalized * (2.0f * screenRadius - cavernToTarget.magnitude);
                    break;
                case OrbitMode.FixedDistance:
                    Vector3 thisPosition = new Vector3(transform.position.x, 0.0f, transform.position.z);
                    transform.position = cavernPosition +
                                 new Vector3(0.0f, target.position.y, 0.0f) +
                                 cavernToTarget.normalized * (thisPosition - cavernPosition).magnitude;
                    break;
                default:
                    transform.position = cavernPosition +
                                 new Vector3(0.0f, target.position.y, 0.0f) +
                                 cavernToTarget.normalized * (screenRadius + cavernToTarget.magnitude);
                    break;
            }

            // Set Rotation.
            if (lookAtCentre)
            {
                transform.LookAt(new Vector3(cavernSetup.transform.position.x, target.position.y, cavernSetup.transform.position.z));
            }
        }
    }
}
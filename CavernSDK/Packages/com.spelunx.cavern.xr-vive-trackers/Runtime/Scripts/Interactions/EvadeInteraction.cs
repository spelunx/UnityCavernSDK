using UnityEngine;

namespace Spelunx.XR.Vive
{
    public class EvadeInteraction : Interaction
    {
        [SerializeField] private float triggerDistance = 2.0f; // Distance at which it moves away
        [SerializeField] private float evadeDistance = 3.0f; // How far it moves away
        [SerializeField] private float movementSpeed = 3.0f; // Speed of movement

        private Vector3 evadeDestination = Vector3.zero;
        private Vector3 evadeDirection = Vector3.zero;
        private bool isEvading = false;

        public void SetTriggerDistance(float triggerDistance) { this.triggerDistance = triggerDistance; }
        public float GetTriggerDistance() { return triggerDistance; }

        public void SetEvadeDistance(float evadeDistance) { this.evadeDistance = evadeDistance; }
        public float GetEvadeDistance() { return evadeDistance; }

        public void SetMovementSpeed(float movementSpeed) { this.movementSpeed = movementSpeed; }
        public float GetMovementSpeed() { return movementSpeed; }

        private void Update()
        {
            if (isEvading)
            {
                Evade();
            }
            else
            {
                Idle();
            }
        }

        private void Idle()
        {
            if (target == null) return;
            if (triggerDistance * triggerDistance < (transform.position - target.position).sqrMagnitude) { return; }

            Vector3 thisPosition = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 targetPosition = new Vector3(target.position.x, 0.0f, target.position.z);
            Vector3 targetToThis = thisPosition - targetPosition;

            evadeDirection = (targetToThis.sqrMagnitude < Mathf.Epsilon) ? Vector3.forward : targetToThis.normalized;
            evadeDestination = transform.position + evadeDirection * evadeDistance;
            isEvading = true;
        }

        private void Evade()
        {
            Vector3 thisToDestination = evadeDestination - transform.position;
            if (thisToDestination.sqrMagnitude < 0.1f)
            {
                isEvading = false;
                return;
            }

            transform.LookAt(transform.position + evadeDirection);
            transform.Translate(thisToDestination.normalized * movementSpeed * Time.deltaTime, Space.World);
        }
    }
}
using UnityEngine;

namespace Spelunx.XR.Vive
{
    /*
     * This script copies the movement of the target with an optional offset (x, y, z)
     * The default is no offset, moving at the same place as the target
     */
    public class FollowInteraction : Interaction
    {
        [SerializeField, Tooltip("The offset vector from the target position.")] private Vector3 positionOffset = new Vector3(0.0f, 0.0f, 0.0f);

        public Vector3 GetPositionOffset() { return positionOffset; }
        public void SetPositionOffset(Vector3 positionOffset) { this.positionOffset = positionOffset; }

        private void Update()
        {
            if (target == null) return;
            transform.position = target.position + positionOffset;
        }
    }
}
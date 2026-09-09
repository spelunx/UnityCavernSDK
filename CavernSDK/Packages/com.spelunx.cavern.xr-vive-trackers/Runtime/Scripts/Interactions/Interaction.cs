using UnityEngine;

namespace Spelunx.XR.Vive
{
    public abstract class Interaction : MonoBehaviour
    {
        [SerializeField] protected Transform target; // The transform the object reacts to

        public void SetTarget(Transform target) { this.target = target; }
        public Transform GetTarget() { return target; }
    }
}

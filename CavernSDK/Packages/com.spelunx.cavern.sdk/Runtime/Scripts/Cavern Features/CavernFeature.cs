using System;
using UnityEngine;

namespace Spelunx
{
    [ExecuteInEditMode]
    public abstract class CavernFeature : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected CavernSetup cavernSetup;

        public virtual void Awake()
        {
            EnsureCavernSetupInParent();
        }
        public virtual string Name { get; }

        public virtual void Create() { }

        public virtual void Remove() { }

        public virtual void Reset()
        {
            EnsureCavernSetupInParent();
        }

        public virtual void OnValidate()
        {
            EnsureCavernSetupInParent();
        }

        void EnsureCavernSetupInParent()
        {
            // Try to find the cavern in a parent component
            var parentCavernSetup = GetComponentInParent<CavernSetup>();
            if (parentCavernSetup != null)
            {
                cavernSetup = parentCavernSetup;
                return;
            }
            // If no setup there, try to find it on the current object
            if (TryGetComponent(out cavernSetup))
            {
                return;
            }

            Debug.LogWarning("Cavern Feature requires a CavernSetup component in parent");
        }
    }
}

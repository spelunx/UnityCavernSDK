using UnityEngine;

namespace Spelunx.XR.Vive
{
    public abstract class CavernInteraction : Interaction
    {
        [SerializeField, Tooltip("The CAVERN setup, needed for CAVERN dimensions.")] protected CavernSetup cavernSetup = null;

        public CavernSetup GetCavernSetup() { return cavernSetup; }
        public void SetCavernSetup(CavernSetup cavernSetup) { this.cavernSetup = cavernSetup; }
    }
}
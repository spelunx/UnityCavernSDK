using UnityEngine;

namespace Spelunx
{
    [ExecuteInEditMode]
    public class TestFeatureSet : MonoBehaviour
    {

        public CavernFeatureSet set = new();

        void Awake()
        {
            set.Add<CavernDebugKeysFeature>();
        }
    }
}

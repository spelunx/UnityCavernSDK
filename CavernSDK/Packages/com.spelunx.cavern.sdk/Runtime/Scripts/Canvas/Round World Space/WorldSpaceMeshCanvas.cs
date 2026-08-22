using UnityEngine;

namespace Spelunx
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class WorldSpaceMeshCanvas : MonoBehaviour
    {
        [SerializeField, Tooltip("A reference to the Cavern Setup")]
        private CavernSetup cavernSetup;

        [SerializeField, Tooltip("Distance from the screen to render. 0 is purely at the center, 1 is at the boundry"), Min(0)]
        private float distance = 1.0f;

        [SerializeField, Tooltip("Should the round canvas be automatically positioned around the CAVERN?")]
        private bool autoposition = true;

        private bool shouldUpdateMesh = false;
        private Mesh mesh;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            UpdateMesh();
            cavernSetup.settingsChanged.AddListener(() => shouldUpdateMesh = true);
        }

        void Update()
        {
            if (shouldUpdateMesh)
            {
                UpdateMesh();
                shouldUpdateMesh = false;
            }
            if (autoposition)
            {
                // center the mesh on the cavern's center by moving the y position down based on the difference in cavern height vs mesh height
                float yOffset = -cavernSetup.CavernHeight * (distance - 1) / 2;
                transform.localPosition = new(transform.localPosition.x, yOffset, transform.localPosition.z);
            }
        }

        // Create the mesh based on CAVERN size
        void UpdateMesh()
        {
            mesh = cavernSetup.GenerateMesh();
            mesh.name = "Round Canvas Mesh";
            GetComponent<MeshFilter>().mesh = mesh;
            transform.localScale = new(distance, distance, distance);
        }

        void OnValidate()
        {
            shouldUpdateMesh = true;
        }

        public void SetCavernRenderer(CavernSetup setup)
        {
            cavernSetup = setup;
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

namespace Spelunx
{
    [AddComponentMenu("Cavern/CAVERN Setup")]
    public class CavernSetup : MonoBehaviour
    {
        public enum StereoscopicMode
        {
            Mono, // Monoscopic mode. No 3D effect.
            Stereo, // Stereoscopic mode. Gives a 3D-movie effect when wearing 3D glasses.
        }

        [Header("Camera Settings")]
        [SerializeField, Tooltip("Dimensions of the Cavern display, in pixels")] private Vector2Int screenDimensions = new(5760, 1200);
        /// Whether to render in stereo or mono
        [SerializeField] private StereoscopicMode stereoMode = StereoscopicMode.Mono;
        /// Interpupillary Distance (IPD) in metres.
        [SerializeField, Range(0.05f, 0.08f), Tooltip("Interpupillary distance is the distance between our two pupils, in meters. Average is 65mm")]
        private float interpupillaryDistance = 0.065f;
        /// Cavern physical screen height in metres.
        [SerializeField, Min(0.1f), Tooltip("Height of the Cavern screen, in meters")]
        private float cavernHeight = 2.334f;
        /// Cavern physical screen radius in metres.
        [SerializeField, Min(0.1f), Tooltip("Radius of the Cavern space, in meters")] private float cavernRadius = 3.048f;
        /// Cavern physical screen angle in degrees.
        [SerializeField, Range(1.0f, 360.0f), Tooltip("Angle span of the Cavern, in degrees")] private float cavernAngle = 270.0f;
        /// Cavern physical screen elevation in metres, relative to the player's feet.
        [SerializeField, Range(-0.5f, 0.5f), Tooltip("Amount the Cavern base is offset from the ground, in meters")] private float cavernElevation = 0.0f;
        /// Increase accuracy at the cost of significant performance.
        [SerializeField, Tooltip("Increase accuracy at the cost of significant performance")] private bool enableConvergence = false;
        /// Software support for swapping the left and right eyes. (Off - Left Eye On Top, On - Right Eye On Top)
        [SerializeField, Tooltip("Should the left and right eyes be swapped")] private bool swapEyes = false;

        [Header("Head Tracking")]
        /// If set to true, the ear will follow the head.
        [SerializeField, Tooltip("Should the audio listener follow the head position. Normally true for single user headtracked and false for multiuser")] private bool tetherEar = true;
        /// If set to true, the head position will be clamped to within the the radius of the screen.
        [SerializeField, Tooltip("If set to true, the head position will be clamped to within the the radius of the screen.")] private bool clampHeadPosition = true;
        /// <summary>
        /// Sets the clamping radius of the head, if clampHeadPosition = true. 
        /// For example, if clampHeadRatio = 0.8 and cavernRadius = 3, the head will be clamped to a radius of 2.4.
        /// </summary>
        [SerializeField, Tooltip("Limit how close the head can get to the wall. There are rendering issues if the head goes past the wall"), Range(0.0f, 1.0f)] private float clampHeadRatio = 0.9f;


        [Header("References (Do NOT edit!)")]
        [SerializeField] private Transform head;
        [SerializeField] private Camera eye; // Ensure that UI culling mask is unset. Ensure that Output > Target Eye is set to None in the Inspector, or it'll render a blank screen on the Cavern PC! No I don't know why.
        [SerializeField] private Camera renderCamera; // Ensure that ONLY UI culling mask is set. Ensure that Output > Target Eye is set to None in the Inspector, or it'll render a blank screen on the Cavern PC! No I don't know why.
        [SerializeField] private AudioListener ear;

        /// <summary>
        /// The main camera used for rendering all the CAVERN render passes
        /// </summary>
        public Camera RenderCamera => renderCamera;

        [HideInInspector]
        public UnityEvent settingsChanged;
        // public CavernFeatureSet features;

        public StereoscopicMode GetStereoscopicMode() { return stereoMode; }
        public float IPD
        {
            get => interpupillaryDistance;
            set
            {
                if (0.05f <= value && 0.08f >= value)
                {

                    interpupillaryDistance = value;
                }
            }
        }
        public Vector2Int ScreenDimensions => screenDimensions;
        public float CavernHeight => cavernHeight;
        public float CavernRadius => cavernRadius;
        public float CavernAngle => cavernAngle;
        public float CavernElevation => cavernElevation;
        public float AspectRatio => cavernAngle / 360.0f * Mathf.PI * cavernRadius * 2.0f / cavernHeight;
        public bool Convergence
        {
            get => enableConvergence;
            set
            {
                enableConvergence = value;
            }
        }
        public GameObject GetHead() { return head.gameObject; }
        public Camera GetEye() { return eye; }
        public GameObject GetEar() { return ear.gameObject; }
        public Camera GetGUICamera() { return renderCamera; }

        public void SetStereoscopicMode(StereoscopicMode mode)
        {
            stereoMode = mode;
        }

        public bool SwapEyes
        {
            get
            {
                return swapEyes;
            }
            set
            {
                swapEyes = value;
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Since we are using the eye to render to cubemaps, we want to disable it here, so that it
            // doesn't do a "normal" render to the screen, which will be a waste since we are overriding it.
            // Instead, we will "highjack" the GUI camera insert a render pass into the URP RenderGraph to render the eye to the screen.
            eye.enabled = false;

        }

        private void LateUpdate()
        {

            // If clampHeadPosition is true, limit the head position to be within the bounds of the circle.
            if (clampHeadPosition)
            {
                Vector2 horizontalPosition = new Vector2(head.transform.localPosition.x, head.transform.localPosition.z);
                if (horizontalPosition.sqrMagnitude > clampHeadRatio * clampHeadRatio * cavernRadius * cavernRadius)
                {
                    horizontalPosition = horizontalPosition.normalized * clampHeadRatio * cavernRadius;
                    head.transform.localPosition = new Vector3(horizontalPosition.x, head.transform.localPosition.y, horizontalPosition.y);
                }
            }

            if (tetherEar)
            {
                ear.gameObject.transform.SetPositionAndRotation(head.transform.position, head.transform.rotation);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            settingsChanged.Invoke();
        }

#endif

        // generates a mesh used for the preview or other uses
        public Mesh GenerateMesh()
        {
            Mesh mesh = new Mesh();
            // Have about one panel every 10 degrees. A reasonable number.
            int numPanels = Mathf.Max(1, (int)(cavernAngle / 10.0f));
            int numVertices = (numPanels + 1) * 2;

            Vector3[] positions = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] indices = new int[numPanels * 6];

            /********************************************** Generate inner surface. **********************************************/

            float cavernBottomHeight = cavernElevation;
            float cavernTopHeight = cavernHeight + cavernElevation;

            //float topUV = (previewEye == PreviewEye.Left) ? 1.0f : 0.5f;
            //float bottomUV = (previewEye == PreviewEye.Left) ? 0.5f : 0.0f;
            float topUV = 1.0f;
            float bottomUV = 0.5f;

            float deltaAngle = cavernAngle / (float)numPanels;

            // Create vertices of surface.
            for (int i = 0; i <= numPanels; i++)
            {
                float ratio = (float)i / (float)numPanels;
                float currAngle = (ratio - 0.5f) * cavernAngle;

                // Take note that angle 0 points down the Z-axis, not the X-axis.
                float directionX = Mathf.Sin(currAngle * Mathf.Deg2Rad);
                float directionZ = Mathf.Cos(currAngle * Mathf.Deg2Rad);

                positions[i * 2] = new Vector3(cavernRadius * directionX, cavernTopHeight, cavernRadius * directionZ); // Top vertex.
                normals[i * 2] = new Vector3(cavernRadius * directionX, 0.0f, cavernRadius * directionZ);
                uvs[i * 2] = new Vector2((float)i / (float)numPanels, topUV);

                positions[i * 2 + 1] = new Vector3(cavernRadius * directionX, cavernBottomHeight, cavernRadius * directionZ); // Top vertex.
                normals[i * 2 + 1] = new Vector3(cavernRadius * directionX, 0.0f, cavernRadius * directionZ);
                uvs[i * 2 + 1] = new Vector2((float)i / (float)numPanels, bottomUV);
            }

            // Assign indices of each panel.
            // Each panel is a quad made up of 2 triangles.
            // Unity uses a CLOCKWISE WINDING ORDER for its triangles.
            for (int i = 0; i < numPanels; ++i)
            {
                // Triangle 1
                indices[i * 6] = i * 2;
                indices[i * 6 + 1] = i * 2 + 2;
                indices[i * 6 + 2] = i * 2 + 1;

                // Triangle 2
                indices[i * 6 + 3] = i * 2 + 1;
                indices[i * 6 + 4] = i * 2 + 2;
                indices[i * 6 + 5] = i * 2 + 3;
            }

            mesh.name = "Cavern Mesh";
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;
            return mesh;
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Events;


namespace Spelunx
{
    [ExecuteInEditMode]
    public class Cavern3CamRenderer : MonoBehaviour
    {
        public enum StereoscopicMode
        {
            Mono, // Monoscopic mode. No 3D effect.
            Stereo, // Stereoscopic mode. Gives a 3D-movie effect when wearing 3D glasses.
        }

        public enum CubemapResolution
        {
            VeryLow = 512,
            Low = 1024,
            Mid = 2048,
            High = 4096,
            VeryHigh = 8192,
        }

        public enum PreviewEye { Left, Right }

        private enum CubemapIndex
        {
            North = 0, // Also used for monoscopic.
            South,
            East,
            West,

            Num,
        }

        [Header("Camera Settings")]
        /// Stereoscopic mode to render the 
        [SerializeField] private StereoscopicMode stereoMode = StereoscopicMode.Mono;
        [SerializeField] private CubemapResolution cubemapResolution = CubemapResolution.Mid;
        /// Interpupillary Distance (IPD) in metres.
        [SerializeField, Range(0.05f, 0.08f)] private float interpupillaryDistance = 0.065f;
        /// Cavern physical screen height in metres.
        [SerializeField, Min(0.1f)] private float cavernHeight = 2.0f;
        /// Cavern physical screen radius in metres.
        [SerializeField, Min(0.1f)] private float cavernRadius = 3.0f;
        /// Cavern physical screen angle in degrees.
        [SerializeField, Range(1.0f, 360.0f)] private float cavernAngle = 270.0f;
        /// Cavern physical screen elevation in metres, relative to the player's feet.
        [SerializeField, Range(-0.5f, 0.5f)] private float cavernElevation = 0.0f;
        /// Increase accuracy at the cost of significant performance.
        [SerializeField] private bool enableConvergence = false;
        /// Software support for swapping the left and right eyes. (Off - Left Eye On Top, On - Right Eye On Top)
        [SerializeField] private bool swapEyes = false;

        [Header("Head Tracking")]
        /// If set to true, the ear will follow the head.
        [SerializeField] private bool tetherEar = true;
        /// If set to true, the head position will be clamped to within the the radius of the screen.
        [SerializeField] private bool clampHeadPosition = true;
        /// <summary>
        /// Sets the clamping radius of the head, if clampHeadPosition = true. 
        /// For example, if clampHeadRatio = 0.8 and cavernRadius = 3, the head will be clamped to a radius of 2.4.
        /// </summary>
        [SerializeField, Range(0.0f, 1.0f)] private float clampHeadRatio = 0.9f;

        [Header("Preview")]
        [SerializeField] private CubemapResolution previewResolution = CubemapResolution.VeryLow;
        [SerializeField] private PreviewEye previewEye = PreviewEye.Left;
        [SerializeField, Tooltip("Should the CAVERN preview live update?")] private bool livePreview = true;

        [Header("References (Do NOT edit!)")]
        [SerializeField] private Transform head;
        [SerializeField] private Camera eye; // Ensure that UI culling mask is unset. Ensure that Output > Target Eye is set to None in the Inspector, or it'll render a blank screen on the Cavern PC! No I don't know why.
        [SerializeField] private Camera leftRenderCam; // Ensure that ONLY UI culling mask is set. Ensure that Output > Target Eye is set to None in the Inspector, or it'll render a blank screen on the Cavern PC! No I don't know why.
        [SerializeField] private Camera centerRenderCam; // Ensure that ONLY UI culling mask is set. Ensure that Output > Target Eye is set to None in the Inspector, or it'll render a blank screen on the Cavern PC! No I don't know why.
        [SerializeField] private Camera rightRenderCam; // Ensure that ONLY UI culling mask is set. Ensure that Output > Target Eye is set to None in the Inspector, or it'll render a blank screen on the Cavern PC! No I don't know why.
        [SerializeField] private Camera renderCam;
        [SerializeField] private AudioListener ear;
        [SerializeField] private Shader shader;
        [SerializeField] private Material previewMaterial;

        // Internal variables.
        private Material material = null;
        private RenderTexture[] cubemaps = null;
        private Mesh previewMesh = null;
        private RTHandle previewTexture = null;
        [SerializeField] private RenderTexture cavernRenderTexture = null;
        private RTHandle cavernRT = null;
        private CavernRenderPass cavernRenderPass;
        private Cavern3CamRenderPass leftCavernRenderPass;
        private Cavern3CamRenderPass centerCavernRenderPass;
        private Cavern3CamRenderPass rightCavernRenderPass;
        // private CavernPreviewRenderPass cavernPreviewRenderPass;
        private Material leftCavernRenderMat;
        private Material centerCavernRenderMat;
        private Material rightCavernRenderMat;
        [SerializeField] private Shader regionBlitter;

        [HideInInspector]
        public UnityEvent settingsChanged;

        public CubemapResolution GetCubemapResolution() { return cubemapResolution; }
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
        public float GetCavernHeight() { return cavernHeight; }
        public float GetCavernRadius() { return cavernRadius; }
        public float GetCavernAngle() { return cavernAngle; }
        public float GetCavernElevation() { return cavernElevation; }
        public float GetAspectRatio() { return ((cavernAngle / 360.0f) * Mathf.PI * cavernRadius * 2.0f) / cavernHeight; }
        public GameObject GetHead() { return head.gameObject; }
        public GameObject GetEye() { return eye.gameObject; }
        public GameObject GetEar() { return ear.gameObject; }

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

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
            UnityEditor.EditorApplication.delayCall += OnEditorDelayCall;
#endif
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
            UnityEditor.EditorApplication.delayCall -= OnEditorDelayCall;
#endif
        }

        private void OnDestroy()
        {
            cavernRT?.Release();
            previewTexture?.Release();
        }

        private void Awake()
        {
            CreateCubemaps();
            CreateMaterial();
            CreatePreviewMesh();
            CreatePreviewTexture();
            // cavernRT = RTHandles.Alloc(
            //     width: 5760,
            //     height: 2400,
            //     // depthBufferBits: DepthBits.Depth32,
            //     // colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
            //     colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.ARGB32, true),
            //     dimension: TextureDimension.Tex2D,
            //     wrapMode: TextureWrapMode.Clamp
            // );
            // RenderTextureDescriptor desc = cavernRenderTexture.descriptor;
            // desc.depthBufferBits = 0;
            // desc.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            // cavernRT = RTHandles.Alloc(desc);
            cavernRT = RTHandles.Alloc(cavernRenderTexture);
            cavernRenderPass = new CavernRenderPass(material);
            leftCavernRenderPass = new Cavern3CamRenderPass(leftCavernRenderMat, cavernRT);
            centerCavernRenderPass = new Cavern3CamRenderPass(centerCavernRenderMat, cavernRT);
            rightCavernRenderPass = new Cavern3CamRenderPass(rightCavernRenderMat, cavernRT);
#if UNITY_EDITOR
            // cavernPreviewRenderPass = new CavernPreviewRenderPass(previewTexture);
#endif
        }

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
                ear.gameObject.transform.position = head.transform.position;
                ear.gameObject.transform.rotation = head.transform.rotation;
            }

            //#if UNITY_EDITOR
            // Only render if we are playing and or showing a live preview.
            /*
            if (UnityEditor.EditorApplication.isPlaying || livePreview)
            {
                RenderEyes();
                if (previewTexture != null && material != null)
                {
                    Graphics.Blit(null, previewTexture, material);
                }
            }
            */
            //#else
            RenderEyes();
            //#endif
#if UNITY_EDITOR
            // Only manually render if we are not playing but showing a live preview.
            if (!UnityEditor.EditorApplication.isPlaying && livePreview)
            {
                // renderCam.Render();
                // UniversalRenderPipeline.StandardRequest request = new()
                // {
                //     destination = cavernRenderTexture
                // };
                // UniversalRenderPipeline.StandardRequest request = new();

                // RenderPipeline.SubmitRenderRequest(renderCam, request);

                // // Submit it to the active Render Pipeline
                // if (RenderPipelineManager.currentPipeline != null)
                // {
                //     RenderPipeline.SubmitRenderRequest(targetCamera, request);
                //     Debug.Log($"Manually forced URP to render: {targetCamera.name}");
                // }
            }
#endif
        }

        // Find out which faces of the cubemaps should be rendered. We want the minimum number of faces to reduce the rendering workload.
        // General approach: For front, back, left and right faces, look at the Cavern from the top-down view, so that it looks like a circle.
        // "Slice" the circle into 4 quadrants using 2 lines that form an X, with the player's head being the intersection of the 2 lines.
        // Then for each quadrant, determine which faces of each cubemap can be seen. Those are the faces we want to render.
        private void GetRenderFaces(out int monoMask, out int northMask, out int southMask, out int eastMask, out int westMask)
        {
            // These are the built in bitmasks for Unity's cubemap faces.
            const int rightMask = 1 << (int)CubemapFace.PositiveX;
            const int leftMask = 1 << (int)CubemapFace.NegativeX;
            const int topMask = 1 << (int)CubemapFace.PositiveY;
            const int bottomMask = 1 << (int)CubemapFace.NegativeY;
            const int frontMask = 1 << (int)CubemapFace.PositiveZ;
            const int backMask = 1 << (int)CubemapFace.NegativeZ;

            // Let's initalise all the output to 0.
            monoMask = 0; northMask = 0; southMask = 0; eastMask = 0; westMask = 0;

            Vector3 headPosition = head.transform.localPosition;

            /*
                Imagine this circle to be the Cavern screen. (Let's use a complete circle because this function should
                generalise to a circle of any angle, even though the Cavern is only 270 degrees.)
                         , - ~ ~ ~ - ,
                     , '               ' ,
                   ,                       ,
                  ,                         ,
                 ,                           ,
                 ,                           ,
                 ,                           ,
                  ,                         ,
                   ,                       ,
                     ,                  , '
                       ' - , _ _ _ ,  '

                // Now we want to "slice" the circle. I know my ASCII art is terrible, bear with me.
                // I put all my skill points into programming and have none left for art.

                                \        /
 North-West Boundary -> , - ~ ~  \ - ,  /
                     , '          \    / , <- North-East Boundary
                   ,               \  /    ,
                  ,                  O      , <- The intersection of the 2 lines is the head position. It can be off-centre.
                 ,                  / \      ,
                 ,                 /   \     ,
                 ,                /     \    ,
                  ,              /       \  ,
                   ,            /         \, <- South-East Boundary
                     ,         /        , '\
 South-West Boundary ->' - , _/_ _ ,  '     \
                             /               \

                // The circle is sliced into 4 quadrants, each being 90 degrees. (The ASCII art is not to scale. Just pretend it is.)
                // The places where the straight lines intersect with the circle are called boundaries (becauses I couldn't come up with a better name).
            */
            Vector3 southWestBoundary = Vector3.zero;
            Vector3 northEastBoundary = Vector3.zero;
            Vector3 northWestBoundary = Vector3.zero;
            Vector3 southEastBoundary = Vector3.zero;

            /*
            To find the boundaries, just remember our secondary school linear algebra.
            Note that our cubemaps are always taken from the head's position, we take the head to always be at (0, 0).
            Instead, we "move" the screen by -HeadPosition,

            Let (a, b) be the centre of the circle.
            Circle Equation: (x - a)^2 + (y - b)^2 = r^2.  ---- (1)
            South West to North East Line Equation: y = x  ---- (2)
            North West to South East Line Equation: y = -x ---- (3)

            Substitute (2) into (1):
            (x - a)^2 + (x - b)^2 = r^2
            x^2 - x(a + b) - 0.5(r^2 - a^2 - b^2) = 0
            Solve this quadratic equation to get our intersection points for the South-West to North-East line and the circle.

            Substitute (3) into (1):
            (x - a)^2 + (-x - b)^2 = r^2
            x^2 - x(a - b) - 0.5(r^2 - a^2 - b^2) = 0
            Solve this quadratic equation to get our intersection points for the North-West to South-East line and the circle.
            */
            // Get North-East and South-West boundaries where the sampled cubemap switches for stereoscopic rendering.
            List<float> xIntersectSouthWestToNorthEast = MathsUtil.SolveQuadraticEquation(
                1.0f,
                headPosition.x + headPosition.z,
                -0.5f * (cavernRadius * cavernRadius - headPosition.x * headPosition.x - headPosition.z * headPosition.z));

            // If there is only one solution to the quadratic equation, then there is only 1 point of intersection.
            if (xIntersectSouthWestToNorthEast.Count == 1)
            {
                northEastBoundary = new Vector3(xIntersectSouthWestToNorthEast[0], 0.0f, xIntersectSouthWestToNorthEast[0]);
                southWestBoundary = new Vector3(xIntersectSouthWestToNorthEast[0], 0.0f, xIntersectSouthWestToNorthEast[0]);
            }
            // Else there are 2 points of intersection.
            else if (xIntersectSouthWestToNorthEast.Count == 2)
            {
                northEastBoundary = new Vector3(xIntersectSouthWestToNorthEast[1], 0.0f, xIntersectSouthWestToNorthEast[1]);
                southWestBoundary = new Vector3(xIntersectSouthWestToNorthEast[0], 0.0f, xIntersectSouthWestToNorthEast[0]);
            }

            // Get North-West and South-East boundaries where the sampled cubemap switches for stereoscopic rendering.
            List<float> xIntersectNorthWestToSouthEast = MathsUtil.SolveQuadraticEquation(
                1.0f,
                headPosition.x - headPosition.z,
                -0.5f * (cavernRadius * cavernRadius - headPosition.x * headPosition.x - headPosition.z * headPosition.z));
            if (xIntersectNorthWestToSouthEast.Count == 1)
            {
                northWestBoundary = new Vector3(xIntersectNorthWestToSouthEast[0], 0.0f, -xIntersectNorthWestToSouthEast[0]);
                southEastBoundary = new Vector3(xIntersectNorthWestToSouthEast[0], 0.0f, -xIntersectNorthWestToSouthEast[0]);
            }
            else if (xIntersectNorthWestToSouthEast.Count == 2)
            {
                northWestBoundary = new Vector3(xIntersectNorthWestToSouthEast[0], 0.0f, -xIntersectNorthWestToSouthEast[0]);
                southEastBoundary = new Vector3(xIntersectNorthWestToSouthEast[1], 0.0f, -xIntersectNorthWestToSouthEast[1]);
            }

            // For edge cases, assume that the top and bottom faces are not visible.
            // It should be correct for most cases if the Cavern has sane dimensions.

            // Edge Case 1: Head is moved out of the screen area and there are no intersects.
            // This means that the screen is entirely in one quadrant relative to the head.
            if (xIntersectSouthWestToNorthEast.Count == 0 && xIntersectNorthWestToSouthEast.Count == 0)
            {
                /*
                 \     /
                  \   /
                   \ /
                    O <- Head (Not to scale.)
                   / \
                  /   \
                 /     \
                   --
                 |    | <- Screen (Not to scale.)
                   --
                 */
                // Screen is entirely south of the head.
                if (0.0f < headPosition.z &&
                    Mathf.Abs(headPosition.x) < Mathf.Abs(headPosition.z))
                {
                    monoMask |= backMask;
                    eastMask |= backMask;
                    westMask |= backMask;
                    return;
                }

                /*
                   --
                 |    | <- Screen (Not to scale.)
                   --
                 \     /
                  \   /
                   \ /
                    O <- Head (Not to scale.)
                   / \
                  /   \
                 /     \
                 */
                // Screen is entirely north of the head.
                if (headPosition.z < 0.0f &&
                    Mathf.Abs(headPosition.x) < Mathf.Abs(headPosition.z))
                {
                    monoMask |= frontMask;
                    eastMask |= frontMask;
                    westMask |= frontMask;
                    return;
                }

                // Screen is entirely east of the head. (No more drawings, you should get the point by now.)
                if (headPosition.x < 0.0f &&
                    Mathf.Abs(headPosition.z) < Mathf.Abs(headPosition.x))
                {
                    monoMask |= rightMask;
                    northMask |= rightMask;
                    southMask |= rightMask;
                    return;
                }

                // Screen is entirely west of the head.
                if (headPosition.x > 0.0f &&
                    Mathf.Abs(headPosition.z) < Mathf.Abs(headPosition.x))
                {
                    monoMask |= leftMask;
                    northMask |= leftMask;
                    southMask |= leftMask;
                    return;
                }
            }

            // Edge Case 2: Head is moved out of the screen and only the South-West to North-East line intersects.
            if (xIntersectSouthWestToNorthEast.Count > 0 && xIntersectNorthWestToSouthEast.Count == 0)
            {
                /*
                     \     /
                      \   /
                       \ /
                        O <- Head (Not to scale.)
                       / \
                      /   \
                     /     \
                    /
                  --
                | /  | <- Screen (Not to scale.)
                 /--
                /
                */
                // Screen is entirely south-west of the head.
                if (Vector3.Dot(new Vector3(1.0f, 1.0f), new Vector2(headPosition.x, headPosition.z)) > 1.0f)
                {
                    monoMask |= (backMask | leftMask);
                    eastMask |= backMask;
                    westMask |= backMask;
                    northMask |= leftMask;
                    southMask |= leftMask;
                    return;
                }

                /*
                            /
                          --
                        | /  | <- Screen (Not to scale.)
                         /--
                        /
                 \     /
                  \   /
                   \ /
                    O <- Head (Not to scale.)
                   / \
                  /   \
                 /     \
                 */
                // Screen is entirely north-east of the head.
                if (Vector3.Dot(new Vector3(-1.0f, -1.0f), new Vector2(headPosition.x, headPosition.z)) > 1.0f)
                {
                    monoMask = (frontMask | rightMask);
                    eastMask |= frontMask;
                    westMask |= frontMask;
                    northMask |= rightMask;
                    southMask |= rightMask;
                    return;
                }
            }

            // Edge Case 3: Head is moved out of the screen and only the North-West to South-East line intersects.
            if (xIntersectSouthWestToNorthEast.Count == 0 && xIntersectNorthWestToSouthEast.Count > 0)
            {
                // Screen is entirely north-west of the head. (Imagine the above drawings but for the North-West to South-East line.)
                if (Vector3.Dot(new Vector3(1.0f, -1.0f), new Vector2(headPosition.x, headPosition.z)) > 1.0f)
                {
                    monoMask = (frontMask | leftMask);
                    eastMask |= frontMask;
                    westMask |= frontMask;
                    northMask |= leftMask;
                    southMask |= leftMask;
                    return;
                }

                // Screen is entirely south-east of the head.
                if (Vector3.Dot(new Vector3(-1.0f, 1.0f), new Vector2(headPosition.x, headPosition.z)) > 1.0f)
                {
                    monoMask = (backMask | rightMask);
                    eastMask |= backMask;
                    westMask |= backMask;
                    northMask |= rightMask;
                    southMask |= rightMask;
                    return;
                }
            }

            // Regular Case: The head is within the screen area.
            // Take note that if we want more accurate rendering, that is to have the 2 eyes converge, more faces need to be rendered.
            // Personally I don't notice much difference in terms of accuracy in real world experience, but it does cost quite a bit of performance.
            // Therefore I added a toggle for it, and set it to false by default.
            float screenTop = cavernElevation + cavernHeight - headPosition.y;
            float screenBottom = cavernElevation - headPosition.y;
            Vector3 headOffset = new Vector3(headPosition.x, 0.0f, headPosition.z);

            /******************* Looking North *******************/
            monoMask |= frontMask;
            // For the enableConvergence = true case, much of the face cannot be seen. In the future, we can consider using a stencil buffer to disable rendering on the unseen portions of the face.
            westMask |= frontMask | (enableConvergence ? rightMask : 0); // Left Eye
            eastMask |= frontMask | (enableConvergence ? leftMask : 0); // Right Eye

            /******************* Looking South *******************/
            if (Vector3.Angle(headOffset + southWestBoundary, Vector3.forward) < cavernAngle * 0.5f ||
                Vector3.Angle(headOffset + southEastBoundary, Vector3.forward) < cavernAngle * 0.5f)
            {
                monoMask |= backMask;
                eastMask |= backMask; // Left Eye, no need to account for convergence because that is already handled in 'Looking North'.
                westMask |= backMask; // Right Eye, no need to account for convergence because that is already handled in 'Looking North'.
            }

            /******************* Looking East *******************/
            if (Vector3.Angle(headOffset + northEastBoundary, Vector3.forward) < cavernAngle * 0.5f ||
                Vector3.Angle(headOffset + southEastBoundary, Vector3.forward) < cavernAngle * 0.5f)
            {
                monoMask |= rightMask;
                northMask |= rightMask | (enableConvergence ? backMask : 0); // Left Eye
                southMask |= rightMask | (enableConvergence ? frontMask : 0); // Right Eye
            }

            /******************* Looking West *******************/
            if (Vector3.Angle(headOffset + northWestBoundary, Vector3.forward) < cavernAngle * 0.5f ||
                Vector3.Angle(headOffset + southWestBoundary, Vector3.forward) < cavernAngle * 0.5f)
            {
                monoMask |= leftMask;
                southMask |= leftMask | (enableConvergence ? frontMask : 0); // Left Eye
                northMask |= leftMask | (enableConvergence ? backMask : 0); // Right Eye
            }

            /******************* Top & Bottom Faces *******************/
            /*
            Cubemap rendered from eye's position:
                  ----------------------------------
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |             (-0-)              |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  ----------------------------------

            Find the horizontal distance from the eye to the screen.
            Find the vertical distance from the eye to the top of the screen.
                                                  ^| (Top of Screen)
                                                 / |
                                                /  |
                                               /   |
                                              /    |
                                             /     |
                                            /      |
                                           /       | Vertical
                                          /        |
                                         /         |
                                        /          |
                                       /           |
                                      v            |
                                (-0-) <----------->O
                                        Horizontal |
                                  ^                |
                                  |                |
                            Badly Drawn Eye        |
                                                   |
                                                   |
                                                   |
                                                   |

            Top of cubemap needs to be rendered if the horizontal distance is smaller than the vertical distance:
                                                  ^|
                                                 / |
                                                /  |
                                               /   |
                                              /    |
                                             /     |
                  --------------------------/-------
                  |                        /       |
                  |                       /        |
                  |                      /         |
                  |                     /          |
                  |                    /           |
                  |                   v            |
                  |             (-0-) <----------->O
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  ----------------------------------


            Find the horizontal distance from the eye to the screen.
            Find the vertical distance from the eye to the bottom of the screen. (May be different from the distance to the top of the screen!)
                                                   |
                                                   |
                                                   |
                                                   |
                                                   |
                                                   |
                                                   |
                                                   | 
                                                   |
                                                   |
                                                   |
                                                   |
                                        Horizontal |
                                (-0-) <----------->O
                                      ^            |
                                  ^    \           |
                                  |      \         | Vertical
                            Badly Drawn Eye        |
                                            \      |
                                              \    |
                                                \  |
                                                  v|

            Bottom of cubemap needs to be rendered if the horizontal distance is smaller than the vertical distance:
                                                   |
                                                   |
                                                   |
                                                   |
                                                   |
                                                   |
                  ----------------------------------
                  |                                | 
                  |                                |
                  |                                |
                  |                                |
                  |                                |
                  |                     Horizontal |
                  |             (-0-) <----------->O
                  |                   ^            |
                  |                    \           |
                  |                      \         | Vertical
                  |                        \       |
                  |                         \      |
                  |                           \    |
                  ------------------------------\--|
                                                  v|

            */
            if (Mathf.Abs(northEastBoundary.z) < Mathf.Abs(screenTop) || // Looking North
                Mathf.Abs(northWestBoundary.z) < Mathf.Abs(screenTop) || // Looking North
                Mathf.Abs(southEastBoundary.z) < Mathf.Abs(screenTop) || // Looking South
                Mathf.Abs(southWestBoundary.z) < Mathf.Abs(screenTop))
            { // Looking South
                monoMask |= topMask;
                eastMask |= topMask;
                westMask |= topMask;
            }
            if (Mathf.Abs(northEastBoundary.z) < Mathf.Abs(screenBottom) || // Looking North
                Mathf.Abs(northWestBoundary.z) < Mathf.Abs(screenBottom) || // Looking North
                Mathf.Abs(southEastBoundary.z) < Mathf.Abs(screenBottom) || // Looking South
                Mathf.Abs(southWestBoundary.z) < Mathf.Abs(screenBottom))
            { // Looking South
                monoMask |= bottomMask;
                eastMask |= bottomMask;
                westMask |= bottomMask;
            }
            if (Mathf.Abs(northEastBoundary.x) < Mathf.Abs(screenTop) || // Looking East
                Mathf.Abs(southEastBoundary.x) < Mathf.Abs(screenTop) || // Looking East
                Mathf.Abs(northWestBoundary.x) < Mathf.Abs(screenTop) || // Looking West
                Mathf.Abs(southWestBoundary.x) < Mathf.Abs(screenTop))
            { // Looking West
                monoMask |= topMask;
                northMask |= topMask;
                southMask |= topMask;
            }
            if (Mathf.Abs(northEastBoundary.x) < Mathf.Abs(screenBottom) || // Looking East
                Mathf.Abs(southEastBoundary.x) < Mathf.Abs(screenBottom) || // Looking East
                Mathf.Abs(northWestBoundary.x) < Mathf.Abs(screenBottom) || // Looking West
                Mathf.Abs(southWestBoundary.x) < Mathf.Abs(screenBottom))
            { // Looking West
                monoMask |= bottomMask;
                northMask |= bottomMask;
                southMask |= bottomMask;
            }
        }

        private void RenderEyes()
        {
            // Use Camera.MonoOrStereoscopicEye.Left or Camera.MonoOrStereoscopicEye.Right to ensure that the cubemap follows the camera's rotation.
            // Camera.MonoOrStereoscopicEye.Mono renders the cubemap to be aligned to the world's axes instead.
            int monoMask = 0; int northMask = 0; int southMask = 0; int eastMask = 0; int westMask = 0;
            GetRenderFaces(out monoMask, out northMask, out southMask, out eastMask, out westMask);
            switch (stereoMode)
            {
                case StereoscopicMode.Mono:
                    eye.stereoSeparation = 0.0f;
                    eye.transform.rotation = gameObject.transform.rotation; // Set eye's global orientation to the screen's orientation, regardless of the head's orientation.
                    eye.transform.localPosition = Vector3.zero;
                    eye.RenderToCubemap(cubemaps[(int)CubemapIndex.North], monoMask, Camera.MonoOrStereoscopicEye.Left);
                    break;
                case StereoscopicMode.Stereo:
                    eye.stereoSeparation = 0.0f;
                    eye.transform.rotation = gameObject.transform.rotation; // Set eye's global orientation to the screen's orientation, regardless of the head's orientation.
                    eye.transform.localPosition = new Vector3(0.0f, 0.0f, interpupillaryDistance * 0.5f);
                    eye.RenderToCubemap(cubemaps[(int)CubemapIndex.North], northMask, Camera.MonoOrStereoscopicEye.Left);
                    eye.transform.localPosition = new Vector3(0.0f, 0.0f, interpupillaryDistance * -0.5f);
                    eye.RenderToCubemap(cubemaps[(int)CubemapIndex.South], southMask, Camera.MonoOrStereoscopicEye.Right);
                    eye.transform.localPosition = new Vector3(interpupillaryDistance * 0.5f, 0.0f, 0.0f);
                    eye.RenderToCubemap(cubemaps[(int)CubemapIndex.East], eastMask, Camera.MonoOrStereoscopicEye.Right);
                    eye.transform.localPosition = new Vector3(interpupillaryDistance * -0.5f, 0.0f, 0.0f);
                    eye.RenderToCubemap(cubemaps[(int)CubemapIndex.West], westMask, Camera.MonoOrStereoscopicEye.Left);
                    eye.transform.localPosition = Vector3.zero;
                    break;
            }

            // Cavern Dimensions Uniforms
            material.SetFloat("_CavernHeight", cavernHeight);
            material.SetFloat("_CavernRadius", cavernRadius);
            material.SetFloat("_CavernAngle", cavernAngle);
            material.SetFloat("_CavernElevation", cavernElevation);

            // Head Tracking Uniforms
            material.SetVector("_HeadPosition", head.transform.localPosition);

            // Stereoscopic Rendering Uniforms
            material.SetInteger("_EnableStereoscopic", stereoMode == StereoscopicMode.Stereo ? 1 : 0);
            material.SetInteger("_EnableConvergence", enableConvergence ? 1 : 0);
            material.SetFloat("_InterpupillaryDistance", interpupillaryDistance);
            material.SetInteger("_SwapEyes", swapEyes ? 1 : 0);
        }

        private void CreateCubemaps()
        {
            cubemaps = new RenderTexture[(int)CubemapIndex.Num];
            for (int i = 0; i < (int)CubemapIndex.Num; ++i)
            {
                cubemaps[i] = new RenderTexture((int)cubemapResolution, (int)cubemapResolution, 32, RenderTextureFormat.ARGB32)
                {
                    dimension = TextureDimension.Cube,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        private void CreateMaterial()
        {
            material = new Material(shader);
            material.SetTexture("_CubemapNorth", cubemaps[(int)CubemapIndex.North]);
            material.SetTexture("_CubemapSouth", cubemaps[(int)CubemapIndex.South]);
            material.SetTexture("_CubemapEast", cubemaps[(int)CubemapIndex.East]);
            material.SetTexture("_CubemapWest", cubemaps[(int)CubemapIndex.West]);
            cavernRenderPass?.SetMaterial(material);

            leftCavernRenderMat = new Material(regionBlitter);
            leftCavernRenderMat.SetVector("_Region", new Vector4(0, 0, 1.0f / 3, 1));
            leftCavernRenderMat.SetTexture("_BaseTex", cavernRenderTexture);
            leftCavernRenderPass?.SetMaterial(leftCavernRenderMat);
            centerCavernRenderMat = new Material(regionBlitter);
            centerCavernRenderMat.SetVector("_Region", new Vector4(1.0f / 3, 0, 1.0f / 3, 1));
            centerCavernRenderMat.SetTexture("_BaseTex", cavernRenderTexture);
            centerCavernRenderPass?.SetMaterial(centerCavernRenderMat);
            rightCavernRenderMat = new Material(regionBlitter);
            rightCavernRenderMat.SetVector("_Region", new Vector4(2.0f / 3, 0, 1.0f / 3, 1));
            rightCavernRenderMat.SetTexture("_BaseTex", cavernRenderTexture);
            rightCavernRenderPass?.SetMaterial(rightCavernRenderMat);
        }

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

            float topUV = (previewEye == PreviewEye.Left) ? 1.0f : 0.5f;
            float bottomUV = (previewEye == PreviewEye.Left) ? 0.5f : 0.0f;

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

        /// \*brief
        /// Generate a curved screen mesh.
        /// \*warning Ensure that the mesh's material disables back-face culling!
        private void CreatePreviewMesh()
        {
            previewMesh = GenerateMesh();
            previewMesh.name = "Cavern Preview Mesh";

        }

        private void CreatePreviewTexture()
        {
            previewTexture = RTHandles.Alloc(
                width: (int)previewResolution,
                height: (int)previewResolution,
                // depthBufferBits: DepthBits.Depth32,
                // colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.ARGB32, true),
                dimension: TextureDimension.Tex2D,
                wrapMode: TextureWrapMode.Clamp
            );
            // previewTexture = new RenderTexture((int)previewResolution, (int)previewResolution, 32, RenderTextureFormat.ARGB32);
            // previewTexture.dimension = TextureDimension.Tex2D;
            // previewTexture.wrapMode = TextureWrapMode.Clamp;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // "Highjack" the GUI camera insert a render pass into the URP RenderGraph to render the output.
            if (camera == renderCam)
            {
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(cavernRenderPass);
            }
            else if (camera == leftRenderCam)
            {
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(leftCavernRenderPass);
            }
            else if (camera == centerRenderCam)
            {
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(centerCavernRenderPass);
            }
            else if (camera == rightRenderCam)
            {
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(rightCavernRenderPass);
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) { }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // This method is called whenever a setting is changed in the inspector, or at the beginning of scene mode rendering.
            // If any of the Cavern size settings are changed, we need to regenerate the mesh.
            CreatePreviewMesh();
            settingsChanged.Invoke();
        }

        // The cubemap render targets get cleaned up by Unity's garbage collector on scene save or assembly reload. The material needs to have it's texture references restored. 
        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            CreateMaterial();
        }

        private void OnEditorDelayCall()
        {
            CreateMaterial();
        }

        private void OnDrawGizmos()
        {
            if (!enabled) return;
            if (previewMaterial == null)
            {
                Debug.LogAssertion("Cavern3CamRenderer: Preview material cannot be null!");
            }
            previewMaterial.SetPass(0);
            previewMaterial.mainTexture = livePreview ? previewTexture : null;

            // We need to use Graphics.DrawMeshNow instead of Gizmos.DrawMesh so we can get a texture on it.
            Graphics.DrawMeshNow(previewMesh, transform.position, transform.rotation);
        }
#endif
    }
}

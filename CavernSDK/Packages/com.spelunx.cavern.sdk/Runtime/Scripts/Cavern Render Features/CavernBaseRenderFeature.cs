using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Spelunx
{
    [AddComponentMenu("Cavern/Render Features/CAVERN Base Render Feature")]
    public class CavernBaseRenderFeature : CavernFeature, ICavernRenderFeature
    {
        [SerializeField] private Shader cubeMapRenderShader;
        [SerializeField] private CubemapResolution cubemapResolution = CubemapResolution.Mid;
        [SerializeField] private Camera eyeCamera;
        private Material material;
        private RenderTexture[] cubemaps = null;

        CavernBaseRenderPass m_ScriptablePass;

        private Camera renderCam;

        public enum CubemapResolution
        {
            VeryLow = 512,
            Low = 1024,
            Mid = 2048,
            High = 4096,
            VeryHigh = 8192,
        }

        private enum CubemapIndex
        {
            North = 0, // Also used for monoscopic.
            South,
            East,
            West,

            Num,
        }

        /// <inheritdoc/>
        void OnEnable()
        {
            renderCam = cavernSetup.RenderCamera;
            CreateCubemaps();
            CreateMaterial();
            m_ScriptablePass = new CavernBaseRenderPass(material);
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
            UnityEditor.EditorApplication.delayCall += OnEditorDelayCall;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
            UnityEditor.EditorApplication.delayCall -= OnEditorDelayCall;
#endif
        }

        private void CreateCubemaps()
        {
            cubemaps = new RenderTexture[(int)CubemapIndex.Num];
            for (int i = 0; i < (int)CubemapIndex.Num; ++i)
            {
                cubemaps[i] = new RenderTexture((int)cubemapResolution, (int)cubemapResolution, 32, RenderTextureFormat.ARGBHalf)
                {
                    dimension = TextureDimension.Cube,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        private void CreateMaterial()
        {
            material = new Material(cubeMapRenderShader);
            material.SetTexture("_CubemapNorth", cubemaps[(int)CubemapIndex.North]);
            material.SetTexture("_CubemapSouth", cubemaps[(int)CubemapIndex.South]);
            material.SetTexture("_CubemapEast", cubemaps[(int)CubemapIndex.East]);
            material.SetTexture("_CubemapWest", cubemaps[(int)CubemapIndex.West]);
            m_ScriptablePass?.SetMaterial(material);
        }

        private void SetMaterialProperties()
        {
            // Cavern Dimensions Uniforms
            material.SetFloat("_CavernHeight", cavernSetup.CavernHeight);
            material.SetFloat("_CavernRadius", cavernSetup.CavernRadius);
            material.SetFloat("_CavernAngle", cavernSetup.CavernAngle);
            material.SetFloat("_CavernElevation", cavernSetup.CavernElevation);

            // Stereoscopic Rendering Uniforms
            material.SetInteger("_EnableStereoscopic", cavernSetup.GetStereoscopicMode() == CavernSetup.StereoscopicMode.Stereo ? 1 : 0);
            material.SetInteger("_EnableConvergence", cavernSetup.Convergence ? 1 : 0);
            material.SetFloat("_InterpupillaryDistance", cavernSetup.IPD);
            material.SetInteger("_SwapEyes", cavernSetup.SwapEyes ? 1 : 0);
        }


        // Find out which faces of the cubemaps should be rendered. We want the minimum number of faces to reduce the rendering workload.
        // General approach: For front, back, left and right faces, look at the Cavern from the top-down view, so that it looks like a circle.
        // "Slice" the circle into 4 quadrants using 2 lines that form an X, with the player's head being the intersection of the 2 lines.
        // Then for each quadrant, determine which faces of each cubemap can be seen. Those are the faces we want to render.
        private void GetRenderFaces(CavernSetup cavernSetup, out int monoMask, out int northMask, out int southMask, out int eastMask, out int westMask)
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

            Vector3 headPosition = cavernSetup.GetHead().transform.localPosition;

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
                -0.5f * (cavernSetup.CavernRadius * cavernSetup.CavernRadius - headPosition.x * headPosition.x - headPosition.z * headPosition.z));

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
                -0.5f * (cavernSetup.CavernRadius * cavernSetup.CavernRadius - headPosition.x * headPosition.x - headPosition.z * headPosition.z));
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
            float screenTop = cavernSetup.CavernElevation + cavernSetup.CavernHeight - headPosition.y;
            float screenBottom = cavernSetup.CavernElevation - headPosition.y;
            Vector3 headOffset = new Vector3(headPosition.x, 0.0f, headPosition.z);

            /******************* Looking North *******************/
            monoMask |= frontMask;
            // For the enableConvergence = true case, much of the face cannot be seen. In the future, we can consider using a stencil buffer to disable rendering on the unseen portions of the face.
            westMask |= frontMask | (cavernSetup.Convergence ? rightMask : 0); // Left Eye
            eastMask |= frontMask | (cavernSetup.Convergence ? leftMask : 0); // Right Eye

            /******************* Looking South *******************/
            if (Vector3.Angle(headOffset + southWestBoundary, Vector3.forward) < cavernSetup.CavernAngle * 0.5f ||
                Vector3.Angle(headOffset + southEastBoundary, Vector3.forward) < cavernSetup.CavernAngle * 0.5f)
            {
                monoMask |= backMask;
                eastMask |= backMask; // Left Eye, no need to account for convergence because that is already handled in 'Looking North'.
                westMask |= backMask; // Right Eye, no need to account for convergence because that is already handled in 'Looking North'.
            }

            /******************* Looking East *******************/
            if (Vector3.Angle(headOffset + northEastBoundary, Vector3.forward) < cavernSetup.CavernAngle * 0.5f ||
                Vector3.Angle(headOffset + southEastBoundary, Vector3.forward) < cavernSetup.CavernAngle * 0.5f)
            {
                monoMask |= rightMask;
                northMask |= rightMask | (cavernSetup.Convergence ? backMask : 0); // Left Eye
                southMask |= rightMask | (cavernSetup.Convergence ? frontMask : 0); // Right Eye
            }

            /******************* Looking West *******************/
            if (Vector3.Angle(headOffset + northWestBoundary, Vector3.forward) < cavernSetup.CavernAngle * 0.5f ||
                Vector3.Angle(headOffset + southWestBoundary, Vector3.forward) < cavernSetup.CavernAngle * 0.5f)
            {
                monoMask |= leftMask;
                southMask |= leftMask | (cavernSetup.Convergence ? frontMask : 0); // Left Eye
                northMask |= leftMask | (cavernSetup.Convergence ? backMask : 0); // Right Eye
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

        // The cubemap render targets get cleaned up by Unity's garbage collector on scene save or assembly reload. The material needs to have it's texture references restored. 
        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            CreateMaterial();
        }

        private void OnEditorDelayCall()
        {
            CreateMaterial();
        }


        private void RenderEyes()
        {
            // Use Camera.MonoOrStereoscopicEye.Left or Camera.MonoOrStereoscopicEye.Right to ensure that the cubemap follows the camera's rotation.
            // Camera.MonoOrStereoscopicEye.Mono renders the cubemap to be aligned to the world's axes instead.
            int monoMask = 0; int northMask = 0; int southMask = 0; int eastMask = 0; int westMask = 0;
            GetRenderFaces(cavernSetup, out monoMask, out northMask, out southMask, out eastMask, out westMask);
            switch (cavernSetup.GetStereoscopicMode())
            {
                case CavernSetup.StereoscopicMode.Mono:
                    eyeCamera.stereoSeparation = 0.0f;
                    eyeCamera.transform.rotation = gameObject.transform.rotation; // Set eye's global orientation to the screen's orientation, regardless of the head's orientation.
                    eyeCamera.transform.localPosition = Vector3.zero;
                    eyeCamera.RenderToCubemap(cubemaps[(int)CubemapIndex.North], monoMask, Camera.MonoOrStereoscopicEye.Left);
                    // RenderToCubemap(eyeCamera, cubemaps[(int)CubemapIndex.North], monoMask, Camera.MonoOrStereoscopicEye.Left);
                    break;
                case CavernSetup.StereoscopicMode.Stereo:
                    eyeCamera.stereoSeparation = 0.0f;
                    eyeCamera.transform.rotation = gameObject.transform.rotation; // Set eye's global orientation to the screen's orientation, regardless of the head's orientation.
                    eyeCamera.transform.localPosition = new Vector3(0.0f, 0.0f, cavernSetup.IPD * 0.5f);
                    eyeCamera.RenderToCubemap(cubemaps[(int)CubemapIndex.North], northMask, Camera.MonoOrStereoscopicEye.Left);
                    // RenderToCubemap(eyeCamera, cubemaps[(int)CubemapIndex.North], northMask, Camera.MonoOrStereoscopicEye.Left);
                    eyeCamera.transform.localPosition = new Vector3(0.0f, 0.0f, cavernSetup.IPD * -0.5f);
                    eyeCamera.RenderToCubemap(cubemaps[(int)CubemapIndex.South], southMask, Camera.MonoOrStereoscopicEye.Right);
                    // RenderToCubemap(eyeCamera, cubemaps[(int)CubemapIndex.South], southMask, Camera.MonoOrStereoscopicEye.Right);
                    eyeCamera.transform.localPosition = new Vector3(cavernSetup.IPD * 0.5f, 0.0f, 0.0f);
                    eyeCamera.RenderToCubemap(cubemaps[(int)CubemapIndex.East], eastMask, Camera.MonoOrStereoscopicEye.Right);
                    // RenderToCubemap(eyeCamera, cubemaps[(int)CubemapIndex.East], eastMask, Camera.MonoOrStereoscopicEye.Right);
                    eyeCamera.transform.localPosition = new Vector3(cavernSetup.IPD * -0.5f, 0.0f, 0.0f);
                    eyeCamera.RenderToCubemap(cubemaps[(int)CubemapIndex.West], westMask, Camera.MonoOrStereoscopicEye.Left);
                    // RenderToCubemap(eyeCamera, cubemaps[(int)CubemapIndex.West], westMask, Camera.MonoOrStereoscopicEye.Left);
                    eyeCamera.transform.localPosition = Vector3.zero;
                    break;
            }

            // Head Tracking Uniforms
            material.SetVector("_HeadPosition", cavernSetup.GetHead().transform.localPosition);

            SetMaterialProperties();
        }
        readonly CubemapFace[] faces = new[] {
                CubemapFace.NegativeX, CubemapFace.PositiveX,
                CubemapFace.NegativeY, CubemapFace.PositiveY,
                CubemapFace.NegativeZ, CubemapFace.PositiveZ
            };
        // TODO: this is unused for now, but the existing Camera.renderToCubemap might be deprecated
        // TODO: the colors of this render differently than Camera.renderToCubemap
        private void RenderToCubemap(Camera eyeCam, RenderTexture cubemapTarget, int mask, Camera.MonoOrStereoscopicEye stereoEye)
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest()
            {
                destination = cubemapTarget
            };

            eyeCamera.transform.rotation = gameObject.transform.rotation;
            foreach (var face in faces)
            {
                if ((mask & (1 << (int)face)) != 0)
                {
                    request.face = face;
                    eyeCam.transform.localRotation = GetCubemapFaceRotation(face);
                    // Render camera and fill face of cubeMap with its view
                    UniversalRenderPipeline.SubmitRenderRequest(eyeCam, request);
                }
            }
            eyeCamera.transform.rotation = gameObject.transform.rotation;
        }

        // TODO: I think these values are currently wrong
        Quaternion GetCubemapFaceRotation(CubemapFace face)
        {
            return face switch
            {
                CubemapFace.PositiveX => Quaternion.LookRotation(Vector3.right, Vector3.down),
                CubemapFace.NegativeX => Quaternion.LookRotation(Vector3.left, Vector3.down),
                CubemapFace.PositiveY => Quaternion.LookRotation(Vector3.up, Vector3.forward),
                CubemapFace.NegativeY => Quaternion.LookRotation(Vector3.down, Vector3.back),
                CubemapFace.PositiveZ => Quaternion.LookRotation(Vector3.forward, Vector3.down),
                CubemapFace.NegativeZ => Quaternion.LookRotation(Vector3.back, Vector3.down),
                _ => Quaternion.identity,
            };
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        // public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        // {
        //     // var request = new RenderPipeline.StandardRequest { destination = targetCubemap };
        //     // if (RenderPipeline.SupportsRenderRequest(eyeCamera, request))
        //     // {
        //     //     RenderPipeline.SubmitRenderRequest(eyeCamera, request);
        //     // }
        //     renderer.EnqueuePass(m_ScriptablePass);
        // }


        // public void EnqueuePass(ScriptableRenderer renderer, ref RenderingData renderingData)
        // {
        //     renderer.EnqueuePass(m_ScriptablePass);
        // }

        public void EnqueuePass(ScriptableRenderContext context, Camera camera)
        {
            if (camera == renderCam)
            {
                RenderEyes();
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_ScriptablePass);
            }
        }

        class CavernBaseRenderPass : ScriptableRenderPass
        {
            private Material blitMaterial;
            const string name = "CavernBaseRenderPass";

            class PassData
            {
                public Material material;
            }

            public CavernBaseRenderPass(Material blitMaterial)
            {
                this.blitMaterial = blitMaterial;
                this.requiresIntermediateTexture = true; // TODO: was false
                this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            public void SetMaterial(Material m)
            {
                blitMaterial = m;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                /*
                using var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData);
                var resourceData = frameData.Get<UniversalResourceData>();

                passData.material = blitMaterial;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
                });

                // resourceData.cameraColor = resourceData.activeColorTexture;
                */

                var resourceData = frameData.Get<UniversalResourceData>();
                var source = resourceData.activeColorTexture;


                // We blit from the source to the source, just so we can overlay with transparent stuff
                RenderGraphUtils.BlitMaterialParameters para = new(source, source, blitMaterial, 0);
                // Add blit pass.
                renderGraph.AddBlitPass(para, passName: name);
            }
        }
    }
}
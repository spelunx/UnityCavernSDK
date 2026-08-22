using UnityEngine;
using UnityEngine.Rendering;

namespace Spelunx
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class CavernRoundWorldSpaceUIFeature : CavernFeature
    {
        [SerializeField, Tooltip("Distance from the screen to render. 0 is purely at the center, 1 is at the boundry"), Min(0)]
        private float distance = 1.0f;

        [SerializeField, Tooltip("Should the round canvas be automatically positioned around the CAVERN?")]
        private bool autoposition = true;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Material baseUIRenderMaterial;
        private RenderTexture uiRenderTexture;
        private Material uiRenderMat;
        private bool shouldUpdateMesh = false;
        private Mesh mesh;

        // void Create()
        // {
        //     CavernSetup cavernSetup = FindFirstObjectByType<CavernSetup>();

        //     // load from path
        //     GameObject cavernUIPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/CavernUI.prefab", typeof(GameObject));
        //     GameObject cavernUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(cavernUIPrefab as GameObject);

        //     GameObject roundCavernMeshRendererPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/RoundCavernMeshRenderer.prefab", typeof(GameObject));
        //     GameObject roundCavernMeshRendererInstance = (GameObject)PrefabUtility.InstantiatePrefab(roundCavernMeshRendererPrefab as GameObject);

        //     WorldSpaceMeshCanvas meshCanvas = roundCavernMeshRendererInstance.GetComponent<WorldSpaceMeshCanvas>();
        //     meshCanvas.SetCavernRenderer(cavernSetup);

        //     // set default parameters of roundUI mesh
        //     meshCanvas.transform.parent = cavernSetup.transform;
        //     meshCanvas.transform.localPosition = Vector3.zero;
        //     meshCanvas.transform.localRotation = Quaternion.identity;

        //     // mark scene as edited to prompt saving
        //     EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        // }

        private void CreateRenderTexture()
        {
            if (uiRenderTexture == null)
            {
                uiRenderTexture = new RenderTexture(width: cavernSetup.ScreenDimensions.x, height: cavernSetup.ScreenDimensions.y, depth: 24, RenderTextureFormat.ARGB32)
                {
                    dimension = TextureDimension.Tex2D,
                    wrapMode = TextureWrapMode.Clamp
                };
                uiRenderTexture.Create();
                if (baseUIRenderMaterial != null)
                {
                    uiRenderMat = new Material(baseUIRenderMaterial)
                    {
                        mainTexture = uiRenderTexture,
                    };
                    GetComponent<MeshRenderer>().material = uiRenderMat;
                }
            }

            if (uiCamera != null)
            {
                uiCamera.targetTexture = uiRenderTexture;
            }
        }

        void Start()
        {
            CreateRenderTexture();
            UpdateMesh();
            cavernSetup.settingsChanged.AddListener(() => shouldUpdateMesh = true);
        }

        void Update()
        {
            if (shouldUpdateMesh)
            {
                CreateRenderTexture();
                UpdateMesh();
                shouldUpdateMesh = false;
            }
            if (autoposition)
            {
                // center the mesh on the cavern's center by moving the y position down based on the difference in cavern height vs mesh height
                float yOffset = -cavernSetup.CavernHeight * (distance - 1) / 2;
                transform.SetLocalPositionAndRotation(new Vector3(0, yOffset, 0), Quaternion.identity);
                // transform.localPosition = new(transform.localPosition.x, yOffset, transform.localPosition.z);
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

        public override void OnValidate()
        {
            base.OnValidate();
            shouldUpdateMesh = true;
        }

    }
}
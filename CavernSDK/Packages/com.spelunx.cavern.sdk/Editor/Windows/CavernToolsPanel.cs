using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Spelunx
{
    public class CavernToolsPanel : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        // places tools under CAVERN toolbar with hierarchy ordering
        [MenuItem("CAVERN/CAVERN Tools", false, 100)]
        public static void ShowWindow()
        {
            CavernToolsPanel wnd = GetWindow<CavernToolsPanel>();
            wnd.titleContent = new GUIContent("CAVERN Tools");
        }

        public void CreateGUI()
        {
            // root VisualElement object of editor window
            VisualElement root = rootVisualElement;

            // Instantiate UXML, UI setup in UXML document
            VisualElement panelSetup = m_VisualTreeAsset.Instantiate();
            root.Add(panelSetup);

            VisualElement roundUI = root.Q("RoundUISetup");

            // Add button functionality for CAVERN setup and round UI setup
            Button cavernSetupButton = root.Q<Button>("CavernSetupButton");
            cavernSetupButton.RegisterCallback<ClickEvent, VisualElement>(CavernSetup, roundUI);

            Button roundUISetupButton = root.Q<Button>("RoundUISetupButton");

            CavernRenderer cavernRenderer = FindFirstObjectByType<CavernRenderer>();

            // Hides roundUI setup if no CAVERN setup present in scene since it depends on the setup
            if (cavernRenderer == null)
            {
                roundUI.style.visibility = Visibility.Hidden;
            }
            roundUISetupButton.RegisterCallback<ClickEvent>(RoundUISetup);
        }

        private void CavernSetup(ClickEvent evt, VisualElement roundUI)
        {
            // load from path
            GameObject cavernSetupPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/CavernSetup.prefab", typeof(GameObject));
            GameObject cavernSetupInstance = (GameObject)PrefabUtility.InstantiatePrefab(cavernSetupPrefab as GameObject);

            // sets speaker mode to 7.1 surround
            AudioConfiguration audioConfigs = AudioSettings.GetConfiguration();
            audioConfigs.speakerMode = AudioSpeakerMode.Mode7point1;
            AudioSettings.Reset(audioConfigs);

            // removes any default main cameras in scene (but preserves any cameras not tagged as MainCamera)
            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamera != null)
            {
                Undo.DestroyObjectImmediate(GameObject.FindGameObjectWithTag("MainCamera"));
            }

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            // sets roundUI section of tools panel to be visible
            roundUI.style.visibility = Visibility.Visible;
        }

        private void RoundUISetup(ClickEvent evt)
        {
            CavernRenderer cavernRenderer = FindFirstObjectByType<CavernRenderer>();

            // load from path
            GameObject cavernUIPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/CavernUI.prefab", typeof(GameObject));
            GameObject cavernUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(cavernUIPrefab as GameObject);

            GameObject roundCavernMeshRendererPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/RoundCavernMeshRenderer.prefab", typeof(GameObject));
            GameObject roundCavernMeshRendererInstance = (GameObject)PrefabUtility.InstantiatePrefab(roundCavernMeshRendererPrefab as GameObject);

            WorldSpaceMeshCanvas meshCanvas = roundCavernMeshRendererInstance.GetComponent<WorldSpaceMeshCanvas>();
            meshCanvas.setCavernRenderer(cavernRenderer);

            // set default parameters of roundUI mesh
            meshCanvas.transform.parent = cavernRenderer.transform;
            meshCanvas.transform.localPosition = Vector3.zero;
            meshCanvas.transform.localRotation = Quaternion.identity;

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}

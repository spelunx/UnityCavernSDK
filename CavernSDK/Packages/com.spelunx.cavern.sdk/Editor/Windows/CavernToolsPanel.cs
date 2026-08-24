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
            VisualElement screenSpaceUI = root.Q("ScreenSpaceUISetup");

            CavernRenderer cavernRenderer = FindAnyObjectByType<CavernRenderer>();

            // Hides roundUI setup if no CAVERN setup present in scene since it depends on the setup
            if (cavernRenderer == null)
            {
                roundUI.style.visibility = Visibility.Hidden;
                screenSpaceUI.style.visibility = Visibility.Hidden;
            }

            // Add button functionality for CAVERN setup
            Button cavernSetupButton = root.Q<Button>("CavernSetupButton");
            cavernSetupButton.RegisterCallback<ClickEvent, VisualElement>(CavernSetup, roundUI);

            // Add button functionality for Round UI setup
            Button roundUISetupButton = root.Q<Button>("RoundUISetupButton");
            roundUISetupButton.RegisterCallback<ClickEvent>(RoundUISetup);

            // Add button functionality for Screen Space UI setup
            Button screenSpaceUISetupButton = root.Q<Button>("ScreenSpaceUISetupButton");
            screenSpaceUISetupButton.RegisterCallback<ClickEvent>(ScreenSpaceUISetup);
        }

        private void CavernSetup(ClickEvent evt, VisualElement roundUI)
        {
            // load from path
            GameObject cavernSetupPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/Cavern Setup.prefab", typeof(GameObject));
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
            CavernSetup cavernSetup = FindAnyObjectByType<CavernSetup>();

            // load from path
            GameObject cavernUIPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/CavernUI.prefab", typeof(GameObject));
            GameObject cavernUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(cavernUIPrefab as GameObject);

            GameObject roundCavernMeshRendererPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/RoundCavernMeshRenderer.prefab", typeof(GameObject));
            GameObject roundCavernMeshRendererInstance = (GameObject)PrefabUtility.InstantiatePrefab(roundCavernMeshRendererPrefab as GameObject, cavernSetup.transform);
            CavernRoundWorldSpaceUIFeature feat = roundCavernMeshRendererInstance.GetComponent<CavernRoundWorldSpaceUIFeature>();

            feat.uiCamera = cavernUIInstance.GetComponentInChildren<Camera>();

            // set default parameters of roundUI mesh
            feat.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private void ScreenSpaceUISetup(ClickEvent evt)
        {
            CavernSetup cavernSetup = FindAnyObjectByType<CavernSetup>();

            // load from path
            GameObject cavernUIPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Prefabs/CavernUI.prefab", typeof(GameObject));
            GameObject cavernUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(cavernUIPrefab as GameObject);


            CavernScreenSpaceUIFeature feat = cavernSetup.GetComponentInChildren<CavernRenderer>().gameObject.AddComponent<CavernScreenSpaceUIFeature>();
            feat.uiCamera = cavernUIInstance.GetComponentInChildren<Camera>();
            feat.screenSpaceUIShader = (Shader)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.sdk/Runtime/Scripts/Canvas/DoublerWithOffset.shadergraph", typeof(Shader));
            feat.CreateMaterial();

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}

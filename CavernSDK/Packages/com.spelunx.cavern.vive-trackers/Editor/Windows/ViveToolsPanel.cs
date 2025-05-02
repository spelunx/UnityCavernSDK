using Spelunx;
using Spelunx.Vive;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Spelunx
{
    public class ViveToolsPanel : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private UnityEngine.Object viveManagerPrefab;
        private UnityEngine.Object viveTrackerPrefab;
        private GameObject viveManagerInstance;
        private int trackerCount = 0;
        private Label trackerCountLabel;

        // places tools under CAVERN toolbar with hierarchy ordering
        [MenuItem("CAVERN/VIVE Tracker Tools", false, 101)]
        public static void ShowExample()
        {
            ViveToolsPanel wnd = GetWindow<ViveToolsPanel>();
            wnd.titleContent = new GUIContent("Vive Tracker Tools");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML, UI setup in UXML document
            VisualElement panelSetup = m_VisualTreeAsset.Instantiate();
            root.Add(panelSetup);

            // Add button functionality for vive setup
            VisualElement viveSetupButton = root.Q("ViveSetupButton");
            viveSetupButton.RegisterCallback<ClickEvent>(ViveSetup);

            trackerCountLabel = root.Q<Label>("TrackerCount");
            EditorApplication.hierarchyChanged += OnHierarchyChanged;


            // Add button functionality for all of the building block interactions
            Button followButton = root.Q<Button>("FollowButton");
            followButton.RegisterCallback<ClickEvent>(AddBuildingBlock<FollowInteraction>);

            Button orbitButton = root.Q<Button>("OrbitButton");
            orbitButton.RegisterCallback<ClickEvent>(AddBuildingBlock<OrbitCavernInteraction>);

            Button evadeButton = root.Q<Button>("EvadeButton");
            evadeButton.RegisterCallback<ClickEvent>(AddBuildingBlock<EvadeInteraction>);

            Button lookAtButton = root.Q<Button>("LookAtButton");
            lookAtButton.RegisterCallback<ClickEvent>(AddBuildingBlock<LookAt>);

            Button zonesButton = root.Q<Button>("ZonesButton");
            zonesButton.RegisterCallback<ClickEvent>(AddZones);

        }

        private void OnHierarchyChanged()
        {
            var trackers = FindObjectsByType<ViveTracker>(FindObjectsSortMode.None);

            // update information if number of Vive trackers in scene changes
            if (trackerCount != trackers.Length)
            {
                trackerCount = trackers.Length;
                trackerCountLabel.text = "VIVE Trackers in Scene: " + trackerCount;
            }
        }

        // adds vive tracker 
        private void ViveSetup(ClickEvent evt)
        {
            // load from path
            viveManagerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.vive-trackers/Prefabs/ViveTrackerManager.prefab", typeof(GameObject));
            viveTrackerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.vive-trackers/Prefabs/ViveTracker.prefab", typeof(GameObject));

            // check if vive tracker manager is already present
            var manager = FindObjectsByType<Vive_Manager>(FindObjectsSortMode.None);

            // adds manager if not present in scene
            if (manager.Length == 0)
            {
                viveManagerInstance = (GameObject)PrefabUtility.InstantiatePrefab(viveManagerPrefab as GameObject);

                // set vive manager to be in the CAVERN setup folder in the scene hierarchy
                GameObject cavernSetup = GameObject.Find("CavernSetup");
                if (cavernSetup != null)
                {
                    viveManagerInstance.GetComponent<Transform>().parent = cavernSetup.transform;
                    // load in the debug keys
                    cavernSetup.AddComponent<ViveDebugKeys>();
                }
            }

            // instantiate a new vive tracker and set its origin to the vive manager
            GameObject viveTrackerInstance = (GameObject)PrefabUtility.InstantiatePrefab(viveTrackerPrefab as GameObject);
            viveTrackerInstance.GetComponent<ViveTracker>().SetOrigin(FindObjectsByType<Vive_Manager>(FindObjectsSortMode.None)[0].transform);

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // adds a building block script to the selected object
        private void AddBuildingBlock<T>(ClickEvent evt) where T : Interaction
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go != null)
                {
                    T interaction = go.AddComponent<T>();

                    // Set the target to the first vive tracker found by default.
                    ViveTracker defaultViveTracker = FindFirstObjectByType<ViveTracker>();
                    if (defaultViveTracker != null)
                    {
                        interaction.SetTarget(defaultViveTracker.transform);
                    }

                    // CavernInteraction specific
                    if (typeof(T).IsSubclassOf(typeof(CavernInteraction)))
                    {
                        CavernInteraction cavernInteraction = interaction as CavernInteraction;
                        cavernInteraction.SetCavernRenderer(FindFirstObjectByType<CavernRenderer>());
                    }
                }
            }
        }

        // adds zones to the CAVERN 
        private void AddZones(ClickEvent evt)
        {
            Zones component = FindObjectsByType<Vive_Manager>(FindObjectsSortMode.None)[0].gameObject.AddComponent(typeof(Zones)) as Zones;
            component.cavern = FindFirstObjectByType<CavernRenderer>();
        }
    }
}

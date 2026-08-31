using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Spelunx.XR.Vive
{
    public class ViveToolsPanel : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private UnityEngine.Object trackingOriginPrefab;
        private UnityEngine.Object viveTrackerPrefab;
        private UnityEngine.Object indexControllerPrefab;
        private GameObject trackingOriginInstance;
        private int trackerCount = 0;
        private int controllerCount = 0;
        private Label trackerCountLabel;
        private Label controllerCountLabel;

        // places tools under CAVERN toolbar with hierarchy ordering
        [MenuItem("CAVERN/XR Tracker Tools", false, 101)]
        public static void ShowExample()
        {
            ViveToolsPanel wnd = GetWindow<ViveToolsPanel>();
            wnd.titleContent = new GUIContent("XR Tracker Tools");
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

            // Add button functionality for vive controller setup
            VisualElement viveControllerSetupButton = root.Q("ViveControllerSetupButton");
            viveControllerSetupButton.RegisterCallback<ClickEvent>(IndexControllerSetup);

            trackerCountLabel = root.Q<Label>("TrackerCount");
            controllerCountLabel = root.Q<Label>("ControllerCount");
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
            var controllers = FindObjectsByType<IndexController>(FindObjectsSortMode.None);
            // update information if number of Vive trackers in scene changes
            if (trackerCount != trackers.Length)
            {
                trackerCount = trackers.Length;
                trackerCountLabel.text = "VIVE Trackers in Scene: " + trackerCount;
            }

            if (controllerCount != controllers.Length)
            {
                controllerCount = controllers.Length;
                controllerCountLabel.text = "Index Controllers in Scene: " + controllerCount;
            }
        }

        // adds vive tracker 
        private void ViveSetup(ClickEvent evt)
        {
            ConfigureTrackingOrigin();

            // load from path
            viveTrackerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.xr-vive-trackers/Prefabs/Vive Tracker.prefab", typeof(GameObject));

            // instantiate a new vive tracker and set its origin to the tracking space origin
            GameObject viveTrackerInstance = (GameObject)PrefabUtility.InstantiatePrefab(viveTrackerPrefab as GameObject);
            viveTrackerInstance.GetComponent<ViveTracker>().SetOrigin(FindAnyObjectByType<TrackingSpaceOrigin>());

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // Adds vive controller
        private void IndexControllerSetup(ClickEvent evt)
        {
            ConfigureTrackingOrigin();

            // load from path
            indexControllerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.xr-vive-trackers/Prefabs/ViveController.prefab", typeof(GameObject));

            // instantiate a new index controller and set its origin to the tracking space origin
            GameObject indexControllerInstance = (GameObject)PrefabUtility.InstantiatePrefab(indexControllerPrefab as GameObject);
            indexControllerInstance.GetComponent<IndexController>().SetOrigin(FindAnyObjectByType<TrackingSpaceOrigin>());

            // mark scene as edited to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private void ConfigureTrackingOrigin()
        {
            var trackingOrigin = FindAnyObjectByType<TrackingSpaceOrigin>();

            // adds tracking space origin if not present in scene
            if (trackingOrigin == null)
            {
                trackingOriginPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.xr-vive-trackers/Prefabs/Tracking Space Origin.prefab", typeof(GameObject));
                // set tracking space origin to be in the CAVERN setup folder in the scene hierarchy
                CavernSetup cavernSetup = FindAnyObjectByType<CavernSetup>();
                if (cavernSetup != null)
                {
                    trackingOriginInstance = (GameObject)PrefabUtility.InstantiatePrefab(trackingOriginPrefab as GameObject, cavernSetup.transform);
                    // load in the debug keys
                    cavernSetup.gameObject.AddComponent<ViveDebugKeysFeature>();
                }
                else
                {
                    trackingOriginInstance = (GameObject)PrefabUtility.InstantiatePrefab(trackingOriginPrefab as GameObject);
                }
            }
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
                        cavernInteraction.SetCavernSetup(FindFirstObjectByType<CavernSetup>());
                    }
                }
            }
        }

        // adds zones to the CAVERN 
        private void AddZones(ClickEvent evt)
        {
            ConfigureTrackingOrigin();
            Zones component = FindAnyObjectByType<TrackingSpaceOrigin>().gameObject.AddComponent(typeof(Zones)) as Zones;
            component.cavern = FindAnyObjectByType<CavernSetup>();
        }
    }
}

using Spelunx.Orbbec;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Spelunx
{
    public class OrbbecToolsPanel : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        // places tools under CAVERN toolbar with hierarchy ordering
        [MenuItem("CAVERN/Orbbec Tools", false, 102)]
        public static void ShowExample()
        {
            OrbbecToolsPanel wnd = GetWindow<OrbbecToolsPanel>();
            wnd.titleContent = new GUIContent("Orbbec Tools");
        }

        public void CreateGUI()
        {
            // root VisualElement object of editor window
            VisualElement root = rootVisualElement;

            // Instantiate UXML, UI setup in UXML document
            VisualElement panelSetup = m_VisualTreeAsset.Instantiate();
            root.Add(panelSetup);

            VisualElement avatarSetup = root.Q("AvatarSetup");
            BodyTracker bodyTracker = FindFirstObjectByType<BodyTracker>();

            // Hides avatar setup section if there's no BodyTracker in scene since avatar depends on it
            if (bodyTracker == null)
            {
                avatarSetup.style.visibility = Visibility.Hidden;
            }

            // add functionality for the Orbbec and avatar setup buttons
            Button orbbecSetupButton = root.Q<Button>("OrbbecSetupButton");
            orbbecSetupButton.RegisterCallback<ClickEvent, VisualElement>(OrbbecSetup, avatarSetup);

            Button avatarSetupButton = root.Q<Button>("AvatarSetupButton");
            avatarSetupButton.RegisterCallback<ClickEvent>(AddSampleAvatar);
        }

        private void OrbbecSetup(ClickEvent evt, VisualElement avatarSetup)
        {
            // load from path and instantiate
            GameObject bodyTrackerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.orbbec.sdk/Prefabs/BodyTracker.prefab", typeof(GameObject));
            GameObject bodyTrackerManagerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.orbbec.sdk/Prefabs/BodyTrackerManager.prefab", typeof(GameObject));
            GameObject bodyTrackerInstance = (GameObject)PrefabUtility.InstantiatePrefab(bodyTrackerPrefab);
            GameObject bodyTrackerManagerInstance = (GameObject)PrefabUtility.InstantiatePrefab(bodyTrackerManagerPrefab);

            bodyTrackerManagerInstance.GetComponent<BodyTrackerManager>().SetBodyTracker(bodyTrackerInstance.GetComponent<BodyTracker>());

            // set avatar to be visible after Orbbec setup is present in scene
            avatarSetup.style.visibility = Visibility.Visible;

            // mark scene as changed to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private void AddSampleAvatar(ClickEvent evt)
        {
            BodyTracker bodyTracker = FindFirstObjectByType<BodyTracker>();

            // load from path and instantiate
            GameObject bodyTrackerAvatarPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.orbbec.sdk/Prefabs/BodyTrackerAvatar.prefab", typeof(GameObject));
            GameObject bodyTrackerAvatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(bodyTrackerAvatarPrefab);

            // set default body tracker and root joint
            bodyTrackerAvatarInstance.GetComponent<BodyTrackerAvatar>().SetBodyTracker(bodyTracker);
            bodyTrackerAvatarInstance.GetComponent<BodyTrackerAvatar>().SetSkeletonRootJoint(bodyTracker.GetRootJoint());

            // mark scene as changed to prompt saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
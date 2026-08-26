using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Spelunx
{
    [CustomEditor(typeof(CavernFeature), true)]
    public class CavernFeatureEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            CavernFeature feat = (CavernFeature)target;
            bool hasCavernSetup = feat.GetComponentInParent<CavernSetup>();
            if (!hasCavernSetup)
            {
                HelpBox warningBox = new HelpBox(
                "Warning: This feature needs to be a child of a Cavern Setup.",
                HelpBoxMessageType.Warning
            );

                // 3. Add the warning to the top of the container
                root.Add(warningBox);
            }

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }
    }
}

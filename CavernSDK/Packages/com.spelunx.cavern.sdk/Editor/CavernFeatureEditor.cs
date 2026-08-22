using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Rendering;

namespace Spelunx
{
    [CustomPropertyDrawer(typeof(CavernFeatureSet))]
    public class CavernFeatureEditor : PropertyDrawer
    {
        class Styles
        {
            public static readonly GUIContent CavernFeatures =
                new GUIContent("Cavern Features",
                    "A Cavern Feature adds additional functionality to the CAVERN rendering.");
            public static readonly GUIContent MissingFeature = new GUIContent("Missing CavernFeature",
                "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature.");

            public static GUIStyle BoldLabelSimple;

            static Styles()
            {
                BoldLabelSimple = new GUIStyle(EditorStyles.label);
                BoldLabelSimple.fontStyle = FontStyle.Bold;
            }
        }

        private SerializedProperty m_CavernFeatures;
        private SerializedProperty m_CavernFeaturesMap;
        private SerializedProperty m_FalseBool;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_CavernFeatures = property.FindPropertyRelative("cavernFeatures");
            var container = new UnityEngine.UIElements.PopupWindow
            {
                text = "Cavern Features"
            };
            var list = new ListView
            {
                showAddRemoveFooter = false,
                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                showBorder = true,
                makeItem = ListItemDefault,
                bindItem = (element, index) => BindListItem(element, m_CavernFeatures.GetArrayElementAtIndex(index)),
                makeNoneElement = () => new HelpBox("No Cavern Features added", HelpBoxMessageType.Info),
                bindingPath = m_CavernFeatures.propertyPath,
                reorderMode = ListViewReorderMode.Animated,
                reorderable = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            // for (int i = 0; i < m_CavernFeatures.arraySize; i++)
            // {
            //     SerializedProperty cavernFeaturesProperty = m_CavernFeatures.GetArrayElementAtIndex(i);
            //     // DrawCavernFeature(i, ref cavernFeaturesProperty);
            //     // CoreEditorUtils.DrawSplitter();
            //     var prop = DrawCavernFeature(ref cavernFeaturesProperty);
            //     list.Add(prop);
            // }
            container.Add(list);

            var spacer = new VisualElement();
            // spacer.style.marginTop = 10;
            spacer.style.height = 10;

            var addFeatureButton = new Button(() =>
            {
                // var r = hscope.rect;
                //     var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                //     FilterWindow.Show(pos, new CavernFeatureProvider(this));

            })
            {
                text = "Add Cavern Feature"
            };

            container.Add(spacer);
            container.Add(addFeatureButton);

            // var cavernFeaturesField = new PropertyField(property.FindPropertyRelative("cavernFeatures"));            

            /*
            EditorGUILayout.LabelField(Styles.CavernFeatures, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_CavernFeatures.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Cavern Features added", MessageType.Info);
            }
            else
            {
                //Draw List
                CoreEditorUtils.DrawSplitter();
                for (int i = 0; i < m_CavernFeatures.arraySize; i++)
                {
                    SerializedProperty cavernFeaturesProperty = m_CavernFeatures.GetArrayElementAtIndex(i);
                    DrawCavernFeature(i, ref cavernFeaturesProperty);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            EditorGUILayout.Space();

            //Add renderer
            using (var hscope = new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Cavern Feature", EditorStyles.miniButton))
                {
                    var r = hscope.rect;
                    var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                    FilterWindow.Show(pos, new CavernFeatureProvider(this));
                }
            }
            */

            return container;
        }

        public void CreateComponentTree(List<FilterWindow.Element> tree)
        {
            tree.Add(new FilterWindow.GroupElement(0, "Cavern Features"));
            var types = TypeCache.GetTypesDerivedFrom<CavernFeature>();
            foreach (var type in types)
            {
                // Check to see if the current renderer feature can be used with the current renderer. If the attribute isn't found then its compatible with everything.

                if (type.IsAbstract)
                    continue;



                // TODO: check for duplicate features
                // if (data.DuplicateFeatureCheck(type))
                // {
                //     continue;
                // }

                string path = GetMenuNameFromType(type);
                tree.Add(new FeatureElement
                {
                    content = new GUIContent(path),
                    level = 1,
                    type = type
                });
            }
        }
        class FeatureElement : FilterWindow.Element
        {
            public Type type;
        }

        string GetMenuNameFromType(Type type)
        {
            string path = GetCustomTitle(type);

            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            return path;
        }

        internal string GetCustomTitle(Type type)
        {
            var isSingleFeature = type.GetCustomAttribute<DisallowMultipleCavernFeature>();
            string title = null;
            if (isSingleFeature != null)
            {
                title = isSingleFeature.customTitle;
            }
            title ??= ObjectNames.NicifyVariableName(type.Name);
            return title;
        }

        private VisualElement ListItemDefault()
        {
            var foldout = new HeaderFoldout
            {
                showEnableCheckbox = true
            };
            var prop = new PropertyField();
            // var prop = new InspectorElement();
            foldout.Add(prop);
            return foldout;
        }

        private void BindListItem(VisualElement element, SerializedProperty cavernFeatureProperty)
        {
            var foldout = element as HeaderFoldout;
            var cavernFeatureObjRef = cavernFeatureProperty.managedReferenceValue;
            if (cavernFeatureObjRef != null)
            {
                foldout.text = GetCustomTitle(cavernFeatureObjRef.GetType());
                foldout.tooltip = GetTooltip(cavernFeatureObjRef.GetType());

                foldout.enableToggle.BindProperty(cavernFeatureProperty.FindPropertyRelative("isEnabled"));
                // foldout.enableToggle.RegisterValueChangedCallback((val) =>
                // {
                //     if (val.newValue)
                //     {
                //         ((CavernFeature)cavernFeatureObjRef).Enable();
                //     }
                //     else
                //     {
                //         ((CavernFeature)cavernFeatureObjRef).Disable();
                //     }
                // });
                // foldout.BindProperty(cavernFeatureProperty);
                foldout.Q<PropertyField>().BindProperty(cavernFeatureProperty);
                // foldout.BindProperty(cavernFeatureProperty);

                // var prop = new PropertyField(cavernFeatureProperty);

            }
            else
            {
                // new Label("Missing Feature");
            }
        }

        // private void OnEnable()
        // {
        //     m_CavernFeatures = serializedObject.FindProperty(nameof(CavernFeatureSet.cavernFeatures));
        //     m_CavernFeaturesMap = serializedObject.FindProperty(nameof(CavernFeatureSet.cavernFeaturesMap));
        //     UpdateEditorList();
        // }

        // private void OnDisable()
        // {
        //     ClearEditorsList();
        // }

        /// <inheritdoc/>
        // public override void OnInspectorGUI()
        // {
        //     if (m_CavernFeatures == null)
        //         OnEnable();
        //     else if (m_CavernFeatures.arraySize != m_Editors.Count)
        //         UpdateEditorList();

        //     serializedObject.Update();
        //     DrawCavernFeatureList();
        // }

        // private void DrawCavernFeatureList()
        // {
        //     EditorGUILayout.LabelField(Styles.CavernFeatures, EditorStyles.boldLabel);
        //     EditorGUILayout.Space();

        //     if (m_CavernFeatures.arraySize == 0)
        //     {
        //         EditorGUILayout.HelpBox("No Cavern Features added", MessageType.Info);
        //     }
        //     else
        //     {
        //         //Draw List
        //         CoreEditorUtils.DrawSplitter();
        //         for (int i = 0; i < m_CavernFeatures.arraySize; i++)
        //         {
        //             SerializedProperty cavernFeaturesProperty = m_CavernFeatures.GetArrayElementAtIndex(i);
        //             DrawCavernFeature(i, ref cavernFeaturesProperty);
        //             CoreEditorUtils.DrawSplitter();
        //         }
        //     }
        //     EditorGUILayout.Space();

        //     //Add renderer
        //     using (var hscope = new EditorGUILayout.HorizontalScope())
        //     {
        //         if (GUILayout.Button("Add Cavern Feature", EditorStyles.miniButton))
        //         {
        //             var r = hscope.rect;
        //             var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
        //             FilterWindow.Show(pos, new CavernFeatureProvider(this));
        //         }
        //     }
        // }


        //     // string helpURL;
        //     // DocumentationUtils.TryGetHelpURL(cavernFeatureObjRef.GetType(), out helpURL);

        //     // Get the serialized object for the editor script & update it
        //     Editor rendererFeatureEditor = m_Editors[index];
        //     SerializedObject serializedRendererFeaturesEditor = rendererFeatureEditor.serializedObject;
        //     serializedRendererFeaturesEditor.Update();

        //     // Foldout header
        //     EditorGUI.BeginChangeCheck();
        //     SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
        //     bool displayContent = CoreEditorUtils.DrawHeaderToggle(EditorGUIUtility.TrTextContent(title, tooltip), cavernFeatureProperty, activeProperty, pos => OnContextClick(cavernFeatureObjRef, pos, index), null, null, null);
        //     hasChangedProperties |= EditorGUI.EndChangeCheck();

        //     // ObjectEditor
        //     if (displayContent)
        //     {
        //         /*
        //         if (!hasCustomTitle)
        //         {
        //             EditorGUI.BeginChangeCheck();
        //             SerializedProperty nameProperty = serializedRendererFeaturesEditor.FindProperty("m_Name");
        //             nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
        //             if (EditorGUI.EndChangeCheck())
        //             {
        //                 hasChangedProperties = true;

        //                 // We need to update sub-asset name
        //                 cavernFeatureObjRef.name = nameProperty.stringValue;
        //                 AssetDatabase.SaveAssets();

        //                 // Triggers update for sub-asset name change
        //                 ProjectWindowUtil.ShowCreatedAsset(target);
        //             }
        //         }
        //         */

        //         EditorGUI.BeginChangeCheck();
        //         rendererFeatureEditor.OnInspectorGUI();
        //         hasChangedProperties |= EditorGUI.EndChangeCheck();

        //         EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        //     }

        //     // Apply changes and save if the user has modified any settings
        //     if (hasChangedProperties)
        //     {
        //         serializedRendererFeaturesEditor.ApplyModifiedProperties();
        //         serializedObject.ApplyModifiedProperties();
        //         ForceSave();
        //     }
        // }
        // else
        // {
        //     CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature, cavernFeatureProperty, m_FalseBool, pos => OnContextClick(cavernFeatureObjRef, pos, index));
        //     m_FalseBool.boolValue = false; // always make sure false bool is false
        //     EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
        //     if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
        //     {
        //         // ScriptableRendererData data = target as ScriptableRendererData;
        //         // if (!data.ValidateRendererFeatures())
        //         // {
        //         //     if (EditorUtility.DisplayDialog("Remove Missing Cavern Feature",
        //         //             "This cavern feature script is missing (likely deleted or failed to compile). Do you want to remove it from the list and delete the associated sub-asset?",
        //         //             "Yes", "No"))
        //         //     {
        //         //         data.RemoveMissingRendererFeatures();
        //         //     }
        //         // }
        //     }
        // }


        // private void DrawCavernFeature(int index, ref SerializedProperty cavernFeatureProperty)
        // {
        //     Object cavernFeatureObjRef = cavernFeatureProperty.objectReferenceValue;
        //     if (cavernFeatureObjRef != null)
        //     {
        //         bool hasChangedProperties = false;
        //         string title;

        //         bool hasCustomTitle = GetCustomTitle(cavernFeatureObjRef.GetType(), out title);

        //         if (!hasCustomTitle)
        //         {
        //             title = ObjectNames.GetInspectorTitle(cavernFeatureObjRef);
        //         }

        //         string tooltip;
        //         GetTooltip(cavernFeatureObjRef.GetType(), out tooltip);

        //         // string helpURL;
        //         // DocumentationUtils.TryGetHelpURL(cavernFeatureObjRef.GetType(), out helpURL);

        //         // Get the serialized object for the editor script & update it
        //         Editor rendererFeatureEditor = m_Editors[index];
        //         SerializedObject serializedRendererFeaturesEditor = rendererFeatureEditor.serializedObject;
        //         serializedRendererFeaturesEditor.Update();

        //         // Foldout header
        //         EditorGUI.BeginChangeCheck();
        //         SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
        //         bool displayContent = CoreEditorUtils.DrawHeaderToggle(EditorGUIUtility.TrTextContent(title, tooltip), cavernFeatureProperty, activeProperty, pos => OnContextClick(cavernFeatureObjRef, pos, index), null, null, null);
        //         hasChangedProperties |= EditorGUI.EndChangeCheck();

        //         // ObjectEditor
        //         if (displayContent)
        //         {
        //             /*
        //             if (!hasCustomTitle)
        //             {
        //                 EditorGUI.BeginChangeCheck();
        //                 SerializedProperty nameProperty = serializedRendererFeaturesEditor.FindProperty("m_Name");
        //                 nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
        //                 if (EditorGUI.EndChangeCheck())
        //                 {
        //                     hasChangedProperties = true;

        //                     // We need to update sub-asset name
        //                     cavernFeatureObjRef.name = nameProperty.stringValue;
        //                     AssetDatabase.SaveAssets();

        //                     // Triggers update for sub-asset name change
        //                     ProjectWindowUtil.ShowCreatedAsset(target);
        //                 }
        //             }
        //             */

        //             EditorGUI.BeginChangeCheck();
        //             rendererFeatureEditor.OnInspectorGUI();
        //             hasChangedProperties |= EditorGUI.EndChangeCheck();

        //             EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        //         }

        //         // Apply changes and save if the user has modified any settings
        //         if (hasChangedProperties)
        //         {
        //             serializedRendererFeaturesEditor.ApplyModifiedProperties();
        //             serializedObject.ApplyModifiedProperties();
        //             ForceSave();
        //         }
        //     }
        //     else
        //     {
        //         CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature, cavernFeatureProperty, m_FalseBool, pos => OnContextClick(cavernFeatureObjRef, pos, index));
        //         m_FalseBool.boolValue = false; // always make sure false bool is false
        //         EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
        //         if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
        //         {
        //             // ScriptableRendererData data = target as ScriptableRendererData;
        //             // if (!data.ValidateRendererFeatures())
        //             // {
        //             //     if (EditorUtility.DisplayDialog("Remove Missing Cavern Feature",
        //             //             "This cavern feature script is missing (likely deleted or failed to compile). Do you want to remove it from the list and delete the associated sub-asset?",
        //             //             "Yes", "No"))
        //             //     {
        //             //         data.RemoveMissingRendererFeatures();
        //             //     }
        //             // }
        //         }
        //     }
        // }
        // internal void AddComponent(Type type)
        // {
        //     serializedObject.Update();

        //     ScriptableObject component = CreateInstance(type);
        //     component.name = $"{type.Name}";
        //     Undo.RegisterCreatedObjectUndo(component, "Add Cavern Feature");

        //     // Store this new effect as a sub-asset so we can reference it safely afterwards
        //     // Only when we're not dealing with an instantiated asset
        //     if (EditorUtility.IsPersistent(target))
        //     {
        //         AssetDatabase.AddObjectToAsset(component, target);
        //     }
        //     AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

        //     // Grow the list first, then add - that's how serialized lists work in Unity
        //     m_CavernFeatures.arraySize++;
        //     SerializedProperty componentProp = m_CavernFeatures.GetArrayElementAtIndex(m_CavernFeatures.arraySize - 1);
        //     componentProp.objectReferenceValue = component;

        //     // Update GUID Map
        //     m_CavernFeaturesMap.arraySize++;
        //     SerializedProperty guidProp = m_CavernFeaturesMap.GetArrayElementAtIndex(m_CavernFeaturesMap.arraySize - 1);
        //     // guidProp.longValue = localId;
        //     UpdateEditorList();
        //     serializedObject.ApplyModifiedProperties();

        //     // Force save / refresh
        //     if (EditorUtility.IsPersistent(target))
        //     {
        //         ForceSave();
        //     }
        //     serializedObject.ApplyModifiedProperties();
        // }

        // private void RemoveComponent(int id)
        // {
        //     SerializedProperty property = m_CavernFeatures.GetArrayElementAtIndex(id);
        //     Object component = property.objectReferenceValue;
        //     property.objectReferenceValue = null;

        //     Undo.SetCurrentGroupName(component == null ? "Remove Renderer Feature" : $"Remove {component.name}");

        //     // remove the array index itself from the list
        //     m_CavernFeatures.DeleteArrayElementAtIndex(id);
        //     m_CavernFeaturesMap.DeleteArrayElementAtIndex(id);
        //     UpdateEditorList();
        //     serializedObject.ApplyModifiedProperties();

        //     // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
        //     // actions will be in the wrong order and the reference to the setting object in the
        //     // list will be lost.
        //     if (component != null)
        //     {
        //         Undo.DestroyObjectImmediate(component);

        //         CavernFeature feature = component as CavernFeature;
        //         feature?.Dispose();
        //     }

        //     // Force save / refresh
        //     ForceSave();
        // }

        private string ValidateName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
        }

        // private void OnContextClick(Object rendererFeatureObject, Vector2 position, int id)
        // {
        //     var menu = new GenericMenu();

        //     if (id == 0)
        //         menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
        //     else
        //         menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, -1));

        //     if (id == m_CavernFeatures.arraySize - 1)
        //         menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
        //     else
        //         menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));

        //     // if(rendererFeatureObject?.GetType() == typeof(FullScreenPassRendererFeature))
        //     //     menu.AddAdvancedPropertiesBoolMenuItem();

        //     menu.AddSeparator(string.Empty);
        //     menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

        //     menu.DropDown(new Rect(position, Vector2.zero));
        // }

        // private void UpdateEditorList()
        // {
        //     ClearEditorsList();
        //     for (int i = 0; i < m_CavernFeatures.arraySize; i++)
        //     {
        //         m_Editors.Add(CreateEditor(m_CavernFeatures.GetArrayElementAtIndex(i).objectReferenceValue));
        //     }
        // }

        //To avoid leaking memory we destroy editors when we clear editors list
        // private void ClearEditorsList()
        // {
        //     for (int i = m_Editors.Count - 1; i >= 0; --i)
        //     {
        //         DestroyImmediate(m_Editors[i]);
        //     }
        //     m_Editors.Clear();
        // }

        // private void ForceSave()
        // {
        //     EditorUtility.SetDirty(target);
        // }

        private string GetTooltip(Type type)
        {
            var attribute = type.GetCustomAttribute<TooltipAttribute>();
            if (attribute != null)
            {
                return attribute.tooltip;
            }
            return null;
        }
    }

    /// <summary>
    ///   <para>Prevents <c>CavernFeatures</c> of same type to be added more than once to a CavernSetup.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleCavernFeature : Attribute
    {
        /// <summary>
        /// Set the custom title for Cavern feature.
        /// </summary>
        public string customTitle { private set; get; }

        /// <summary>
        /// Constructor for the attribute to prevent <c>CavernFeatures</c> of same type to be added more than once to a CavernSetup.
        /// </summary>
        /// <param name="customTitle">Sets the custom title for Cavern feature.</param>
        public DisallowMultipleCavernFeature(string customTitle = null)
        {
            this.customTitle = customTitle;
        }
    }
}
